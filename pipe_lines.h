// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#ifndef CRWASMP_PIPE_LINES_H
#define CRWASMP_PIPE_LINES_H

#include <stddef.h> // for size_t
#include <stdio.h>  // for FILE
#include <iconv.h>  // for iconv_t

struct pipe_context;

typedef unsigned char byte;

typedef void (*process_line_callback_fn)(void* arg0, int line_num, byte** line_p, size_t* line_sz_p);

struct pipe_context* pipe_lines_ctx_create(FILE* f_out, iconv_t conversion_descriptor);
void pipe_lines_ctx_free(struct pipe_context* pipe_ctx);

void pipe_lines_write(struct pipe_context* pipe_ctx, void* buf, size_t len);
void pipe_lines_flush(struct pipe_context* pipe_ctx);

void pipe_lines(struct pipe_context* pipe_ctx, FILE* f_in, process_line_callback_fn process_line_cb, void* arg0);

#endif
