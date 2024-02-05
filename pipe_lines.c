// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#include <stdlib.h> // for malloc, free, abort
#include <stdio.h>  // for FILE, fread, fwrite, feof, ferror, perror, fprintf, stderr
#include <string.h> // for memset, memchr, memmove
#include <stddef.h> // for size_t
#include <stdbool.h>
#include <errno.h>
#include <assert.h>

#include <iconv.h>

#include "common.h"
#include "pipe_lines.h"

struct pipe_context {
    FILE* f_out;
    iconv_t conversion_descriptor;
    size_t outbuf_sz;
    char* outbuf;
    size_t outbuf_len;
};

struct pipe_context* pipe_lines_ctx_create(FILE* f_out, iconv_t conversion_descriptor) {
    size_t outbuf_sz = 32 * 1024;
    char* outbuf = malloc(outbuf_sz);
    assert(outbuf != NULL);

    struct pipe_context* pipe_ctx = malloc(sizeof(struct pipe_context));
    memset(pipe_ctx, 0, sizeof(struct pipe_context));
    pipe_ctx->f_out = f_out;
    pipe_ctx->conversion_descriptor = conversion_descriptor;
    pipe_ctx->outbuf_sz = outbuf_sz;
    pipe_ctx->outbuf = outbuf;
    pipe_ctx->outbuf_len = 0;
    return pipe_ctx;
}

void pipe_lines_ctx_free(struct pipe_context* pipe_ctx) {
    free(pipe_ctx->outbuf);
    free(pipe_ctx);
}

void pipe_lines_write_outbuf(struct pipe_context* pipe_ctx) {
    FILE* f_out = pipe_ctx->f_out;

    // Write (all of) outbuf
    char* write_p = pipe_ctx->outbuf;
    size_t write_n = pipe_ctx->outbuf_len;
    while (write_n != 0) {
        size_t nwritten = fwrite(write_p, 1, write_n, f_out);
        if (nwritten == 0) {
            if (ferror(f_out)) {
                perror("fwrite");
                assert(false);
            }
        }
        write_p += nwritten;
        write_n -= nwritten;
    }
    pipe_ctx->outbuf_len = 0;
}

// TODO find better name, expecting buf to be fully encodable (no partial code point)
void pipe_lines_write(struct pipe_context* pipe_ctx, void* buf, size_t len) {
    iconv_t conversion_descriptor = pipe_ctx->conversion_descriptor;

    // Convert encoding
    // https://www.gnu.org/software/libc/manual/html_node/iconv-Examples.html
    char* inbuf = buf;
    size_t inbytesleft = len;
    while (inbytesleft != 0) {
        char* outbufp = pipe_ctx->outbuf + pipe_ctx->outbuf_len;
        size_t outbytesleft = pipe_ctx->outbuf_sz - pipe_ctx->outbuf_len;
        size_t iconv_ret = iconv(conversion_descriptor, &inbuf, &inbytesleft, &outbufp, &outbytesleft);
        pipe_ctx->outbuf_len = pipe_ctx->outbuf_sz - outbytesleft;
        if (iconv_ret == (size_t)-1) {
            switch (errno) {
                case EILSEQ: // invalid multibyte sequence in input
                case EINVAL: // incomplete multibyte sequence in input
                default:
                    perror("pipe_lines_write: iconv");
                    abort();
                    break;

                case E2BIG: // output buffer full
                    pipe_lines_write_outbuf(pipe_ctx);
                    break;
            }
        } else {
            assert(inbytesleft == 0);
        }
    }
}

