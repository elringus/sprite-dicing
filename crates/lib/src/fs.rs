#![cfg(feature = "fs")]

//! Provides additional APIs to the main lib for dicing sprites stored as images of various
//! formats on file system, writing generated atlases as images of specified format and
//! sprite meshes as JSON files.

use image::codecs::png::{CompressionType, FilterType, PngEncoder};
use image::error::{DecodingError, ImageFormatHint};
use image::{ImageError, ImageFormat};
use std::{fs, fs::File, io::BufWriter, path::Path};

use crate::models::*;

/// Preferences for dicing operations involving image encoding and file system access.
#[derive(Debug, Clone)]
pub struct FsPrefs<'a> {
    /// Directory path to write generated data; will use input directory when not specified.
    pub out: Option<&'a Path>,
    /// When recursive, will use the separator to join ID of nested sprites; false by default.
    pub recursive: bool,
    /// When recursive enabled, will use the separator when building sprite IDs; '/' by default.
    pub separator: String,
    /// Format to encode generated atlas textures into.
    pub atlas_format: AtlasFormat,
}

impl Default for FsPrefs<'_> {
    fn default() -> Self {
        Self {
            out: None,
            recursive: false,
            separator: "/".to_owned(),
            atlas_format: AtlasFormat::Png,
        }
    }
}

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
    let sources = collect_sources(dir, dir, fs_prefs)?;
    let diced = crate::dice(&sources, prefs)?;
    let out_dir = fs_prefs.out.unwrap_or(dir);
    write_atlases(diced.atlases, out_dir, &fs_prefs.atlas_format)?;
    write_sprites(diced.sprites, out_dir)
}

fn collect_sources(root: &Path, dir: &Path, prefs: &FsPrefs) -> Result<Vec<SourceSprite>> {
    let mut sprites = vec![];
    for entry in fs::read_dir(dir)? {
        let path = entry?.path();
        if path.is_dir() && prefs.recursive {
            sprites.extend(collect_sources(root, &path, prefs)?);
        } else if path.is_file() && is_supported_texture(&path) {
            sprites.push(create_sprite(root, &path, prefs)?);
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

fn create_sprite(root: &Path, path: &Path, prefs: &FsPrefs) -> Result<SourceSprite> {
    let id = eval_sprite_id(root, path, &prefs.separator);
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

fn eval_sprite_id(root: &Path, path: &Path, separator: &str) -> String {
    path.with_extension("")
        .iter()
        .skip(root.iter().count())
        .map(|o| o.to_str().unwrap_or(""))
        .collect::<Vec<_>>()
        .join(separator)
}

fn write_atlases(atlases: Vec<Texture>, out_dir: &Path, fmt: &AtlasFormat) -> Result<()> {
    atlases.into_iter().enumerate().try_for_each(|(idx, tex)| {
        let name = format!("atlas_{idx}.{}", fmt.extension());
        write_atlas(tex, &out_dir.join(name))
    })
}

fn write_atlas(texture: Texture, path: &Path) -> Result<()> {
    let img = texture.to_image()?;
    if ImageFormat::from_path(path)? == ImageFormat::Png {
        let mut buf = &mut BufWriter::new(File::create(path)?);
        let e = PngEncoder::new_with_quality(&mut buf, CompressionType::Best, FilterType::Adaptive);
        img.write_with_encoder(e).map_err(Error::Image)
    } else {
        img.save(path).map_err(Error::Image)
    }
}

fn write_sprites(sprites: Vec<DicedSprite>, out_dir: &Path) -> Result<()> {
    let json = sprites_to_json(&sprites);
    let path = out_dir.join("sprites.json");
    fs::write(path, json).map_err(Error::Io)
}

fn sprites_to_json(sprites: &[DicedSprite]) -> String {
    let sprites = sprites
        .iter()
        .map(sprite_to_json)
        .collect::<Vec<_>>()
        .join(",");

    format!("[{sprites}\n]\n")
}

fn sprite_to_json(sprite: &DicedSprite) -> String {
    let id = &sprite.id;
    let atlas = sprite.atlas_index;
    let vertices = sprite
        .vertices
        .iter()
        .map(|v| format!(r#"{{ "x": {}, "y": {} }}"#, v.x, v.y))
        .collect::<Vec<_>>()
        .join(", ");
    let uvs = sprite
        .uvs
        .iter()
        .map(|uv| format!(r#"{{ "u": {}, "v": {} }}"#, uv.u, uv.v))
        .collect::<Vec<_>>()
        .join(", ");
    let indices = sprite
        .indices
        .iter()
        .map(|i| i.to_string())
        .collect::<Vec<_>>()
        .join(", ");
    let x = sprite.rect.x;
    let y = sprite.rect.y;
    let width = sprite.rect.width;
    let height = sprite.rect.height;

    format!(
        r#"
    {{
        "id": "{id}",
        "atlas": {atlas},
        "vertices": [{vertices}],
        "uvs": [{uvs}],
        "indices": [{indices}],
        "rect": {{ "x": {x}, "y": {y}, "width": {width}, "height": {height} }}
    }}"#
    )
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn evaluates_sprite_id_from_path() {
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

    #[test]
    fn builds_sprites_json() {
        let sprites = [
            DicedSprite {
                id: "foo/bar/img".to_owned(),
                atlas_index: 0,
                vertices: vec![Vertex::new(1.0, -2.0), Vertex::new(-3.0, 4.525)],
                uvs: vec![UV::new(0.1, 0.2), UV::new(0.3, 0.4)],
                indices: vec![1, 2, 3],
                rect: Rect::new(0.5, 0.5, 100.0, 50.0),
            },
            DicedSprite {
                id: "img".to_owned(),
                atlas_index: 1,
                vertices: vec![Vertex::new(-1.0, 2.0)],
                uvs: vec![UV::new(0.01, 0.02)],
                indices: vec![0],
                rect: Rect::new(-1.5, 0.0, 0.0, 10.10),
            },
        ];
        assert_eq!(
            sprites_to_json(&sprites),
            r#"[
    {
        "id": "foo/bar/img",
        "atlas": 0,
        "vertices": [{ "x": 1, "y": -2 }, { "x": -3, "y": 4.525 }],
        "uvs": [{ "u": 0.1, "v": 0.2 }, { "u": 0.3, "v": 0.4 }],
        "indices": [1, 2, 3],
        "rect": { "x": 0.5, "y": 0.5, "width": 100, "height": 50 }
    },
    {
        "id": "img",
        "atlas": 1,
        "vertices": [{ "x": -1, "y": 2 }],
        "uvs": [{ "u": 0.01, "v": 0.02 }],
        "indices": [0],
        "rect": { "x": -1.5, "y": 0, "width": 0, "height": 10.1 }
    }
]
"#
        );
    }
}
