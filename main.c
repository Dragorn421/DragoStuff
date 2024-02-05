// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#include <stdbool.h>
#include <stdlib.h>   // for malloc, free, abort, mkdtemp
#include <stdio.h>    // for fprintf, stderr, perror, fopen, fclose, snprintf
#include <string.h>   // for memset
#include <unistd.h>   // for execvp, fork, unlink, rmdir
#include <sys/wait.h> // for waitpid
#include <assert.h>

// sudo apt install uuid-dev
#include <uuid/uuid.h>

#include <iconv.h>

#include "common.h"
#include "pipe_lines.h"
#include "process_line.h"
#include "utils.h"

struct crwasmp_args_optflags {
    bool g3;
    bool g;
    bool O0;
    bool O1;
    bool O2;
    bool framepointer;
    bool mips1;
};

struct crwasmp_args {
    char* in_file;
    char* in_file_dirname;
    char* in_file_basename;
    char* out_file;
    cvector_vector_type(char*) compiler;
    cvector_vector_type(char*) assembler_args;
    cvector_vector_type(char*) compile_args;
    struct crwasmp_args_optflags opt_flags;
};

struct crwasmp_args* crwasmp_args_parse(int argc, char** argv) {
    cvector_vector_type(char*) all_args = NULL;
    cvector_init(all_args, argc - 1, NULL);
    for (int i = 1; i < argc; i++)
        cvector_push_back(all_args, argv[i]);

    int sep0 = 0;
    while (sep0 < cvector_size(all_args) && str_startswith(all_args[sep0], "-")) {
        sep0++;
    }
    assert(sep0 < cvector_size(all_args));

    int sep1 = sep0 + 1;
    while (sep1 < cvector_size(all_args) && !str_equal(all_args[sep1], "--")) {
        sep1++;
    }
    assert(sep1 < cvector_size(all_args));

    int sep2 = sep1 + 1;
    while (sep2 < cvector_size(all_args) && !str_equal(all_args[sep2], "--")) {
        sep2++;
    }
    assert(sep2 < cvector_size(all_args));

    cvector_vector_type(char*) asmproc_flags = NULL;
    cvector_slice(asmproc_flags, all_args, 0, sep0);
    cvector_vector_type(char*) compiler = NULL;
    cvector_slice(compiler, all_args, sep0, sep1);
    cvector_vector_type(char*) assembler_args = NULL;
    cvector_slice(assembler_args, all_args, sep1 + 1, sep2);
    cvector_vector_type(char*) compile_args = NULL;
    cvector_slice(compile_args, all_args, sep2 + 1, cvector_size(all_args));

    assert(cvector_size(compile_args) >= 1);
    char* in_file = compile_args[cvector_size(compile_args) - 1];
    cvector_pop_back(compile_args);

    int out_ind = 0;
    while (out_ind < cvector_size(compile_args) && !str_equal(compile_args[out_ind], "-o")) {
        out_ind++;
    }
    assert(out_ind < cvector_size(compile_args));

    assert(out_ind + 1 < cvector_size(compile_args));
    char* out_file = compile_args[out_ind + 1];
    cvector_erase(compile_args, out_ind + 1);
    cvector_erase(compile_args, out_ind);

    struct crwasmp_args_optflags opt_flags;
    memset(&opt_flags, 0, sizeof(struct crwasmp_args_optflags));
    struct {
        char* arg_str;
        bool* opt_flag_p;
    } available_opt_flags[] = {
        { "-g3", &opt_flags.g3 }, { "-g", &opt_flags.g },   { "-O0", &opt_flags.O0 },
        { "-O1", &opt_flags.O1 }, { "-O2", &opt_flags.O2 }, { "-framepointer", &opt_flags.framepointer },
    };
    for (int j = 0; j < ARRAY_COUNT(available_opt_flags); j++) {
        bool has_flag = false;
        for (int i = 0; i < cvector_size(compile_args); i++) {
            if (str_equal(compile_args[i], available_opt_flags[j].arg_str)) {
                has_flag = true;
                break;
            }
        }
        *available_opt_flags->opt_flag_p = has_flag;
    }
    bool mips2_in_compile_args = false;
    for (int i = 0; i < cvector_size(compile_args); i++) {
        if (str_equal(compile_args[i], "-mips2")) {
            mips2_in_compile_args = true;
            break;
        }
    }
    opt_flags.mips1 = !mips2_in_compile_args;

    // TODO handle asmproc_flags
    // (I think only --drop-mdebug-gptab is relevant here,
    //  as it's the only flag in asm_processor.py that is not handled by build.py)
    assert(cvector_size(asmproc_flags) == 0);

    for (int i = 0; i < cvector_size(asmproc_flags); i++) {
        fprintf(stderr, "asmproc_flags[%d] = %s\n", i, asmproc_flags[i]);
    }
    for (int i = 0; i < cvector_size(compiler); i++) {
        fprintf(stderr, "compiler[%d] = %s\n", i, compiler[i]);
    }
    for (int i = 0; i < cvector_size(assembler_args); i++) {
        fprintf(stderr, "assembler_args[%d] = %s\n", i, assembler_args[i]);
    }
    for (int i = 0; i < cvector_size(compile_args); i++) {
        fprintf(stderr, "compile_args[%d] = %s\n", i, compile_args[i]);
    }

    cvector_free(all_args);
    cvector_free(asmproc_flags);

    char* in_file_dirname;
    char* in_file_basename;
    split_path(in_file, &in_file_dirname, &in_file_basename);

