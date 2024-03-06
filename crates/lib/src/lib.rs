//! Provides API for generating atlas textures and diced sprite meshes from/to raw pixels.
//! When `fs` features is enabled, additionally provides APIs to read and decode popular
//! texture formats from the file system as well as to write generated atlases into PNG and
//! diced sprite meshes into OBJ files.

#[cfg(feature = "fs")]
mod fs;

#[cfg(feature = "fs")]
pub use fs::*;

pub fn dice() {}
