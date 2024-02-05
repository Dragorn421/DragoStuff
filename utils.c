// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#define _GNU_SOURCE       // for strlcpy
#define _XOPEN_SOURCE 500 // for realpath

#include <stddef.h> // for size_t
#include <stdlib.h> // for abort, realpath
#include <stdio.h>  // for fprintf, abort, perror
#include <string.h> // for strdup, strlcpy
#include <limits.h> // for PATH_MAX
#include <libgen.h> // for dirname, basename

#include "utils.h"

void regex_utf_init(const PCRE2_UCHAR* pattern, size_t pattern_sz, pcre2_code** code_p) {
    int errorcode;
    size_t erroroffset;

    assert(pattern != NULL);
    assert(code_p != NULL);

    // TODO PCRE2_MATCH_INVALID_UTF, PCRE2_NO_UTF_CHECK for speed
    pcre2_code* code = pcre2_compile(pattern, pattern_sz, PCRE2_UTF, &errorcode, &erroroffset, NULL);
    if (code == NULL) {
        PCRE2_UCHAR buf[1024];
        pcre2_get_error_message(errorcode, buf, sizeof(buf));
        static_assert(sizeof(PCRE2_UCHAR) == 1,
                      "printf is for single-byte code units. Must use iconv, wchar_t and/or %ls otherwise");
        fprintf(stderr, "pcre2_compile error at %zd: %.*s\n", erroroffset, (int)sizeof(buf), buf);
        fprintf(stderr, "%.*s\n", (int)pattern_sz, pattern);
        fprintf(stderr, "%*c\n", (int)erroroffset + 1, '^');
        abort();
    }

    *code_p = code;
}

/**
 * Resolve and split f_path="path/to/thing" into "/resolved/path/to" and "thing"
 * The strings returned to f_dirname_p and f_basename_p should be freed by the caller.
 */
void split_path(char* f_path, char** f_dirname_p, char** f_basename_p) {
    char buf[PATH_MAX];

    assert(f_path != NULL);

    char* in_file_resolved = realpath(f_path, buf);
    if (in_file_resolved == NULL) {
        perror("split_path: realpath");
        abort();
    }
    in_file_resolved = strdup(in_file_resolved);
    assert(in_file_resolved != NULL);

    strlcpy(buf, in_file_resolved, sizeof(buf));
    char* f_dirname = strdup(dirname(buf));
    assert(f_dirname != NULL);

    strlcpy(buf, in_file_resolved, sizeof(buf));
    char* f_basename = strdup(basename(buf));
    assert(f_basename != NULL);

    free(in_file_resolved);
    if (f_dirname_p != NULL)
        *f_dirname_p = f_dirname;
    if (f_basename_p != NULL)
        *f_basename_p = f_basename;
}
