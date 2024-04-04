use image::RgbaImage;
use sprite_dicing::{Pixel, Texture};
use std::collections::HashMap;
use std::fs;
use std::path::{Path, PathBuf};

pub fn load_all<T>(dir: &T) -> HashMap<PathBuf, RgbaImage>
where
    T: AsRef<Path>,
{
    let mut results = HashMap::new();

    for entry in fs::read_dir(dir).unwrap() {
        let path = entry.unwrap().path();
        if path.is_dir() {
            results.extend(load_all(&path));
            continue;
        }

        if let Ok(image) = image::open(&path) {
            let mut image = image.into_rgba8();
            normalize_clear(&mut image);
            results.insert(path.to_owned(), image);
        }
    }

    results
}

pub fn to_texture(img: &RgbaImage) -> Texture {
    Texture {
        width: img.width(),
        height: img.height(),
        pixels: img.pixels().map(rgba_to_pixel).collect(),
    }
}

pub fn from_texture(tex: &Texture) -> RgbaImage {
    let buf = tex.pixels.iter().flat_map(|p| Pixel::to_raw(*p)).collect();
    RgbaImage::from_raw(tex.width, tex.height, buf).unwrap()
}

pub fn is_clear(img: &RgbaImage) -> bool {
    img.pixels().all(|p| p.0[3] == 0)
}

fn rgba_to_pixel(rgba: &image::Rgba<u8>) -> Pixel {
    Pixel::from_raw(rgba.0)
}

fn normalize_clear(img: &mut RgbaImage) -> &RgbaImage {
    // Transparent (a=0) pixels may have random rgb readings,
    // so normalize them to rgb=0 for consistency in repro asserts.
    for pixel in img.pixels_mut() {
        if pixel.0[3] == 0 {
            pixel.0[0] = 0;
            pixel.0[1] = 0;
            pixel.0[2] = 0;
        }
    }
    img
}