void pipe_lines_flush(struct pipe_context* pipe_ctx) {
    iconv_t conversion_descriptor = pipe_ctx->conversion_descriptor;

    char* outbufp = pipe_ctx->outbuf + pipe_ctx->outbuf_len;
    size_t outbytesleft = pipe_ctx->outbuf_sz - pipe_ctx->outbuf_len;
    size_t iconv_ret;
    iconv_ret = iconv(conversion_descriptor, NULL, NULL, &outbufp, &outbytesleft);
    if (iconv_ret == (size_t)-1) {
        if (errno == E2BIG) {
            pipe_lines_write_outbuf(pipe_ctx);
            iconv_ret = iconv(conversion_descriptor, NULL, NULL, &outbufp, &outbytesleft);
            if (iconv_ret == (size_t)-1) {
                perror("pipe_lines_flush: 2nd iconv");
                abort();
            }
        } else {
            perror("pipe_lines_flush: iconv");
            abort();
        }
    }
    pipe_lines_write_outbuf(pipe_ctx);
}

void pipe_lines(struct pipe_context* pipe_ctx, FILE* f_in, process_line_callback_fn process_line_cb, void* arg0) {
    byte inbuf[32 * 1024];
    size_t inbuf_len = 0;
    bool finish = false;
    int line_num = 1;

    int niter = 0;
    while (!finish) {
        niter++;
        TRACE("niter=%d inbuf_remaining=%zd\n", niter, sizeof(inbuf) - inbuf_len);

        assert(sizeof(inbuf) - inbuf_len != 0);
        size_t nread = fread(inbuf + inbuf_len, 1, sizeof(inbuf) - inbuf_len, f_in);
        inbuf_len += nread;
        finish = feof(f_in);

        TRACE("nread=%zd finish=%d\n", nread, (int)finish);

        byte* inbuf_prev_line_end = inbuf;
        while (1) {
            byte* inbuf_line_start = inbuf_prev_line_end;
            byte* inbuf_line_end;

            size_t remaining_bytes = inbuf + inbuf_len - inbuf_prev_line_end;

            TRACE("inbuf_len=%zd prev_line_offset=%zd remaining_bytes=%zd\n", inbuf_len, inbuf_prev_line_end - inbuf,
                  remaining_bytes);
            if (remaining_bytes == 0) {
                inbuf_len = 0;
                break;
            }

            static_assert(INPUT_MUST_BE_UTF8);
            /*
            a '\n' byte in a utf-8 string always encodes the \n character
            (i.e. it cannot be a byte part of encoding another code point)
            this means finding a \n character is finding a '\n' byte (faster)
            */
            byte* lf_p = memchr(inbuf_prev_line_end, '\n', remaining_bytes);
            if (lf_p == NULL) {
                TRACE("no \\n found\n");
                // no \n found
                if (finish) {
                    // set the "line" end at end of file and add a \n
                    static_assert(INPUT_MUST_BE_UTF8);
                    inbuf_line_end = inbuf_line_start + remaining_bytes + 1;
                    assert(&inbuf_line_end[-1] < inbuf + sizeof(inbuf)); // hope there is one byte available for the \n
                    inbuf_line_end[-1] = '\n';
                } else {
                    // truncated line, wait to read more

                    // assert part of inbuf was processed or there's still room in inbuf
                    // (otherwise there is a line in in_f longer than inbuf can hold...)
                    assert(inbuf_prev_line_end != inbuf || sizeof(inbuf) - inbuf_len != 0);

                    memmove(inbuf, inbuf_prev_line_end, remaining_bytes);
                    inbuf_len = remaining_bytes;
                    break;
                }
            } else {
                // line ends after the \n
                inbuf_line_end = lf_p + 1;
            }

            byte* line = inbuf_line_start;
            size_t line_sz = inbuf_line_end - inbuf_line_start;
            process_line_cb(arg0, line_num, &line, &line_sz);
            if (memchr(line, 0, line_sz) != NULL) {
                fprintf(stderr, "line_sz=%zd strlen=%zd\n", line_sz, strlen((char*)line));
                fprintf(stderr, "%.*s\n", (int)line_sz, (char*)line);
                abort();
            }

            if (line != NULL) {
                pipe_lines_write(pipe_ctx, line, line_sz);
            }

            inbuf_prev_line_end = inbuf_line_end;
            line_num += 1;
        }
    }
}
