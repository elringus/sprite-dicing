//! Provides API for generating atlas textures and diced sprite meshes from/to raw pixels.
//! When `fs` features is enabled, additionally provides APIs to read and decode popular
//! texture formats from the file system as well as to write generated atlases into PNG and
//! diced sprite meshes into OBJ files.

#[cfg(feature = "fs")]
mod fs;
mod models;

#[cfg(feature = "fs")]
pub use fs::*;
pub use models::*;
use std::error::Error;

/// Dices specified sprites with  producing atlas textures containing unique
/// 
/// # Arguments 
/// 
/// * `sprites`: 
/// 
/// returns: Result<DiceResult, Box<dyn Error, Global>> 
/// 
/// # Examples 
/// 
/// ```
/// 
/// ```
pub fn dice(sprites: Vec<SourceSprite>) -> Result<DiceResult, Box<dyn Error>> {
    Ok(DiceResult {
        atlases: Vec::new(),
        sprites: Vec::new(),
    })
}
