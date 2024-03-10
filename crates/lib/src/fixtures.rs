#![cfg(test)]

use image::{Rgba, RgbaImage};
use once_cell::sync::Lazy;

pub const RED: Rgba<u8> = Rgba([255, 0, 0, 255]);
pub const GREEN: Rgba<u8> = Rgba([0, 255, 0, 255]);
pub const BLUE: Rgba<u8> = Rgba([0, 0, 255, 255]);
pub const BLACK: Rgba<u8> = Rgba([0, 0, 0, 255]);

pub static B: Lazy<RgbaImage> = Lazy::new(|| load("1x1/B"));
pub static R: Lazy<RgbaImage> = Lazy::new(|| load("1x1/R"));
pub static BGRT: Lazy<RgbaImage> = Lazy::new(|| load("2x2/BGRT"));
pub static BTGR: Lazy<RgbaImage> = Lazy::new(|| load("2x2/BTGR"));
pub static BTGT: Lazy<RgbaImage> = Lazy::new(|| load("2x2/BTGT"));
pub static TTTT: Lazy<RgbaImage> = Lazy::new(|| load("2x2/TTTT"));
pub static RGB1X3: Lazy<RgbaImage> = Lazy::new(|| load("RGB1x3"));
pub static RGB3X1: Lazy<RgbaImage> = Lazy::new(|| load("RGB3x1"));
pub static RGB4X4: Lazy<RgbaImage> = Lazy::new(|| load("RGB4x4"));
pub static UIC4X4: Lazy<RgbaImage> = Lazy::new(|| load("UIC4x4"));

fn load(name: &'static str) -> RgbaImage {
    let lib_dir = env!("CARGO_MANIFEST_DIR");
    let path = format!("{lib_dir}/../tests/fixtures/{name}.png");
    RgbaImage::from(image::open(path).unwrap())
}
