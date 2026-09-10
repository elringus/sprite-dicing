# ABI

SpriteDicing has a C ABI wrapper compiled into native library for embedding the tool into game engines, frameworks or languages. The contract is declared in the [sprite_dicing.h](https://github.com/elringus/sprite-dicing/blob/main/crates/abi/sprite_dicing.h) header and consists of two functions:

```c
sd_result sd_dice(const sd_source_sprite* sprites, size_t count, const sd_prefs* prefs);
void sd_free(sd_result result);
```

`sd_dice` is the counterpart of the [dice](/guide/api) function of the Rust crate: it takes source sprites with preferences and returns generated atlas textures and diced sprites. `sd_free` releases the result.

## Ownership

- The caller owns the input; it's not accessed after `sd_dice` returns.
- The library owns everything the result references; release it with `sd_free` exactly once, whether the operation succeeded or not.
- `error` is `NULL` on success; otherwise it describes the failure and the arrays are empty.
- Empty arrays have `NULL` pointer and zero count.
- Strings are NUL-terminated UTF-8.
- `activity` of the progress is valid only for the duration of the callback.

## Example

```c
#include <sprite_dicing.h>
#include <stdio.h>

int main(void) {
    const sd_pixel red = { 255, 0, 0, 255 };
    const sd_pixel blue = { 0, 0, 255, 255 };
    const sd_pixel pixels[] = { red, blue, blue, red };

    sd_source_sprite sprite = { 0 };
    sprite.id = "sprite";
    sprite.texture.width = 2;
    sprite.texture.height = 2;
    sprite.texture.pixels = pixels;

    sd_prefs prefs = { 0 };
    prefs.unit_size = 64;
    prefs.padding = 1;
    prefs.trim_transparent = true;
    prefs.atlas_size_limit = 2048;
    prefs.ppu = 100.0f;
    prefs.pivot.x = 0.5f;
    prefs.pivot.y = 0.5f;

    sd_result result = sd_dice(&sprite, 1, &prefs);
    if (result.error) {
        printf("Failed to dice: %s\n", result.error);
    } else {
        for (size_t i = 0; i < result.atlas_count; i++) {
            const sd_texture* atlas = &result.atlases[i];
            printf("Atlas %zu: %ux%u\n", i, atlas->width, atlas->height);
        }
        for (size_t i = 0; i < result.sprite_count; i++) {
            const sd_diced_sprite* diced = &result.sprites[i];
            printf("Sprite %s: %zu vertices on atlas %zu\n", diced->id, diced->vertex_count, diced->atlas_index);
        }
    }

    int failed = result.error != NULL;
    sd_free(result);
    return failed;
}
```

## Binaries

Download latest pre-compiled binaries:

- [Windows x64](https://github.com/elringus/sprite-dicing/releases/latest/download/sprite_dicing.dll)
- [macOS ARM](https://github.com/elringus/sprite-dicing/releases/latest/download/sprite_dicing.dylib)
- [Linux x64](https://github.com/elringus/sprite-dicing/releases/latest/download/sprite_dicing.so)

To build from source, clone the [repository](https://github.com/elringus/sprite-dicing) and run `cargo build` under "crates" directory. You may also find this [build script](https://github.com/elringus/sprite-dicing/tree/main/scripts/build.sh) useful for cross-compilation.

::: tip
For the ABI usage examples, see the [C# bindings](https://github.com/elringus/sprite-dicing/blob/main/plugins/unity/Assets/SpriteDicing/Editor/Native/Native.cs) authored for the Unity integration and the [C++ bindings](https://github.com/elringus/sprite-dicing/blob/main/plugins/godot/addons/sprite_dicing/editor/native/src/sprite_dicing.cpp) authored for the Godot integration.
:::
