#![cfg(feature = "fs")]

//! Provides additional APIs to the main lib for dicing sprites stored as images of various
//! formats on file system, writing generated atlases as images of specified format and
//! sprite meshes as JSON files.

use image::error::{DecodingError, ImageFormatHint};
use image::{ImageError, ImageFormat};
use std::{fs, path::Path};

use crate::models::*;

/// Preferences for dicing operations involving image encoding and file system access.
#[derive(Debug, Clone)]
pub struct FsPrefs<'a> {
    /// Directory path to write generated data; will use input directory when not specified.
    out: Option<&'a Path>,
    /// Whether to recursively search for textures inside input directory; false by default.
    recursive: bool,
    /// When recursive enabled, will use the separator when building sprite IDs; '/' by default.
    separator: String,
    /// Format to encode generated atlas textures into.
    atlas_format: AtlasFormat,
}

impl Default for FsPrefs<'_> {
    fn default() -> Self {
        Self {
            out: None,
            recursive: false,
            separator: "/".to_owned(),
            atlas_format: AtlasFormat::PNG,
        }
    }
}

/// Supported encode formats for atlas textures.
#[derive(Debug, Copy, Clone, Eq, PartialEq)]
pub enum AtlasFormat {
    PNG,
    JPEG,
    WEBP,
    TGA,
    DDS,
    TIFF,
}

impl AtlasFormat {
    /// File extension of the format.
    pub fn extension(&self) -> &'static str {
        match self {
            AtlasFormat::PNG => "png",
            AtlasFormat::JPEG => "jpeg",
            AtlasFormat::WEBP => "webp",
            AtlasFormat::TGA => "tga",
            AtlasFormat::DDS => "dds",
            AtlasFormat::TIFF => "tiff",
        }
    }
}

/// Packs all the textures of supported formats inside directory with specified path and
/// writes generated atlas textures and diced sprite meshes serialized in JSON.
///
/// # Arguments
///
/// * `dir`: Directory to look for textures to pack.
/// * `fs_prefs`: FS-related preferences: out directory, atlas format, etc.
/// * `prefs`: Dicing-related preferences: unit size, padding, PPU, etc.
///
/// returns: [Ok] when operation successful or [Error].
pub fn dice_in_dir(dir: &Path, fs_prefs: &FsPrefs, prefs: &Prefs) -> Result<()> {
    let sources = collect_sources(dir, fs_prefs)?;
    let diced = crate::dice(&sources, prefs)?;
    let out_dir = fs_prefs.out.unwrap_or(dir);
    write_atlases(diced.atlases, out_dir, &fs_prefs.atlas_format)?;
    write_sprites(diced.sprites, out_dir)
}

fn collect_sources(dir: &Path, prefs: &FsPrefs) -> Result<Vec<SourceSprite>> {
    let mut sprites = vec![];
    for entry in fs::read_dir(dir)? {
        if entry.is_err() {
            continue;
        }
        let path = &entry.unwrap().path();
        if path.is_dir() && prefs.recursive {
            sprites.extend(collect_sources(path, prefs)?);
        } else if path.is_file() && is_supported_texture(path) {
            sprites.push(create_sprite(dir, path, prefs)?);
        }
    }

    Ok(sprites)
}

fn is_supported_texture(path: &Path) -> bool {
    match ImageFormat::from_path(path) {
        Ok(fmt) => matches!(
            fmt,
            ImageFormat::Png
                | ImageFormat::Jpeg
                | ImageFormat::WebP
                | ImageFormat::Tga
                | ImageFormat::Dds
                | ImageFormat::Tiff
        ),
        Err(_) => false,
    }
}

fn create_sprite(dir: &Path, path: &Path, prefs: &FsPrefs) -> Result<SourceSprite> {
    let id = eval_sprite_id(dir, path, &prefs.separator);
    let texture = load_texture(path)?;
    let pivot = None;
    Ok(SourceSprite { id, texture, pivot })
}

fn load_texture(path: &Path) -> Result<Texture> {
    let img = image::io::Reader::open(path)?.decode()?;
    if let Some(img) = img.as_rgba8() {
        Ok(Texture::from_image(img))
    } else {
        Err(Error::Image(ImageError::Decoding(DecodingError::new(
            ImageFormatHint::Unknown,
            format!("Texture at '{}' is not RGBA8.", path.display()),
        ))))
    }
}

fn eval_sprite_id(dir: &Path, path: &Path, separator: &str) -> String {
    path.with_extension("")
        .iter()
        .skip(dir.iter().count())
        .map(|o| o.to_str().unwrap_or(""))
        .collect::<Vec<_>>()
        .join(separator)
}

fn write_atlases(atlases: Vec<Texture>, out_dir: &Path, fmt: &AtlasFormat) -> Result<()> {
    atlases.into_iter().enumerate().try_for_each(|(idx, tex)| {
        let name = format!("atlas_{idx}.{}", fmt.extension());
        let path = out_dir.join(name);
        tex.to_image()?.save(path).map_err(Error::Image)
    })
}

fn write_sprites(sprites: Vec<DicedSprite>, out_dir: &Path) -> Result<()> {
    _ = sprites;
    _ = out_dir;
    todo!()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn can_eval_sprite_id_from_path() {
        assert_eq!(
            eval_sprite_id(Path::new("/foo/bar"), Path::new("/foo/bar/img.png"), "/"),
            "img"
        );
        assert_eq!(
            eval_sprite_id(Path::new("/foo"), Path::new("/foo/bar/img.png"), "/"),
            "bar/img"
        );
        assert_eq!(
            eval_sprite_id(Path::new("/"), Path::new("/foo/bar/img.png"), "/"),
            "foo/bar/img"
        );
    }
}
