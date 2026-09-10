// C ABI of the SpriteDicing library: https://dicing.elringus.com/guide/abi

#ifndef SPRITE_DICING_H
#define SPRITE_DICING_H

#include <stddef.h>
#include <stdint.h>

#ifndef __cplusplus
#include <stdbool.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

// 8-bit RGBA pixel.
typedef struct sd_pixel {
    uint8_t r;
    uint8_t g;
    uint8_t b;
    uint8_t a;
} sd_pixel;

// Texture of `width * height` pixels, left to right, top to bottom.
typedef struct sd_texture {
    uint32_t width;
    uint32_t height;
    const sd_pixel* pixels;
} sd_texture;

// Relative (0.0 to 1.0) offset from the top-left corner of the sprite rect.
typedef struct sd_pivot {
    float x;
    float y;
} sd_pivot;

// Sprite to dice; `id` is NUL-terminated UTF-8, unique among the sprites of the operation.
// When `has_pivot` is false, default pivot from the prefs is used.
typedef struct sd_source_sprite {
    const char* id;
    sd_texture texture;
    bool has_pivot;
    sd_pivot pivot;
} sd_source_sprite;

// Progress of the operation; `ratio` is 0.0 to 1.0, `activity` is NUL-terminated UTF-8
// valid only for the duration of the callback.
typedef struct sd_progress {
    float ratio;
    const char* activity;
} sd_progress;

// Invoked on progress updates with `user_data` from the prefs.
typedef void (*sd_progress_callback)(sd_progress progress, void* user_data);

// Preferences of the operation; see `sprite_dicing::Prefs` for the details.
// `on_progress` may be NULL; `user_data` is passed to it as is.
typedef struct sd_prefs {
    uint32_t unit_size;
    uint32_t padding;
    float uv_inset;
    bool trim_transparent;
    uint32_t atlas_size_limit;
    bool atlas_square;
    bool atlas_pot;
    float ppu;
    sd_pivot pivot;
    sd_progress_callback on_progress;
    void* user_data;
} sd_prefs;

// Position of a mesh vertex, in conventional units.
typedef struct sd_vertex {
    float x;
    float y;
} sd_vertex;

// Texture coordinates, in 0.0 to 1.0 range.
typedef struct sd_uv {
    float u;
    float v;
} sd_uv;

// Rectangle in conventional units.
typedef struct sd_rect {
    float x;
    float y;
    float width;
    float height;
} sd_rect;

// Generated sprite; `uvs` are mapped to `vertices` and `vertex_count` applies to both,
// `indices` are triangles into them, `atlas_index` refers to `sd_result.atlases`.
typedef struct sd_diced_sprite {
    const char* id;
    size_t atlas_index;
    const sd_vertex* vertices;
    const sd_uv* uvs;
    size_t vertex_count;
    const uint32_t* indices;
    size_t index_count;
    sd_rect rect;
    sd_pivot pivot;
} sd_diced_sprite;

// Result of the operation, owned by the library; release with `sd_free`.
// `error` is NULL on success; empty arrays are NULL.
typedef struct sd_result {
    const char* error;
    const sd_texture* atlases;
    size_t atlas_count;
    const sd_diced_sprite* sprites;
    size_t sprite_count;
} sd_result;

// Dices the sprites; the result, successful or not, has to be released with `sd_free`.
// The input is not accessed after the call; `sprites` may be NULL when the count is zero.
sd_result sd_dice(const sd_source_sprite* sprites, size_t count, const sd_prefs* prefs);

// Releases a result returned from `sd_dice`; a zero-initialized result is ignored.
void sd_free(sd_result result);

#ifdef __cplusplus
}
#endif

#endif // SPRITE_DICING_H
