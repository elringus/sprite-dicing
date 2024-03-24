#![cfg(test)]

use crate::models::*;
use once_cell::sync::Lazy;

pub const R: Pixel = Pixel::new(255, 0, 0, 255);
pub const G: Pixel = Pixel::new(0, 255, 0, 255);
pub const B: Pixel = Pixel::new(0, 0, 255, 255);
pub const T: Pixel = Pixel::new(0, 0, 0, 0);
pub const C: Pixel = Pixel::new(0, 255, 255, 255);
pub const M: Pixel = Pixel::new(255, 0, 255, 255);
pub const Y: Pixel = Pixel::new(255, 255, 0, 255);

pub static R1X1: Lazy<Texture> = Lazy::new(|| tex(1, 1, vec![R]));
pub static G1X1: Lazy<Texture> = Lazy::new(|| tex(1, 1, vec![G]));
pub static B1X1: Lazy<Texture> = Lazy::new(|| tex(1, 1, vec![B]));
pub static C1X1: Lazy<Texture> = Lazy::new(|| tex(1, 1, vec![C]));
pub static M1X1: Lazy<Texture> = Lazy::new(|| tex(1, 1, vec![M]));
pub static Y1X1: Lazy<Texture> = Lazy::new(|| tex(1, 1, vec![Y]));
#[rustfmt::skip]
pub static BGRT: Lazy<Texture> = Lazy::new(|| tex(2, 2, vec![
    B, G,
    R, T
]));
#[rustfmt::skip]
pub static BTGR: Lazy<Texture> = Lazy::new(|| tex(2, 2, vec![
    B, T,
    G, R
]));
#[rustfmt::skip]
pub static BTGT: Lazy<Texture> = Lazy::new(|| tex(2, 2, vec![
    B, T,
    G, T
]));
#[rustfmt::skip]
pub static TTTM: Lazy<Texture> = Lazy::new(|| tex(2, 2, vec![
    T, T,
    T, M
]));
#[rustfmt::skip]
pub static TTTT: Lazy<Texture> = Lazy::new(|| tex(2, 2, vec![
    T, T,
    T, T
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
pub static PLT4X4: Lazy<Texture> = Lazy::new(|| palette(4, 4));

pub trait AnySource {
    fn texture(&self) -> Texture;
    fn pivot(&self) -> Option<Pivot>;
    fn sprite(&self) -> SourceSprite {
        SourceSprite {
            id: "TEST".to_string(),
            texture: self.texture(),
            pivot: self.pivot(),
        }
    }
}

impl AnySource for Lazy<Texture> {
    fn texture(&self) -> Texture {
        (&self as &Texture).to_owned()
    }
    fn pivot(&self) -> Option<Pivot> {
        None
    }
}

impl AnySource for (&Lazy<Texture>, (f32, f32)) {
    fn texture(&self) -> Texture {
        (&self.0 as &Texture).to_owned()
    }
    fn pivot(&self) -> Option<Pivot> {
        Some(Pivot {
            x: self.1 .0,
            y: self.1 .1,
        })
    }
}

fn tex(width: u32, height: u32, pixels: Vec<Pixel>) -> Texture {
    Texture {
        width,
        height,
        pixels,
    }
}

fn palette(width: u32, height: u32) -> Texture {
    let mut pixels = Vec::new();
    for i in 0..=(width * height) as u8 {
        pixels.push(Pixel::new(i, i, i, 255))
    }
    tex(width, height, pixels)
}
