//! Command line interface of the library.

use clap::{Parser, ValueEnum};
use sprite_dicing::{AtlasFormat, FsPrefs, Pivot, Prefs};
use std::path::PathBuf;

#[derive(Debug, Parser)]
struct Args {
    /// Input directory to look for textures to pack.
    dir: PathBuf,
    /// Directory path to write generated data; will use input directory when not specified.
    #[arg(short, long)]
    out: Option<PathBuf>,
    /// Whether to recursively search for textures inside input directory.
    #[arg(short, long, default_value_t = false)]
    recursive: bool,
    /// When recursive, will use the separator to join ID of nested sprites.
    #[arg(long, default_value = "/")]
    separator: String,
    /// Format to encode generated atlas textures into.
    #[arg(short, long, value_enum, default_value_t = Format::Png)]
    format: Format,
    /// The size of a single diced unit, in pixels. Larger values result in less generated mesh
    /// overhead, but may also diminish number of reused texture regions.
    #[arg(short, long, default_value_t = 64)]
    pub size: u32,
    /// The size of border, in pixels, to add between adjacent diced units inside atlas textures.
    /// Increase to prevent texture bleeding artifacts. Larger values consume more texture space,
    /// but yield better anti-bleeding results.
    #[arg(short, long, default_value_t = 2)]
    pub pad: u32,
    /// Relative inset (in 0.0-1.0 range) of the diced units UV coordinates. Can be used in
    /// addition to (or instead of) padding to prevent texture bleeding artifacts. Won't
    /// consume texture space, but higher values could visually distort the rendered sprite.
    #[arg(short, long, default_value_t = 0.0)]
    pub inset: f32,
    /// Improves compression ratio by discarding fully-transparent dices, but may also change
    /// sprite dimensions. Disable to preserve original sprite texture dimensions.
    #[arg(short, long, default_value_t = true)]
    pub trim: bool,
    /// Maximum size (width or height) of a single generated atlas texture; will generate
    /// multiple textures when the limit is reached.
    #[arg(short, long, default_value_t = 2048)]
    pub limit: u32,
    /// The generated atlas textures will always be square. Less efficient, but required for
    /// PVRTC compression.
    #[arg(long, default_value_t = false)]
    pub square: bool,
    /// The generated atlas textures will always have width and height be power of two.
    /// Extremely inefficient, but required by some older GPUs.
    #[arg(long, default_value_t = false)]
    pub pot: bool,
    /// Pixel per unit ratio to use when evaluating positions of the generated mesh vertices.
    /// Higher values will make sprite smaller in conventional space units.
    #[arg(long, default_value_t = 100.0)]
    pub ppu: f32,
    /// Origin of the generated mesh, in relative offsets from top-left corner of the sprite rect.
    /// When differs from the default (0,0), will offset vertices to center mesh around the pivot.
    #[arg(long, value_delimiter = ',', num_args = 2, default_values_t = [0.0, 0.0])]
    pub pivot: Vec<f32>,
}

#[derive(Debug, Clone, ValueEnum)]
pub enum Format {
    Png,
    Jpeg,
    Webp,
    Tga,
    Dds,
    Tiff,
}

impl From<Format> for AtlasFormat {
    fn from(value: Format) -> Self {
        match value {
            Format::Png => AtlasFormat::Png,
            Format::Jpeg => AtlasFormat::Jpeg,
            Format::Webp => AtlasFormat::Webp,
            Format::Tga => AtlasFormat::Tga,
            Format::Dds => AtlasFormat::Dds,
            Format::Tiff => AtlasFormat::Tiff,
        }
    }
}

fn main() -> sprite_dicing::Result<()> {
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
        pivot: Pivot::new(args.pivot[0], args.pivot[1]),
    };
    sprite_dicing::dice_in_dir(&args.dir, &fs_prefs, &prefs)
}
