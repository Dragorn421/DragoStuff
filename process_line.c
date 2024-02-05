// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#define _GNU_SOURCE // for memmem
#include <stddef.h> // for size_t
#include <stdlib.h> // for malloc, free, abort, strtof
#include <stdio.h>  // for snprintf, fprintf, fopen
#include <string.h> // for memset, memcpy, memmem
#include <limits.h> // for PATH_MAX
#include <stdbool.h>
#include <assert.h>

#include <pcre2.h>

#include "common.h"
#include "utils.h"
#include "pipe_lines.h"
#include "process_line.h"

void writelinedirective(struct pipe_context* pipe_ctx, int line, char* filename) {
    char buf[1024];
    int n = snprintf(buf, sizeof(buf), "#line %d \"%s\"\n", line, filename);
    assert(n < sizeof(buf));
    pipe_lines_write(pipe_ctx, buf, n);
}

int n_users = 0; // "refcount" users of below pcre2_code
// pcre2_code data is read-only, nothing is stored in it after the initial compilation.
// That means they are safe to share.
static pcre2_code* pragma_asmproc_recurse_code = NULL;
static pcre2_code* include_code = NULL;
#if REPLACE_FLOATS_WITH_IEEE754_IN_CUTSCENE_DATA
static pcre2_code* cutscene_data_code = NULL;
static pcre2_code* float_code = NULL;
#endif

static void regex_patterns_compile(void) {
    static_assert(INPUT_MUST_BE_UTF8);

    if (pragma_asmproc_recurse_code == NULL) {
        regex_utf_init_litpat("^\\s*#pragma asmproc recurse\\s*$", &pragma_asmproc_recurse_code);
        assert(pragma_asmproc_recurse_code != NULL);
    }

    if (include_code == NULL) {
        regex_utf_init_litpat("^\\s*#include .(.*).$", &include_code);
        assert(include_code != NULL);
    }

#if REPLACE_FLOATS_WITH_IEEE754_IN_CUTSCENE_DATA
    if (cutscene_data_code == NULL) {
        regex_utf_init_litpat("CutsceneData .*\\[\\] = {", &cutscene_data_code);
        assert(cutscene_data_code != NULL);
    }

    if (float_code == NULL) {
        regex_utf_init_litpat("[-+]?[0-9]*\\.?[0-9]+(?:[eE][-+]?[0-9]+)?f", &float_code);
        assert(float_code != NULL);
    }
#endif
}

static void regex_patterns_free(void) {
    pcre2_code_free(pragma_asmproc_recurse_code);
    pragma_asmproc_recurse_code = NULL;
    pcre2_code_free(include_code);
    include_code = NULL;
    pcre2_code_free(cutscene_data_code);
    cutscene_data_code = NULL;
    pcre2_code_free(float_code);
    float_code = NULL;
}

struct process_line_context {
    struct pipe_context* pipe_ctx;
    char* in_dir;
    char* in_file_basename;
    size_t outbuf_sz;
    void* outbuf;
    bool is_early_include;
    pcre2_match_data* pragma_asmproc_recurse_match_data;
    pcre2_match_data* include_match_data;
#if REPLACE_FLOATS_WITH_IEEE754_IN_CUTSCENE_DATA
    bool is_cutscene_data;
    pcre2_match_data* cutscene_data_match_data;
    pcre2_match_data* float_match_data;
#endif
};

struct process_line_context* process_line_ctx_create(struct pipe_context* pipe_ctx, char* in_dir,
                                                     char* in_file_basename) {
    size_t outbuf_sz = 32 * 1024;
    byte* outbuf = malloc(outbuf_sz);
    assert(outbuf != NULL);

    regex_patterns_compile();

    pcre2_match_data* pragma_asmproc_recurse_match_data =
        pcre2_match_data_create_from_pattern(pragma_asmproc_recurse_code, NULL);
    assert(pragma_asmproc_recurse_match_data != NULL);

    pcre2_match_data* include_match_data = pcre2_match_data_create_from_pattern(include_code, NULL);
    assert(include_match_data != NULL);

#if REPLACE_FLOATS_WITH_IEEE754_IN_CUTSCENE_DATA
    pcre2_match_data* cutscene_data_match_data = pcre2_match_data_create_from_pattern(cutscene_data_code, NULL);
    assert(cutscene_data_match_data != NULL);

    pcre2_match_data* float_match_data = pcre2_match_data_create_from_pattern(float_code, NULL);
    assert(float_match_data != NULL);
#endif

    struct process_line_context* ctx = malloc(sizeof(struct process_line_context));
    assert(ctx != NULL);
    memset(ctx, 0, sizeof(struct process_line_context));
    ctx->pipe_ctx = pipe_ctx;
    ctx->in_dir = in_dir;
    ctx->in_file_basename = in_file_basename;
    ctx->outbuf_sz = outbuf_sz;
    ctx->outbuf = outbuf;
    ctx->is_early_include = false;
    ctx->pragma_asmproc_recurse_match_data = pragma_asmproc_recurse_match_data;
    ctx->include_match_data = include_match_data;
#if REPLACE_FLOATS_WITH_IEEE754_IN_CUTSCENE_DATA
    ctx->is_cutscene_data = false;
    ctx->cutscene_data_match_data = cutscene_data_match_data;
    ctx->float_match_data = float_match_data;
#endif
    n_users += 1;
    return ctx;
}

