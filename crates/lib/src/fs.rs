#![cfg(feature = "fs")]

use std::fs;
use std::io;
use std::path::Path;

/// Dices all the textures of supported formats inside directory with specified path and
/// writes generated atlas texture and sprite meshes under the specified out directory.
pub fn dice_in_dir(dir: &Path, out: &Path) -> Result<String, io::Error> {
    let mut str = format!("Requested dice in: {} to: {}\n", dir.display(), out.display());
    for entry in fs::read_dir(dir)? {
        str.push_str(&format!("{}\n", entry?.path().display()));
    }
    Ok(str)
}
