cd crates
if (Test-Path target) { rm -r target }
cargo build -p abi --lib --release --target=x86_64-pc-windows-msvc
cargo build -p cli --release --target=x86_64-pc-windows-msvc
docker run --rm -t -v $pwd\:/io -w /io messense/cargo-zigbuild sh -c "
RUSTFLAGS='-C link-arg=-s' cargo zigbuild -p abi --lib --release \
--target x86_64-unknown-linux-gnu --target aarch64-apple-darwin &&
RUSTFLAGS='-C link-arg=-s' cargo zigbuild -p cli --release \
--target x86_64-unknown-linux-gnu --target aarch64-apple-darwin"
md target/dist | Out-Null
cp target/x86_64-pc-windows-msvc/release/abi.dll target/dist/sprite-dicing.dll
cp target/x86_64-unknown-linux-gnu/release/libabi.so target/dist/sprite-dicing.so
cp target/aarch64-apple-darwin/release/libabi.dylib target/dist/sprite-dicing.dylib
cp target/x86_64-pc-windows-msvc/release/cli.exe target/dist/dice-win.exe
cp target/x86_64-unknown-linux-gnu/release/cli target/dist/dice-lin
cp target/aarch64-apple-darwin/release/cli target/dist/dice-mac