void process_line_ctx_free(struct process_line_context* ctx) {
    free(ctx->outbuf);
    pcre2_match_data_free(ctx->pragma_asmproc_recurse_match_data);
    pcre2_match_data_free(ctx->include_match_data);
#if REPLACE_FLOATS_WITH_IEEE754_IN_CUTSCENE_DATA
    pcre2_match_data_free(ctx->cutscene_data_match_data);
    pcre2_match_data_free(ctx->float_match_data);
#endif
    free(ctx);
    n_users -= 1;
    if (n_users <= 0) {
        n_users = 0;
        regex_patterns_free();
    }
}

static bool process_line_pragma_asmproc_recurse(struct process_line_context* ctx, int line_num, byte** line_p,
                                                size_t* line_sz_p) {
    int match_ret;
    byte* line = *line_p;
    size_t line_sz = *line_sz_p;

    static_assert(INPUT_MUST_BE_UTF8);
    match_ret =
        pcre2_match(pragma_asmproc_recurse_code, line, line_sz, 0, 0, ctx->pragma_asmproc_recurse_match_data, NULL);
    if (match_ret == 1) {
        ctx->is_early_include = true;
        *line_p = NULL;
        *line_sz_p = 0;
    } else {
        if (match_ret != PCRE2_ERROR_NOMATCH) {
            PCRE2_UCHAR8 buf[1024];
            pcre2_get_error_message(match_ret, buf, sizeof(buf));
            fprintf(stderr, "pragma_asmproc_recurse_code match_ret=%d  %.*s\n", match_ret, (int)sizeof(buf), buf);
        }
        assert(match_ret == PCRE2_ERROR_NOMATCH);

        if (ctx->is_early_include) {
            ctx->is_early_include = false;

            static_assert(INPUT_MUST_BE_UTF8);
            match_ret = pcre2_match(include_code, line, line_sz, 0, 0, ctx->include_match_data, NULL);
            if (match_ret == 2) {
                size_t* include_ovector = pcre2_get_ovector_pointer(ctx->include_match_data);
                size_t include_filename_start = include_ovector[2];
                size_t include_filename_end = include_ovector[3];
                assert(include_filename_start <= include_filename_end);
                assert(include_filename_end <= line_sz);

                size_t include_filename_sz = include_filename_end - include_filename_start;
                char include_filename[include_filename_sz];
                memcpy(include_filename, line + include_filename_start, include_filename_sz);
                include_filename[include_filename_sz] = '\0';

                fprintf(stderr, "include_filename = %s\n", include_filename);
                char include_file_path[PATH_MAX];
                int len =
                    snprintf(include_file_path, sizeof(include_file_path), "%s/%s", ctx->in_dir, include_filename);
                assert(len < sizeof(include_file_path));
                FILE* include_file_f = fopen(include_file_path, "r");
                assert(include_file_f != NULL);
                void* ctx_recurse = process_line_ctx_create(ctx->pipe_ctx, ctx->in_dir, include_filename);
                assert(ctx_recurse != NULL);
                writelinedirective(ctx->pipe_ctx, 1, include_filename);
                pipe_lines(ctx->pipe_ctx, include_file_f, process_line, ctx_recurse);
                process_line_ctx_free(ctx_recurse);
                ctx_recurse = NULL;
                writelinedirective(ctx->pipe_ctx, line_num + 1, ctx->in_file_basename);
                *line_p = NULL;
                *line_sz_p = 0;
                return true;
            } else if (match_ret == PCRE2_ERROR_NOMATCH) {
                fprintf(stderr, "#pragma asmproc recurse must be followed by an #include\n");
                abort();
            } else {
                assert(match_ret < 0);
                PCRE2_UCHAR8 buf[1024];
                pcre2_get_error_message(match_ret, buf, sizeof(buf));
                fprintf(stderr, "pragma_asmproc_recurse_code match_ret=%d  %.*s\n", match_ret, (int)sizeof(buf), buf);
                abort();
            }
        }
    }
    return false;
}

