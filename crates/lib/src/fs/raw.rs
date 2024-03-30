use crate::fs::common::*;
use image::error::{DecodingError, ImageFormatHint};
use image::{ImageError, ImageFormat};
use std::io::Cursor;

/// Raw sprite specified as input for a dicing operation.
#[derive(Debug, Clone)]
pub struct RawSprite<'a> {
    /// Unique identifier of the sprite among others in a dicing operation.
    pub id: String,
    /// Raw bytes content of the texture.
    pub bytes: &'a [u8],
    /// Format of the sprite texture represented as file extension (w/o leading dot).
    pub format: String,
    /// Relative position of the sprite origin point on the generated mesh.
    /// When not specified, will use default pivot specified in [Prefs].
    pub pivot: Option<Pivot>,
}

/// Final raw products of a dicing operation.
#[derive(Debug, Clone)]
pub struct RawArtifacts {
    /// Raw bytes of atlas textures containing unique pixel content of the diced sprites.
    pub atlases: Vec<Vec<u8>>,
    /// Generated diced sprites with data to reconstruct source spites: mesh, uvs, etc.
    pub sprites: Vec<DicedSprite>,
}

/// Packs specified raw sprite textures into raw atlas textures.
///
/// # Arguments
///
/// * `sprites`: Raw sprite textures to pack.
/// * `prefs`: Dicing operation preferences.
/// * `fmt`: Format of the generated raw atlas textures.
///
/// returns: Generated raw assets when operation successful, [Error] otherwise.
pub fn dice_raw(sprites: &[RawSprite], prefs: &Prefs, fmt: &AtlasFormat) -> Result<RawArtifacts> {
    let total = sprites.len();
    let mut sources = Vec::with_capacity(total);
    for (idx, sprite) in sprites.iter().enumerate() {
        Progress::report(prefs, 0, idx, total, "Decoding source textures");
        sources.push(decode_raw(sprite)?);
    }

    let diced = crate::dice(&sources, prefs)?;

    let total = diced.atlases.len();
    let mut atlases = Vec::with_capacity(total);
    for (idx, atlas) in diced.atlases.into_iter().enumerate() {
        Progress::report(prefs, 4, idx, total, "Encoding atlases textures");
        atlases.push(encode_raw(atlas, fmt.image())?);
    }

    let sprites = diced.sprites;
    Ok(RawArtifacts { atlases, sprites })
}

fn decode_raw(raw: &RawSprite) -> Result<SourceSprite> {
    let fmt = ImageFormat::from_extension(&raw.format).ok_or(ImageError::Decoding(
        DecodingError::new(
            ImageFormatHint::Unknown,
            format!("Failed to resolve texture format from '{}'.", raw.format),
        ),
    ))?;
    let img = image::load_from_memory_with_format(raw.bytes, fmt)?;
    Ok(SourceSprite {
        id: raw.id.to_owned(),
        texture: Texture::from_dynamic(&img)?,
        pivot: raw.pivot.to_owned(),
    })
}

fn encode_raw(texture: Texture, fmt: ImageFormat) -> Result<Vec<u8>> {
    let img = texture.to_image()?;
    let mut buf = Cursor::new(Vec::new());
    write_image(img, fmt, &mut buf)?;
    Ok(buf.into_inner())
}
