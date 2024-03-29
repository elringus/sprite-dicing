use crate::fs::common::*;
use crate::fs::json;
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
    /// JSON strings of diced sprites with data to reconstruct source spites: mesh, uvs, etc.
    pub sprites: String,
}

/// Packs specified raw sprites into raw atlas textures and sprite meshes serialized in JSON.
///
/// # Arguments
///
/// * `sprites`: Raw sprites to pack.
/// * `prefs`: Dicing operation preferences.
/// * `fmt`: Format of the generated raw atlas textures.
///
/// returns: Generated raw assets when operation successful, [Error] otherwise.
pub fn dice_raw(sprites: &[RawSprite], prefs: &Prefs, fmt: &AtlasFormat) -> Result<RawArtifacts> {
    let sprites = sprites.iter().map(decode_raw).collect::<Result<Vec<_>>>()?;
    let diced = crate::dice(&sprites, prefs)?;
    let atlases = diced
        .atlases
        .into_iter()
        .map(|a| encode_raw(a, fmt.image()))
        .collect::<Result<Vec<_>>>()?;
    let sprites = json::sprites_to_json(&diced.sprites);
    Ok(RawArtifacts { atlases, sprites })
}

fn encode_raw(texture: Texture, fmt: ImageFormat) -> Result<Vec<u8>> {
    let img = texture.to_image()?;
    let mut buf = Cursor::new(Vec::new());
    write_image(img, fmt, &mut buf)?;
    Ok(buf.into_inner())
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
