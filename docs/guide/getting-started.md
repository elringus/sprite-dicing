# Getting Started

SpriteDicing comes in 3 forms: native C ABI library for embedding into other applications, such as game engines or frameworks, standalone CLI executable to use the tool directly and Unity game engine package.

Download artifacts for the latest release via the links below:

|     | Windows                                                                                                         | macOS                                                                                                         | Linux                                                                                                   |
|-----|-----------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|
| ABI | [sprite_dicing.dll](https://github.com/elringus/sprite-dicing/releases/latest/download/sprite_dicing.dll)       | [sprite_dicing.dylib](https://github.com/elringus/sprite-dicing/releases/latest/download/sprite_dicing.dylib) | [sprite_dicing.so](https://github.com/elringus/sprite-dicing/releases/latest/download/sprite_dicing.so) |
| CLI | [dice-windows-x64.exe](https://github.com/elringus/sprite-dicing/releases/latest/download/dice-windows-x64.exe) | [dice-mac-arm](https://github.com/elringus/sprite-dicing/releases/latest/download/dice-mac-arm)               | [dice-linux-x64](https://github.com/elringus/sprite-dicing/releases/latest/download/dice-linux-x64)     |

To build from source, clone the [repository](https://github.com/elringus/sprite-dicing) and run `cargo build` under "crates" directory. You may also find this [build script](https://github.com/elringus/sprite-dicing/tree/main/scripts/build.ps1) useful for cross-compilation.

SpriteDicing is also available on Rust's package manager: [crates.io/crates/sprite_dicing](https://crates.io/crates/sprite_dicing).

For more information on how to use each type of the artifact, refer to the dedicated guide:

- [Rust crate](/guide/api)
- [Native C ABI library](/guide/abi)
- [CLI Tool](/guide/cli)
- [Unity integration](/guide/unity)
