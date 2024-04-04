use clap_derive::ValueEnum;
use std::path::PathBuf;

/// Result of a dicing operation executed via CLI.
pub type Result<T> = std::result::Result<T, Error>;

/// An issue a dicing operation executed via CLI.
#[derive(Debug)]
pub enum Error {
    /// An issue with dicing operation.
    Dicing(sprite_dicing::Error),
    /// An issue with texture encoding.
    Image(image::ImageError),
    /// An issue with an I/O operation.
    Io(std::io::Error),
}

impl std::fmt::Display for Error {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Error::Dicing(info) => write!(f, "{}", info),
            Error::Image(err) => write!(f, "{}", err),
            Error::Io(err) => write!(f, "{}", err),
        }
    }
}

impl From<std::io::Error> for Error {
    fn from(err: std::io::Error) -> Self {
        Error::Io(err)
    }
}

impl From<image::ImageError> for Error {
    fn from(err: image::ImageError) -> Self {
        Error::Image(err)
    }
}

impl std::error::Error for Error {}

/// Preferences for dicing operations involving file system access.
#[derive(Debug, Clone)]
pub struct FsPrefs {
    /// Directory path to write generated data; will use input directory when not specified.
    pub out: Option<PathBuf>,
    /// When recursive, will use the separator to join ID of nested sprites; false by default.
    pub recursive: bool,
    /// When recursive enabled, will use the separator when building sprite IDs; '/' by default.
    pub separator: String,
    /// Format to encode generated atlas textures into.
    pub atlas_format: AtlasFormat,
}

impl Default for FsPrefs {
    fn default() -> Self {
        Self {
            out: None,
            recursive: false,
            separator: "/".to_owned(),
            atlas_format: AtlasFormat::Png,
        }
    }
}

/// Supported encode formats for atlas textures.
#[derive(Debug, Copy, Clone, Eq, PartialEq, ValueEnum)]
pub enum AtlasFormat {
    Png,
    Webp,
    Tga,
}

impl AtlasFormat {
    /// File extension of the format.
    pub fn extension(&self) -> &'static str {
        match self {
            AtlasFormat::Png => "png",
            AtlasFormat::Webp => "webp",
            AtlasFormat::Tga => "tga",
        }
    }
}
