// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#ifndef CRWASMP_PROCESS_LINE_H
#define CRWASMP_PROCESS_LINE_H

#include <stddef.h> // for size_t

#include "common.h"     // for byte
#include "pipe_lines.h" // for struct pipe_context

void writelinedirective(struct pipe_context* pipe_ctx, int line, char* filename);

struct process_line_context;

struct process_line_context* process_line_ctx_create(struct pipe_context* pipe_ctx, char* in_dir,
                                                     char* in_file_basename);
void process_line_ctx_free(struct process_line_context* ctx);

void process_line(void* arg0, int line_num, byte** line_p, size_t* line_sz_p);

#endif
