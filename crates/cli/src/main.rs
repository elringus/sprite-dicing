//! Command line interface of the library.

use clap::{Parser, ValueEnum};
use sprite_dicing::{AtlasFormat, FsPrefs, Pivot, Prefs, Result};
use std::path::PathBuf;

#[derive(Debug, Parser)]
struct Args {
    /// Input directory to look for textures to pack.
    dir: PathBuf,
    /// Directory path to write generated data.
    #[arg(short, long)]
    out: Option<PathBuf>,
    /// Recursively search for textures inside input directory.
    #[arg(short, long, default_value_t = false)]
    recursive: bool,
    /// When recursive, the separator to join ID of nested sprites.
    #[arg(long, default_value = "/")]
    separator: String,
    /// Format of the generated atlas textures.
    #[arg(short, long, value_enum, default_value_t = Format::Png)]
    format: Format,
    /// The size of a single diced unit, in pixels.
    #[arg(short, long, default_value_t = 64)]
    pub size: u32,
    /// The size of border between adjacent diced units, in pixels.
    #[arg(short, long, default_value_t = 2)]
    pub pad: u32,
    /// Relative inset (in 0.0-1.0 range) of the diced units UV coordinates.
    #[arg(short, long, default_value_t = 0.0)]
    pub inset: f32,
    /// Discarding fully-transparent dices.
    #[arg(short, long, default_value_t = true)]
    pub trim: bool,
    /// Maximum size of a single generated atlas texture.
    #[arg(short, long, default_value_t = 2048)]
    pub limit: u32,
    /// Force atlas size to always be square.
    #[arg(long, default_value_t = false)]
    pub square: bool,
    /// Force atlas size to always be power of two.
    #[arg(long, default_value_t = false)]
    pub pot: bool,
    /// Pixel per unit ratio of the diced sprite mesh vertices.
    #[arg(long, default_value_t = 100.0)]
    pub ppu: f32,
    /// Origin of the diced sprite mesh, in relative offsets from top-left corner.
    #[arg(long, num_args = 2)]
    pub pivot: Option<Vec<f32>>,
}

#[derive(Debug, Clone, ValueEnum)]
pub enum Format {
    Png,
    Jpeg,
    Webp,
    Tga,
    Tiff,
}

impl From<Format> for AtlasFormat {
    fn from(value: Format) -> Self {
        match value {
            Format::Png => AtlasFormat::Png,
            Format::Jpeg => AtlasFormat::Jpeg,
            Format::Webp => AtlasFormat::Webp,
            Format::Tga => AtlasFormat::Tga,
            Format::Tiff => AtlasFormat::Tiff,
        }
    }
}

fn main() -> Result<()> {
    let args = Args::parse();
    let fs_prefs = FsPrefs {
        out: args.out.as_deref(),
        recursive: args.recursive,
        separator: args.separator,
        atlas_format: args.format.into(),
    };
    let prefs = Prefs {
        unit_size: args.size,
        padding: args.pad,
        uv_inset: args.inset,
        trim_transparent: args.trim,
        atlas_size_limit: args.limit,
        atlas_square: args.square,
        atlas_pot: args.pot,
        ppu: args.ppu,
        pivot: if args.pivot.is_some() {
            let pivot = args.pivot.unwrap();
            Some(Pivot::new(pivot[0], pivot[1]))
        } else {
            None
        },
    };
    sprite_dicing::dice_in_dir(&args.dir, &fs_prefs, &prefs)
}
