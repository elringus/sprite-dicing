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
            results.insert(path.to_owned(), image.into_rgba8());
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
