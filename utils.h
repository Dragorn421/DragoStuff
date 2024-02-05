// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#ifndef CRWASMP_UTILS_H
#define CRWASMP_UTILS_H

/*
 * strings
 */

#include <string.h> // for strcmp, strncmp, strlen

#define str_equal(a, b) (strcmp((a), (b)) == 0)
#define str_startswith(a, prefix) (strncmp((a), (prefix), strlen((prefix))) == 0)

/*
 * misc
 */

#define ARRAY_COUNT(arr) (sizeof(arr) / sizeof(arr[0]))

void split_path(char* f_path, char** f_dirname_p, char** f_basename_p);

/*
 * regex
 */

#include <stddef.h> // for size_t

#ifndef PCRE2_CODE_UNIT_WIDTH
#error "Must define PCRE2_CODE_UNIT_WIDTH before including utils.h, which includes pcre2.h"
#endif
#include <pcre2.h>

// Call regex_utf_init with a string literal char[] pattern (not char*).
// C string literals always include an additional trailing nul byte \0, hence the use of `sizeof(pattern) - 1`
// to exclude the nul byte.
#define regex_utf_init_litpat(pattern, code_p) regex_utf_init((PCRE2_UCHAR*)pattern, sizeof(pattern) - 1, code_p)

void regex_utf_init(const PCRE2_UCHAR* pattern, size_t pattern_sz, pcre2_code** code_p);

/*
 * cvector
 */

#define CVECTOR_LOGARITHMIC_GROWTH
// https://github.com/eteran/c-vector/blob/b06b4e286bb90ad0db839af57283cb56fe1f241a/cvector.h
#include "cvector.h"

#include <stddef.h> // for size_t

// dest += src
#define cvector_extend(dest, src)                                          \
    do {                                                                   \
        size_t cv_min_capacity__ = cvector_size(dest) + cvector_size(src); \
        if (cvector_capacity(dest) < cv_min_capacity__) {                  \
            cvector_grow(dest, 2 * cv_min_capacity__);                     \
        }                                                                  \
        for (int i = 0; i < cvector_size(src); i++) {                      \
            cvector_push_back(dest, src[i]);                               \
        }                                                                  \
    } while (0)

// dest += [vec[i], ..., vec[j-1]]  0 <= i <= j <= size(vec)
#define cvector_slice(dest, src, i, j)                         \
    do {                                                       \
        assert(0 <= i);                                        \
        assert(i <= j);                                        \
        assert(j <= cvector_size(src));                        \
        size_t cv_min_capacity__ = cvector_size(dest) + j - i; \
        if (cvector_capacity(dest) < cv_min_capacity__) {      \
            cvector_grow(dest, 2 * cv_min_capacity__);         \
        }                                                      \
        for (int k = i; k < j; k++) {                          \
            cvector_push_back(dest, src[k]);                   \
        }                                                      \
    } while (0)

#endif
