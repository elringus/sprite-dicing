#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/../crates"

dist=target/dist
unity=../plugins/unity/Assets/SpriteDicing/Editor/Native
godot=../plugins/godot/addons/sprite_dicing/editor/native

rm -rf $dist
cargo build -p abi -p dicing --release --target x86_64-pc-windows-msvc
MSYS_NO_PATHCONV=1 docker run --rm -t -v "$PWD:/io" -w /io messense/cargo-zigbuild:0.23.3 sh -c "
cargo zigbuild -p abi -p dicing --release --target x86_64-unknown-linux-gnu --target aarch64-apple-darwin"

mkdir -p $dist
cp target/x86_64-pc-windows-msvc/release/abi.dll $dist/sprite_dicing.dll
cp target/x86_64-unknown-linux-gnu/release/libabi.so $dist/sprite_dicing.so
cp target/aarch64-apple-darwin/release/libabi.dylib $dist/sprite_dicing.dylib
cp abi/sprite_dicing.h $dist/sprite_dicing.h
cp target/x86_64-pc-windows-msvc/release/dicing.exe $dist/dice-windows-x64.exe
cp target/x86_64-unknown-linux-gnu/release/dicing $dist/dice-linux-x64
cp target/aarch64-apple-darwin/release/dicing $dist/dice-mac-arm

cp $dist/sprite_dicing.{dll,so,dylib} $unity/
cp $dist/sprite_dicing.{dll,so,dylib} $godot/bin/
cp $dist/sprite_dicing.h $godot/src/abi/

echo "Build finished."
