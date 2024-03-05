if (Test-Path dist) { rm -r dist }
if (Test-Path target) { rm -r target }
cargo build --release --target=x86_64-pc-windows-msvc
docker run --rm -t -v $pwd\:/io -w /io messense/cargo-zigbuild sh -c "
RUSTFLAGS='-C link-arg=-s' cargo zigbuild --release --target x86_64-unknown-linux-gnu &&
cargo zigbuild --release --target x86_64-apple-darwin &&
cargo zigbuild --release --target aarch64-apple-darwin"
md dist | Out-Null
cp target/x86_64-pc-windows-msvc/release/rustbox.exe dist/rustbox-windows-x64.exe
cp target/x86_64-unknown-linux-gnu/release/rustbox dist/rustbox-linux-x64
cp target/x86_64-apple-darwin/release/rustbox dist/rustbox-mac-x64
cp target/aarch64-apple-darwin/release/rustbox dist/rustbox-mac-arm
