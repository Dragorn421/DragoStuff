// SPDX-FileCopyrightText: 2026 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#include <assert.h>
#include <limits.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

struct Vec2i
{
    int x, y;
};

/**
 * Replace the color of 0-alpha pixels with the color of the nearest visible pixel.
 *
 * @param im A RGBA image of size width*height, with data laid out like {R0,G0,B0,A0,R1,G1,B1,A1,...}
 * @param out A buffer of size width*height*4 to write the resulting image to
 * @param iteration_limit Count of bleed iterations. If zero, iterate until all pixels are filled.
 * @param average_nearest_colors If true, average the nearest visible pixels colors when they are equally distant.
 *                               Otherwise arbitrarily pick one of the colors.
 * @return true on success
 */
bool alpha_bleeder(uint8_t *im, int width, int height, uint8_t *out, int iteration_limit, bool average_nearest_colors)
{
#define IMACCESS(im, x, y, channel) (im)[((y) * width + (x)) * 4 + (channel)]
#define ALPHA(im, x, y) IMACCESS(im, x, y, 3)

    /*
     * This algorithm consists in starting by copying `im` to `out`,
     * then iteratively paint 0-alpha pixels of `out` with the color
     * of the closest visible pixel (including the non-0 alpha) until
     * there are no 0-alpha pixels.
     * Then at the end the alpha channel is restored by copying
     * the alpha from `im` to `out`.
     */

    memcpy(out, im, width * height * 4);

    // The `closest` array tracks the pixels from which each 0-alpha
    // pixel got its color from.
    struct
    {
        struct Vec2i *coords;
        int n_coords;
    }* closest;
    struct Vec2i* expand_to;

    // allocate `closest` and `expand_to` in one call
    // so that they can be freed later with one call
    // as well. micro optimization
    closest = malloc(sizeof(*closest) * (height * width) + sizeof(*expand_to) * (height * width));
    expand_to = (struct Vec2i*)(closest + (height * width));

    if (closest == NULL)
        return false;

    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            closest[y * width + x].coords = NULL;

    for (int iter = 0; iteration_limit == 0 || iter < iteration_limit; iter++)
    {
        // Find all 0-alpha pixels with visible neighbours in `out`
        // (including 0-alpha pixels from `im` that were processed)
        // and store their coordinates into `expand_to`

        int expand_to_len = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (ALPHA(out, x, y) == 0)
                {
                    struct Vec2i neighbours[] = {
                        {x, y - 1},
                        {x + 1, y},
                        {x, y + 1},
                        {x - 1, y},
                    };
                    for (int i = 0; i < (int)(sizeof(neighbours) / sizeof(neighbours[0])); i++)
                    {
                        struct Vec2i *n = &neighbours[i];
                        if (n->x >= 0 && n->x < width && n->y >= 0 && n->y < height)
                        {
                            if (ALPHA(out, n->x, n->y) != 0)
                            {
                                expand_to[expand_to_len].x = x;
                                expand_to[expand_to_len].y = y;
                                expand_to_len++;
                                break;
                            }
                        }
                    }
                }
            }
        }

        // If there are no 0-alpha pixels left, we are done
        if (expand_to_len == 0)
            break;

        // For each 0-alpha pixel with visible neighbours,

        for (int j = 0; j < expand_to_len; j++)
        {
            int x = expand_to[j].x;
            int y = expand_to[j].y;
            int closest_pixels_buf_len = 1;
            struct Vec2i *closest_pixels = malloc(sizeof(closest_pixels[0]) * closest_pixels_buf_len);
            int n_closest_pixels = 0;
            int closest_pixels_dist_sq = INT_MAX;

            struct Vec2i neighbours[] = {
                {x, y - 1},
                {x + 1, y},
                {x, y + 1},
                {x - 1, y},
            };
            for (int i = 0; i < (int)(sizeof(neighbours) / sizeof(neighbours[0])); i++)
            {
                struct Vec2i *n = &neighbours[i];
                if (n->x >= 0 && n->x < width && n->y >= 0 && n->y < height)
                {
                    if (ALPHA(out, n->x, n->y) != 0)
                    {
                        struct Vec2i *coords;
                        int n_coords;
                        if (closest[n->y * width + n->x].coords == NULL)
                        {
                            coords = n;
                            n_coords = 1;
                        }
                        else
                        {
                            coords = closest[n->y * width + n->x].coords;
                            n_coords = closest[n->y * width + n->x].n_coords;
                        }
                        for (int k = 0; k < n_coords; k++)
                        {
                            struct Vec2i *nc = &coords[k];
                            int nc_dist_sq = (nc->x - x) * (nc->x - x) + (nc->y - y) * (nc->y - y);
                            if (nc_dist_sq < closest_pixels_dist_sq)
                            {
                                // closest_pixels = {nc}
                                closest_pixels[0] = *nc;
                                n_closest_pixels = 1;
                                closest_pixels_dist_sq = nc_dist_sq;
                            }
                            else if (nc_dist_sq == closest_pixels_dist_sq)
                            {
                                // Append nc to closest_pixels
                                if (n_closest_pixels == closest_pixels_buf_len)
                                {
                                    closest_pixels_buf_len *= 2;
                                    void *temp = realloc(closest_pixels, sizeof(closest_pixels[0]) * closest_pixels_buf_len);
                                    if (temp == NULL)
                                    {
                                        for (int y = 0; y < height; y++)
                                            for (int x = 0; x < width; x++)
                                                free(closest[y * width + x].coords);
                                        free(closest_pixels);
                                        free(closest);
                                        return false;
                                    }
                                    closest_pixels = temp;
                                }
                                closest_pixels[n_closest_pixels] = *nc;
                                n_closest_pixels++;
                            }
                        }
                    }
                }
            }

            assert(n_closest_pixels != 0);

            closest[y * width + x].coords = closest_pixels;
            closest[y * width + x].n_coords = n_closest_pixels;

            if (average_nearest_colors)
            {
                // Take color from the average color of the closest visible pixels
                int col[4] = {0, 0, 0, 0};
                for (int i = 0; i < n_closest_pixels; i++)
                    for (int chan = 0; chan < 4; chan++)
                        col[chan] += IMACCESS(out, closest_pixels[i].x, closest_pixels[i].y, chan);
                for (int chan = 0; chan < 4; chan++)
                    IMACCESS(out, x, y, chan) = col[chan] / n_closest_pixels;
            }
            else
            {
                // Take color from any one of the closest visible pixels
                for (int chan = 0; chan < 4; chan++)
                    IMACCESS(out, x, y, chan) = IMACCESS(out, closest_pixels[0].x, closest_pixels[0].y, chan);
            }
        }
    }

    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            free(closest[y * width + x].coords);

    free(closest);

    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            ALPHA(out, x, y) = ALPHA(im, x, y);

    return true;
}

#if 0
int main()
{
    int w = 32;
    int h = 128;
    uint8_t im[] = {
#include "test-image.inc.c"
    };
    uint8_t out[w * h * 4];
    return !alpha_bleeder(im, w, h, out, false);
}
#endif
