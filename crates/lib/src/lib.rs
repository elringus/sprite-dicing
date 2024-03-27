//! APIs for generating atlas textures and diced sprite meshes from/to raw pixels.
//! When `fs` feature is enabled, additionally provides APIs to read and decode textures of
//! various formats from the file system as well as to write generated atlases into textures
//! of various formats and to serialize diced sprite meshes as JSON files.

mod builder;
mod dicer;
mod fixtures;
#[cfg(feature = "fs")]
mod fs;
mod models;
mod packer;

#[cfg(feature = "fs")]
pub use fs::*;
pub use models::*;

/// Splits specified sprite textures into chunks, discards identical ones, joins unique
/// chunks into atlas textures and generates sprite meshes with texture coordinates mapped
/// to the atlas textures allowing to render original sprites without the source textures.
///
/// # Arguments
///
/// * `sprites`: Source sprite textures to dice.
/// * `prefs`: User preferences for the dicing operation.
///
/// returns: Generated atlas textures and diced sprite meshes or [Error].
///
/// # Examples
///
/// ```
/// use sprite_dicing::{Prefs, SourceSprite, Texture, Pixel};
///
/// // Fake function to load textures (images).
/// fn load (path: &str) -> Texture {
///     let red = Pixel::new(255, 0, 0, 255);
///     let blue = Pixel::new(0, 0, 255, 255);
///     Texture { width: 2, height: 2, pixels: vec![red, blue, blue, red] }
/// }
///
/// // Fake function to save textures.
/// fn save (path: &str, tex: &Texture) { }
///
/// // Collect source sprites to dice.
/// let sprites = vec![
///     SourceSprite { id: "1".to_owned(), texture: load("1.png"), pivot: None },
///     SourceSprite { id: "2".to_owned(), texture: load("2.png"), pivot: None },
///     // ...
/// ];
///
/// // Dice source sprites with default preferences.
/// let diced = sprite_dicing::dice(&sprites, &Prefs::default()).unwrap();
///
/// // Write generated atlas textures to file system.
/// for (index, atlas) in diced.atlases.iter().enumerate() {
///     save(&format!("atlas_{index}.png"), atlas);
/// }
///
/// // Build diced sprites using generated data.
/// for sprite in diced.sprites {
///     // Unique ID as set in the associated source sprite.
///     _ = sprite.id;
///     // Atlas texture containing all the unique pixels for the sprite.
///     _ = &diced.atlases[sprite.atlas_index];
///     // Mesh vertex positions in local space units.
///     _ = sprite.vertices;
///     // Atlas texture coordinates mapped to the mesh vertices.
///     _ = sprite.uvs;
///     // Mesh quad faces as indices to the vertices array.
///     _ = sprite.indices;
///     // Sprite rect in local space units.
///     _ = sprite.rect;
///     // ... (actual sprite asset building process is engine-specific)
/// }
/// ```
pub fn dice(sprites: &[SourceSprite], prefs: &Prefs) -> Result<Artifacts> {
    let diced = dicer::dice(sprites, prefs)?;
    let packed = packer::pack(diced, prefs)?;
    let sprites = builder::build(&packed, prefs)?;
    let atlases = packed.into_iter().map(|p| p.texture).collect();
    Ok(Artifacts { atlases, sprites })
}
