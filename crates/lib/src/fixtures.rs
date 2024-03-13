#![cfg(test)]

use crate::models::*;
use once_cell::sync::Lazy;

pub const RED: Pixel = Pixel::new(255, 0, 0, 255);
pub const GREEN: Pixel = Pixel::new(0, 255, 0, 255);
pub const BLUE: Pixel = Pixel::new(0, 0, 255, 255);
pub const BLACK: Pixel = Pixel::new(0, 0, 0, 255);
pub const CLEAR: Pixel = Pixel::new(0, 0, 0, 0);

pub static B: Lazy<Texture> = Lazy::new(|| load("1x1/B"));
pub static R: Lazy<Texture> = Lazy::new(|| load("1x1/R"));
pub static BGRT: Lazy<Texture> = Lazy::new(|| load("2x2/BGRT"));
pub static BTGR: Lazy<Texture> = Lazy::new(|| load("2x2/BTGR"));
pub static BTGT: Lazy<Texture> = Lazy::new(|| load("2x2/BTGT"));
pub static TTTT: Lazy<Texture> = Lazy::new(|| load("2x2/TTTT"));
pub static RGB1X3: Lazy<Texture> = Lazy::new(|| load("RGB1x3"));
pub static RGB3X1: Lazy<Texture> = Lazy::new(|| load("RGB3x1"));
pub static RGB4X4: Lazy<Texture> = Lazy::new(|| load("RGB4x4"));
pub static UIC4X4: Lazy<Texture> = Lazy::new(|| load("UIC4x4"));

fn load(name: &'static str) -> Texture {
    let lib_dir = env!("CARGO_MANIFEST_DIR");
    let path = format!("{lib_dir}/../tests/fixtures/{name}.png");
    let img = image::open(path).unwrap();
    Texture {
        width: img.width() as u16,
        height: img.height() as u16,
        pixels: img
            .as_rgba8()
            .unwrap()
            .pixels()
            .map(|p| Pixel::new(p[0], p[1], p[2], p[3]))
            .collect(),
    }
}
