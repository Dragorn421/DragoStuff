// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#define REPLACE_FLOATS_WITH_IEEE754_IN_CUTSCENE_DATA 1

#ifndef INPUT_MUST_BE_UTF8
// TODO also expecting -fexec-charset=utf-8
#error "Define INPUT_MUST_BE_UTF8 to 1 to acknowledge all input passed to the executable will be utf-8"
#endif

typedef unsigned char byte;

#ifdef NDEBUG
#define TRACE(...)
#else
#define TRACE(...) fprintf(stderr, __VA_ARGS__)
#endif
