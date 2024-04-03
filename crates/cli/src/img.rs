use crate::models::*;
use image::codecs::png::{CompressionType, FilterType, PngEncoder};
use image::error::ImageFormatHint;
use image::{ImageError, ImageFormat, RgbaImage};
use sprite_dicing::{Pixel, Texture};
use std::{fs::File, io::BufWriter, path::Path};

pub fn to_texture(img: &RgbaImage) -> Texture {
    Texture {
        width: img.width(),
        height: img.height(),
        pixels: img.pixels().map(rgba_to_pixel).collect(),
    }
}

pub fn from_texture(tex: Texture) -> Result<RgbaImage> {
    let buf = tex.pixels.into_iter().flat_map(Pixel::to_raw).collect();
    RgbaImage::from_raw(tex.width, tex.height, buf).ok_or(Error::Image(ImageError::Encoding(
        image::error::EncodingError::new(
            ImageFormatHint::Unknown,
            "Failed to convert texture into RGBA8 image.",
        ),
    )))
}

pub fn save(path: &Path, img: RgbaImage) -> Result<()> {
    let buf = &mut BufWriter::new(File::create(path)?);
    let fmt = ImageFormat::from_path(path)?;

    match fmt {
        ImageFormat::Png => img
            .write_with_encoder(PngEncoder::new_with_quality(
                buf,
                CompressionType::Best,
                FilterType::Adaptive,
            ))
            .map_err(Error::Image),
        fmt => img.write_to(buf, fmt).map_err(Error::Image),
    }
}

pub fn load(path: &Path) -> Result<RgbaImage> {
    Ok(image::open(path)?.into_rgba8())
}

pub fn supported(path: &Path) -> bool {
    match ImageFormat::from_path(path) {
        Ok(fmt) => matches!(
            fmt,
            ImageFormat::Png
                | ImageFormat::WebP
                | ImageFormat::Tga
                | ImageFormat::Dds
                | ImageFormat::Tiff
                | ImageFormat::Jpeg
                | ImageFormat::Bmp
        ),
        Err(_) => false,
    }
}

fn rgba_to_pixel(rgba: &image::Rgba<u8>) -> Pixel {
    Pixel::from_raw(rgba.0)
}
