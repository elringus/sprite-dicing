#![cfg(test)]

use image::RgbaImage;

pub static B: &RgbaImage = load("1x1/B");

const fn load(name: &str) -> &RgbaImage {
    let lib_dir = env!("CARGO_MANIFEST_DIR");
    let path = format!("{lib_dir}/../tests/fixtures/{name}.png");
    image::open(path)
        .expect(&format!("Failed to load '{name}' fixture."))
        .as_rgba8()
        .expect(&format!("Fixture '{name}' is not RGBA8."))
}
