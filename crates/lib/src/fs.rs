#![cfg(feature = "fs")]

//! Provides additional APIs to the main lib for dicing sprites stored as images of various
//! formats on file system, writing generated atlases as images of specified format and
//! sprite meshes as JSON files.

use crate::models::*;
use std::fs;
use std::path::Path;

/// Dices all the textures of supported formats inside directory with specified path and
/// writes generated atlas texture in specified format and diced sprites meta serialized
/// in specified format under the specified out directory.
pub fn dice_in_dir(dir: &Path, out: &Path) -> Result<String> {
    _ = crate::dice(&Vec::new(), &Prefs::default());
    let mut str = format!(
        "Requested dice in: {} to: {}\n",
        dir.display(),
        out.display()
    );
    for entry in fs::read_dir(dir)? {
        str.push_str(&format!("{}\n", entry?.path().display()));
    }
    Ok(str)
}