    struct crwasmp_args* args = malloc(sizeof(struct crwasmp_args));
    assert(args != NULL);
    memset(args, 0, sizeof(struct crwasmp_args));
    args->in_file_dirname = in_file_dirname;
    args->in_file_basename = in_file_basename;
    args->opt_flags = opt_flags;
    // Note: all strings below are from argv
    args->in_file = in_file;
    args->out_file = out_file;
    args->compiler = compiler;
    args->assembler_args = assembler_args;
    args->compile_args = compile_args;
    return args;
}

void crwasmp_args_free(struct crwasmp_args* args) {
    free(args->in_file_dirname);
    free(args->in_file_basename);
    cvector_free(args->compiler);
    cvector_free(args->assembler_args);
    cvector_free(args->compile_args);
    free(args);
}

void preprocess(struct crwasmp_args* args, char* preprocessed_path) {
    FILE* in_file_f = fopen(args->in_file, "r");
    if (in_file_f == NULL) {
        perror("fopen(in_file)");
        abort();
    }

    FILE* preprocessed_path_f = fopen(preprocessed_path, "w");
    if (preprocessed_path_f == NULL) {
        perror("fopen(preprocessed_path)");
        abort();
    }

    static_assert(INPUT_MUST_BE_UTF8);
    // TODO take encoding from arguments
    iconv_t utf8_to_eucjp = iconv_open("euc-jp", "utf-8");
    assert(utf8_to_eucjp != (iconv_t)-1);

    fprintf(stderr, "%s -> %s\n", args->in_file, preprocessed_path);

    {
        struct pipe_context* pipe_ctx = pipe_lines_ctx_create(preprocessed_path_f, utf8_to_eucjp);
        assert(pipe_ctx != NULL);
        {
            struct process_line_context* ctx =
                process_line_ctx_create(pipe_ctx, args->in_file_dirname, args->in_file_basename);
            assert(ctx != NULL);
            {
                writelinedirective(pipe_ctx, 1, args->in_file_basename); // FIXME put in a better place
                pipe_lines(pipe_ctx, in_file_f, process_line, ctx);
                pipe_lines_flush(pipe_ctx); // TODO should be done automatically
            }
            process_line_ctx_free(ctx);
            ctx = NULL;
        }
        pipe_lines_ctx_free(pipe_ctx);
        pipe_ctx = NULL;
    }

    iconv_close(utf8_to_eucjp);

    fclose(in_file_f);
    in_file_f = NULL;

    fclose(preprocessed_path_f);
    preprocessed_path_f = NULL;
}

/**
 * Does not return.
 */
void compile(struct crwasmp_args* args, char* preprocessed_path) {
    cvector_vector_type(char*) compile_cmdline = NULL;
    cvector_extend(compile_cmdline, args->compiler);
    cvector_extend(compile_cmdline, args->compile_args);
    cvector_push_back(compile_cmdline, "-I");
    cvector_push_back(compile_cmdline, args->in_file_dirname);
    cvector_push_back(compile_cmdline, "-o");
    cvector_push_back(compile_cmdline, args->out_file);
    cvector_push_back(compile_cmdline, preprocessed_path);
    cvector_push_back(compile_cmdline, NULL);

    for (int i = 0; i < cvector_size(compile_cmdline); i++) {
        fprintf(stderr, "compile_cmdline[%d] = %s\n", i, compile_cmdline[i]);
    }
    for (int i = 0; i < cvector_size(compile_cmdline); i++) {
        fprintf(stderr, "%s%s", i == 0 ? "" : " ", compile_cmdline[i]);
    }
    fprintf(stderr, "\n");

    execvp(compile_cmdline[0], compile_cmdline);
    perror("execvp");
    abort();
}

int main(int argc, char** argv) {
    struct crwasmp_args* args = crwasmp_args_parse(argc, argv);

    char tempdir[] = "/tmp/crwasmp_XXXXXX";
    if (mkdtemp(tempdir) == NULL) {
        perror("mkdtemp");
        abort();
    }

    char preprocessed_path[128];
    {
        const char preprocessed_filename_prefix[] = "preprocessed_";
        const char preprocessed_filename_suffix[] = ".c";
        char preprocessed_filename_uuid_str[37];
        uuid_t preprocessed_filename_uuid;
        uuid_generate_random(preprocessed_filename_uuid);
        uuid_unparse(preprocessed_filename_uuid, preprocessed_filename_uuid_str);
        int len = snprintf(preprocessed_path, sizeof(preprocessed_path), "%s/%s%s%s", tempdir,
                           preprocessed_filename_prefix, preprocessed_filename_uuid_str, preprocessed_filename_suffix);
        if (len < 0) {
            perror("snprintf");
            abort();
        }
        if (len >= sizeof(preprocessed_path)) {
            fprintf(stderr, "preprocessed_path buffer too small\n");
            abort();
        }
    }

    preprocess(args, preprocessed_path);

    int exit_status = EXIT_SUCCESS;

    pid_t compile_pid = fork();
    if (compile_pid == -1) {
        perror("fork");
        exit_status = EXIT_FAILURE;
    } else {
        if (compile_pid == 0) {
            compile(args, preprocessed_path);
            // compile() does not return
            abort();
        }

        int wstatus;
        waitpid(compile_pid, &wstatus, 0);
        if (WIFEXITED(wstatus)) {
            // compiler exited normally
            if (WEXITSTATUS(wstatus) != EXIT_SUCCESS) {
                // compilation failed
                exit_status = 55;
            }
        } else {
            // compiler exited abnormally
            exit_status = EXIT_FAILURE;
        }
    }

    crwasmp_args_free(args);
    args = NULL;

    if (unlink(preprocessed_path) != 0) {
        perror("unlink(preprocessed_path)");
        abort();
    }
    if (rmdir(tempdir) != 0) {
        perror("rmdir(tempdir)");
        abort();
    }

    return exit_status;
}
