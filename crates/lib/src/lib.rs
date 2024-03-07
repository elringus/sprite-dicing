//! Provides API for generating atlas textures and diced sprite meshes from/to raw pixels.
//! When `fs` features is enabled, additionally provides APIs to read and decode various
//! texture formats from the file system as well as to write generated atlases into textures
//! of various formats and diced sprite meshes into OBJ files.

#[cfg(feature = "fs")]
mod fs;
mod models;

#[cfg(feature = "fs")]
pub use fs::*;
pub use models::*;
use std::error::Error;

pub fn dice(sprites: Vec<SourceSprite>, prefs: Prefs) -> Result<DiceResult, Box<dyn Error>> {
    Ok(DiceResult {
        atlases: Vec::new(),
        sprites: Vec::new(),
    })
}
