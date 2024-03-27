//! Provides additional APIs to the main lib for dicing sprites stored as images of various
//! formats on file system, writing generated atlases as images of specified format and
//! sprite meshes as JSON files.

mod common;
mod dir;
mod json;
mod raw;

pub use common::AtlasFormat;
pub use dir::{dice_dir, FsPrefs};
pub use raw::{dice_raw, RawArtifacts, RawSprite};