#if REPLACE_FLOATS_WITH_IEEE754_IN_CUTSCENE_DATA
static void process_line_cutscene_data(struct process_line_context* ctx, byte** line_p, size_t* line_sz_p) {
    int match_ret;
    byte* line = *line_p;
    size_t line_sz = *line_sz_p;

    static_assert(INPUT_MUST_BE_UTF8);
    match_ret = pcre2_match(cutscene_data_code, line, line_sz, 0, 0, ctx->cutscene_data_match_data, NULL);

    if (match_ret == 1) {
        ctx->is_cutscene_data = true;
    } else {
        if (match_ret != PCRE2_ERROR_NOMATCH) {
            PCRE2_UCHAR8 buf[1024];
            pcre2_get_error_message(match_ret, buf, sizeof(buf));
            fprintf(stderr, "csdata match_ret=%d  %.*s\n", match_ret, (int)sizeof(buf), buf);
        }
        assert(match_ret == PCRE2_ERROR_NOMATCH);

        if (ctx->is_cutscene_data) {
            static_assert(INPUT_MUST_BE_UTF8);
            char cutscene_data_end[] = "};";
            if (memmem(line, line_sz, cutscene_data_end, sizeof(cutscene_data_end) - 1) != NULL) {
                ctx->is_cutscene_data = false;
            }
        }
    }

    if (ctx->is_cutscene_data) {
        size_t line_offset = 0;
        size_t outlen = 0;
        // TODO realloc buffer if too small (very much "just in case",
        //       it only needs to be able to hold one line...)
        byte* outbuf = ctx->outbuf;
        size_t outremaining = ctx->outbuf_sz;

        while (1) {
            assert(line_offset <= line_sz);
            match_ret = pcre2_match(float_code, line, line_sz, line_offset, 0, ctx->float_match_data, NULL);

            if (match_ret == 1) {
                size_t* float_ovector = pcre2_get_ovector_pointer(ctx->float_match_data);
                size_t float_start = float_ovector[0];
                size_t float_end = float_ovector[1];
                assert(float_start <= float_end);
                assert(float_end <= line_sz);

                TRACE("matched float: %.*s\n", (int)(float_end - float_start), line + float_start);

                char* tail;
                // FIXME strtof depends on the locale
                float f = strtof((char*)line + float_start, &tail);
                assert(tail == (char*)line + float_end - 1);
                assert(*tail == 'f');

                static_assert(sizeof(float) == 4);
                // FIXME assumes host IEEE-754
                uint32_t fbin = (union {
                                    float vf;
                                    uint32_t vi;
                                }){ .vf = f }
                                    .vi;

                size_t frag_up_to_float_sz = float_start - line_offset;
                assert(frag_up_to_float_sz <= outremaining);
                memcpy(outbuf + outlen, line + line_offset, frag_up_to_float_sz);
                outlen += frag_up_to_float_sz;
                outremaining -= frag_up_to_float_sz;

                int len = snprintf((char*)outbuf + outlen, outremaining, "0x%" PRIX32, fbin);
                assert(len < outremaining); // assert buffer was large enough and snprintf didn't truncate
                outlen += len;
                outremaining -= len;

                line_offset = float_end;
            } else {
                if (match_ret != PCRE2_ERROR_NOMATCH) {
                    PCRE2_UCHAR8 buf[1024];
                    pcre2_get_error_message(match_ret, buf, sizeof(buf));
                    fprintf(stderr, "float match_ret=%d  %.*s\n", match_ret, (int)sizeof(buf), buf);
                }
                assert(match_ret == PCRE2_ERROR_NOMATCH);
                break;
            }
        }

        if (line_offset == 0) {
            // no floats
        } else {
            assert(line_offset <= line_sz);
            size_t frag_up_to_end_sz = line_sz - line_offset;
            assert(frag_up_to_end_sz <= outremaining);
            memcpy(outbuf + outlen, line + line_offset, frag_up_to_end_sz);
            outlen += frag_up_to_end_sz;
            outremaining -= frag_up_to_end_sz;

            TRACE("float in %.*s", (int)line_sz, line);
            TRACE("      -> %.*s", (int)outlen, outbuf);

            *line_p = outbuf;
            *line_sz_p = outlen;
        }
    }
}
#endif

void process_line(void* arg0, int line_num, byte** line_p, size_t* line_sz_p) {
    struct process_line_context* ctx = arg0;
    byte* line = *line_p;
    size_t line_sz = *line_sz_p;

    TRACE("%.*s", (int)line_sz, line);

    assert(line_sz >= 1);
    static_assert(INPUT_MUST_BE_UTF8);
    assert(line[line_sz - 1] == '\n');

    if (process_line_pragma_asmproc_recurse(ctx, line_num, line_p, line_sz_p))
        return;

#if REPLACE_FLOATS_WITH_IEEE754_IN_CUTSCENE_DATA
    process_line_cutscene_data(ctx, line_p, line_sz_p);
#endif
}
