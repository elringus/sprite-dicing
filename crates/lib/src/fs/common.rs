use image::codecs::png::{CompressionType, FilterType, PngEncoder};
use image::{ImageFormat, RgbaImage};

pub(crate) use crate::models::*;

/// Supported encode formats for atlas textures.
#[derive(Debug, Copy, Clone, Eq, PartialEq)]
pub enum AtlasFormat {
    Png,
    Jpeg,
    Webp,
    Tga,
    Tiff,
}

impl AtlasFormat {
    /// File extension of the format.
    pub fn extension(&self) -> &'static str {
        match self {
            AtlasFormat::Png => "png",
            AtlasFormat::Jpeg => "jpeg",
            AtlasFormat::Webp => "webp",
            AtlasFormat::Tga => "tga",
            AtlasFormat::Tiff => "tiff",
        }
    }

    pub fn image(&self) -> ImageFormat {
        match self {
            AtlasFormat::Png => ImageFormat::Png,
            AtlasFormat::Jpeg => ImageFormat::Jpeg,
            AtlasFormat::Webp => ImageFormat::WebP,
            AtlasFormat::Tga => ImageFormat::Tga,
            AtlasFormat::Tiff => ImageFormat::Tiff,
        }
    }
}

pub(crate) fn write_image<T>(img: RgbaImage, fmt: ImageFormat, buffer: &mut T) -> Result<()>
where
    T: std::io::Write + std::io::Seek,
{
    match fmt {
        ImageFormat::Png => img
            .write_with_encoder(PngEncoder::new_with_quality(
                buffer,
                CompressionType::Best,
                FilterType::Adaptive,
            ))
            .map_err(Error::Image),
        fmt => img.write_to(buffer, fmt).map_err(Error::Image),
    }
}
