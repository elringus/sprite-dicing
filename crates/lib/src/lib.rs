//! Provides APIs for generating atlas textures and diced sprite meshes from/to raw pixels.
//! When `fs` feature is enabled, additionally provides APIs to read and decode textures of
//! various formats from the file system as well as to save generated atlases into textures
//! of various formats and to write diced sprite meshes into Wavefront OBJ files.

mod dicer;
#[cfg(feature = "fs")]
mod fs;
mod models;

#[cfg(feature = "fs")]
pub use fs::*;
pub use models::*;
use std::error::Error;

/// Splits specified sprite textures into chunks, discards identical ones, joins unique
/// chunks into atlas textures and generates sprite meshes with texture coordinates mapped
/// to the atlas textures allowing to render original sprites without the source textures.
///
/// # Arguments
///
/// * `sprites`: Source sprite textures to dice.
/// * `prefs`: User preferences for the dicing operation.
///
/// returns: Generated atlas textures and diced sprite meshes or error.
///
/// # Examples
///
/// ```no_run
/// use sprite_dicing::{dice, SourceSprite, Prefs};
/// use image::{open}; // https://crates.io/crates/image
///
/// // Collect source sprite textures to dice.
/// let source_sprites = vec![
///     SourceSprite { id: "1".to_owned(), texture: open("1.png")?, pivot: None },
///     SourceSprite { id: "2".to_owned(), texture: open("2.png")?, pivot: None },
///     // ...
/// ];
///
/// // Dice source sprites with default preferences.
/// let generated = dice(source_sprites, Prefs::default())?;
///
/// // Write generated atlas textures to file system.
/// for (index, atlas) in generated.atlases.iter().enumerate() {
///     atlas.save(format!("atlas_{index}.png"))?;
/// }
///
/// // Build sprites from the generated meshes.
/// for sprite in generated.sprites {
///     // Unique ID as set in the associated source sprite.
///     _ = sprite.id;
///     // Atlas texture containing all the unique pixels for the sprite.
///     _ = &generated.atlases[sprite.atlas_index];
///     // Mesh vertex positions in local space (scaled by PPU specified in prefs).
///     _ = sprite.vertices;
///     // Atlas texture coordinates mapped to the mesh vertices.
///     _ = sprite.uvs;
///     // Mesh triangle faces as indices to the vertices array.
///     _ = sprite.indices;
///     // Sprite origin point location in local space.
///     _ = sprite.pivot;
///     // ... (actual sprite asset building process is engine-specific)
/// }
/// ```
pub fn dice(sprites: Vec<SourceSprite>, prefs: Prefs) -> Result<DiceResult, Box<dyn Error>> {
    let dicer = dicer::new(&prefs);
    let diced = sprites.iter().map(|s| dicer.dice(s)).collect::<Vec<_>>();
    _ = diced;
    Ok(DiceResult {
        atlases: Vec::new(),
        sprites: Vec::new(),
    })
}
