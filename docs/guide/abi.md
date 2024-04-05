# ABI

SpriteDicing has a C/C++ ABI wrapper compiled into native library for embedding the tool into game engines, frameworks or languages.

```rust
extern "C" fn dice(sprites: CSlice<CSourceSprite>, prefs: CPrefs) -> CResult
```

::: warning
Memory governed by the slices returned by the function is expected to be reclaimed by the caller.
:::

For the full API see the sources:

- [abi/lib.rs](https://github.com/elringus/sprite-dicing/blob/main/crates/abi/src/lib.rs)
- [abi/models.rs](https://github.com/elringus/sprite-dicing/blob/main/crates/abi/src/models.rs)

Download latest pre-compiled binaries:

- [Windows x64](https://github.com/elringus/sprite-dicing/releases/latest/download/sprite_dicing.dll)
- [macOS ARM](https://github.com/elringus/sprite-dicing/releases/latest/download/sprite_dicing.dylib)
- [Linux x64](https://github.com/elringus/sprite-dicing/releases/latest/download/sprite_dicing.so)

To build from source, clone the [repository](https://github.com/elringus/sprite-dicing) and run `cargo build` under "crates" directory. You may also find this [build script](https://github.com/elringus/sprite-dicing/tree/main/scripts/build.ps1) useful for cross-compilation.

::: tip
For the ABI usage example, see the [C# bindings](https://github.com/elringus/sprite-dicing/blob/main/plugins/unity/Assets/SpriteDicing/Editor/Native/Native.cs) authored for the Unity integration.
:::
