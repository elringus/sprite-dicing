#![cfg(test)]

use crate::models::*;
use once_cell::sync::Lazy;

pub const R: Pixel = Pixel::new(255, 0, 0, 255);
pub const G: Pixel = Pixel::new(0, 255, 0, 255);
pub const B: Pixel = Pixel::new(0, 0, 255, 255);
pub const C: Pixel = Pixel::new(0, 0, 0, 0);

pub static B1X1: Lazy<Texture> = Lazy::new(|| tex(1, 1, vec![B]));
pub static R1X1: Lazy<Texture> = Lazy::new(|| tex(1, 1, vec![R]));
#[rustfmt::skip]
pub static BGRC: Lazy<Texture> = Lazy::new(|| tex(2, 2, vec![
    B, G,
    R, C
]));
#[rustfmt::skip]
pub static BCGR: Lazy<Texture> = Lazy::new(|| tex(2, 2, vec![
    B, C,
    G, R
]));
#[rustfmt::skip]
pub static BCGC: Lazy<Texture> = Lazy::new(|| tex(2, 2, vec![
    B, C,
    G, C
]));
#[rustfmt::skip]
pub static CCCC: Lazy<Texture> = Lazy::new(|| tex(2, 2, vec![
    C, C,
    C, C
]));
#[rustfmt::skip]
pub static RGB1X3: Lazy<Texture> = Lazy::new(|| tex(1, 3, vec![
    G,
    R,
    B
]));
#[rustfmt::skip]
pub static RGB3X1: Lazy<Texture> = Lazy::new(|| tex(3, 1, vec![
    G, R, B
]));
#[rustfmt::skip]
pub static RGB4X4: Lazy<Texture> = Lazy::new(|| tex(4, 4, vec![
    B, G, G, G,
    R, R, G, B,
    R, G, B, R,
    B, B, R, G,
]));
pub static UIC4X4: Lazy<Texture> = Lazy::new(|| uic(4, 4));

fn tex(width: u32, height: u32, pixels: Vec<Pixel>) -> Texture {
    Texture {
        width,
        height,
        pixels,
    }
}

fn uic(width: u32, height: u32) -> Texture {
    let mut pixels = Vec::new();
    for i in 0..=(width * height) as u8 {
        pixels.push(Pixel::new(i, i, i, 255))
    }
    tex(width, height, pixels)
}
