//! Common data models.

use std::collections::{HashMap, HashSet};

/// Result of a dicing operation.
pub type Result<T> = std::result::Result<T, Error>;

/// Error occurred in a dicing operation.
#[derive(Debug)]
pub enum Error {
    /// An issue with [Prefs] and/or input data.
    Spec(&'static str),
    /// An issue with texture encoding.
    #[cfg(feature = "fs")]
    Image(image::ImageError),
    /// An issue with an I/O operation.
    #[cfg(feature = "fs")]
    Io(std::io::Error),
}

impl std::fmt::Display for Error {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Error::Spec(info) => write!(f, "{}", info),
            #[cfg(feature = "fs")]
            Error::Image(err) => write!(f, "{}", err),
            #[cfg(feature = "fs")]
            Error::Io(err) => write!(f, "{}", err),
        }
    }
}

#[cfg(feature = "fs")]
impl From<std::io::Error> for Error {
    fn from(err: std::io::Error) -> Self {
        Error::Io(err)
    }
}

#[cfg(feature = "fs")]
impl From<image::ImageError> for Error {
    fn from(err: image::ImageError) -> Self {
        Error::Image(err)
    }
}

impl std::error::Error for Error {}

/// Preferences for a dicing operation.
#[derive(Debug, Clone)]
pub struct Prefs {
    /// The size of a single diced unit, in pixels. Larger values result in less generated mesh
    /// overhead, but may also diminish number of reused texture regions.
    pub unit_size: u32,
    /// The size of border, in pixels, to add between adjacent diced units inside atlas textures.
    /// Increase to prevent texture bleeding artifacts. Larger values consume more texture space,
    /// but yield better anti-bleeding results.
    pub padding: u32,
    /// Relative inset (in 0.0-1.0 range) of the diced units UV coordinates. Can be used in
    /// addition to (or instead of) [padding] to prevent texture bleeding artifacts. Won't
    /// consume texture space, but higher values could visually distort the rendered sprite.
    pub uv_inset: f32,
    /// Improves compression ratio by discarding fully-transparent dices, but may also change
    /// sprite dimensions. Disable to preserve original sprite texture dimensions.
    pub trim_transparent: bool,
    /// Maximum size (width or height) of a single generated atlas texture; will generate
    /// multiple textures when the limit is reached.
    pub atlas_size_limit: u32,
    /// The generated atlas textures will always be square. Less efficient, but required for
    /// PVRTC compression.
    pub atlas_square: bool,
    /// The generated atlas textures will always have width and height be power of two.
    /// Extremely inefficient, but required by some older GPUs.
    pub atlas_pot: bool,
    /// Pixel per unit ratio to use when evaluating positions of the generated mesh vertices.
    /// Higher values will make sprite smaller in conventional space units.
    pub ppu: f32,
    /// Origin of the generated mesh, in relative offsets from top-left corner of the sprite rect.
    /// When differs from the default (0,0), will offset vertices to center mesh around the pivot.
    /// Ignored when [SourceSprite] has individual pivot specified.
    pub pivot: Pivot,
}

impl Default for Prefs {
    fn default() -> Self {
        Self {
            unit_size: 64,
            padding: 2,
            uv_inset: 0.0,
            trim_transparent: true,
            atlas_size_limit: 2048,
            atlas_square: false,
            atlas_pot: false,
            ppu: 100.0,
            pivot: Pivot { x: 0.0, y: 0.0 },
        }
    }
}

/// A texture pixel represented as 8-bit RGBA components.
#[derive(Debug, Copy, Clone, Eq, PartialEq, Hash, Default)]
pub struct Pixel([u8; 4]);

impl Pixel {
    pub const fn new(r: u8, g: u8, b: u8, a: u8) -> Self {
        Pixel([r, g, b, a])
    }
    pub const fn from_raw(raw: [u8; 4]) -> Self {
        Pixel(raw)
    }
    #[cfg(feature = "fs")]
    pub fn from_rgba(rgba: &image::Rgba<u8>) -> Self {
        Pixel(rgba.0)
    }
    pub fn r(&self) -> u8 {
        self.0[0]
    }
    pub fn g(&self) -> u8 {
        self.0[1]
    }
    pub fn b(&self) -> u8 {
        self.0[2]
    }
    pub fn a(&self) -> u8 {
        self.0[3]
    }
    pub fn to_raw(self) -> [u8; 4] {
        self.0
    }
}

/// A set of pixels forming sprite texture.
#[derive(Debug, Clone)]
pub struct Texture {
    /// Width of the texture, in pixels.
    pub width: u32,
    /// Height of the texture, in pixels.
    pub height: u32,
    /// Pixel content of the texture. Expected to be in order, indexed left to right,
    /// top to bottom; eg, first pixel would be top-left on texture rect, while last
    /// would be the bottom-right one.
    pub pixels: Vec<Pixel>,
}

impl Texture {
    #[cfg(feature = "fs")]
    pub fn from_image(img: &image::RgbaImage) -> Self {
        Texture {
            width: img.width(),
            height: img.height(),
            pixels: img.pixels().map(Pixel::from_rgba).collect(),
        }
    }
    #[cfg(feature = "fs")]
    pub fn to_image(self) -> Result<image::RgbaImage> {
        let buf = self.pixels.into_iter().flat_map(Pixel::to_raw).collect();
        image::RgbaImage::from_raw(self.width, self.height, buf).ok_or(Error::Image(
            image::error::ImageError::Encoding(image::error::EncodingError::new(
                image::error::ImageFormatHint::Unknown,
                "Failed to convert texture into RGBA8 image.",
            )),
        ))
    }
}

/// Original sprite specified as input for a dicing operation.
#[derive(Debug, Clone)]
pub struct SourceSprite {
    /// Unique identifier of the sprite among others in a dicing operation.
    pub id: String,
    /// Texture containing all the pixels of the sprite.
    pub texture: Texture,
    /// Relative position of the sprite origin point on the generated mesh. When not specified,
    /// will use default pivot specified in [Prefs].
    pub pivot: Option<Pivot>,
}

/// Final products of a dicing operation.
#[derive(Debug, Clone)]
pub struct Artifacts {
    /// Generated atlas textures containing unique pixel content of the diced sprites.
    pub atlases: Vec<Texture>,
    /// Generated diced sprites with data to reconstruct source spites: mesh, uvs, etc.
    pub sprites: Vec<DicedSprite>,
}

/// Generated dicing product of a [SourceSprite] containing mesh data and reference to the
/// associated atlas texture required to reconstruct and render sprite at runtime.
#[derive(Debug, Clone)]
pub struct DicedSprite {
    /// ID of the source sprite based on which this sprite is generated.
    pub id: String,
    /// Index of atlas texture in [Artifacts] containing the unique pixels for this sprite.
    pub atlas_index: usize,
    /// Local position of the generated sprite mesh vertices.
    pub vertices: Vec<Vertex>,
    /// Atlas texture coordinates mapped to the [vertices] vector.
    pub uvs: Vec<UV>,
    /// Mesh face (triangle) indices to the [vertices] and [uvs] vectors.
    pub indices: Vec<usize>,
    /// Rect of the sprite in conventional units space, aka boundaries.
    pub rect: Rect,
}

/// A rectangle in conventional units space.
#[derive(Debug, Clone, PartialEq)]
pub struct Rect {
    /// Position of the top-left corner of the rectangle on horizontal axis.
    pub x: f32,
    /// Position of the top-left corner of the rectangle on vertical axis.
    pub y: f32,
    /// Length of the rectangle over horizontal axis, starting from X.
    pub width: f32,
    /// Length of the rectangle over vertical axis, starting from Y.
    pub height: f32,
}

impl Rect {
    pub fn new(x: f32, y: f32, width: f32, height: f32) -> Self {
        Rect {
            x,
            y,
            width,
            height,
        }
    }
}

/// Relative (in 0.0-1.0 range) XY distance of the sprite pivot (origin point), counted
/// from top-left corner of the sprite mesh rectangle.
#[derive(Debug, Clone, PartialEq)]
pub struct Pivot {
    /// Relative distance from the left mesh border (x-axis), where 0 is left border,
    /// 0.5 — center and 1.0 is the right border.
    pub x: f32,
    /// Relative distance from the top mesh border (y-axis), where 0 is top border,
    /// 0.5 — center and 1.0 is the bottom border.
    pub y: f32,
}

impl Pivot {
    pub fn new(x: f32, y: f32) -> Self {
        Pivot { x, y }
    }
}

/// Represents position of a mesh vertex in a local space coordinated with conventional units.
#[derive(Debug, Clone, PartialEq)]
pub struct Vertex {
    /// Position over horizontal (X) axis, in conventional units.
    pub x: f32,
    /// Position over vertical (Y) axis, in conventional units.
    pub y: f32,
}

impl Vertex {
    pub fn new(x: f32, y: f32) -> Self {
        Vertex { x, y }
    }
}

/// Represents position on a texture, relative to its dimensions.
#[derive(Debug, Clone, PartialEq)]
pub struct UV {
    /// Position over horizontal axis, relative to texture width, in 0.0 to 1.0 range.
    pub u: f32,
    /// Position over vertical axis, relative to texture height, in 0.0 to 1.0 range.
    pub v: f32,
}

impl UV {
    pub fn new(u: f32, v: f32) -> Self {
        UV { u, v }
    }
}

/// Product of dicing a [SourceSprite]'s texture.
#[derive(Debug, Clone)]
pub(crate) struct DicedTexture {
    /// Identifier of the [SourceSprite] to which this texture belongs.
    pub id: String,
    /// Pivot of the associated [SourceSprite], if any.
    pub pivot: Option<Pivot>,
    /// Associated diced units.
    pub units: Vec<DicedUnit>,
    /// Hashes of diced units with distinct content.
    pub unique: HashSet<u64>,
}

/// A chunk diced from a source texture.
#[derive(Debug, Clone)]
pub(crate) struct DicedUnit {
    /// Position and dimensions of the unit inside source texture.
    pub rect: URect,
    /// Unit pixels chopped from the source texture, including padding.
    pub pixels: Vec<Pixel>,
    /// Content hash based on the non-padded pixels of the unit.
    pub hash: u64,
}

/// Product of packing [DicedTexture]s.
#[derive(Debug, Clone)]
pub(crate) struct Atlas {
    /// The atlas texture containing unique content of the packed diced textures.
    pub texture: Texture,
    /// Packed unit UV rects on the atlas texture, mapped by unit hashes.
    pub rects: HashMap<u64, FRect>,
    /// Diced textures packed into this atlas.
    pub packed: Vec<DicedTexture>,
}

/// A rectangle in unsigned integer space.
#[derive(Debug, Clone, Eq, PartialEq)]
pub(crate) struct URect {
    /// Position of the top-left corner of the rectangle on horizontal axis.
    pub x: u32,
    /// Position of the top-left corner of the rectangle on vertical axis.
    pub y: u32,
    /// Length of the rectangle over horizontal axis, starting from X.
    pub width: u32,
    /// Length of the rectangle over vertical axis, starting from Y.
    pub height: u32,
}

impl URect {
    #[allow(dead_code)] // Used in tests.
    pub fn new(x: u32, y: u32, width: u32, height: u32) -> Self {
        URect {
            x,
            y,
            width,
            height,
        }
    }
}

/// A rectangle in signed integer space.
#[derive(Debug, Clone, Eq, PartialEq)]
pub(crate) struct IRect {
    /// Position of the top-left corner of the rectangle on horizontal axis.
    pub x: i32,
    /// Position of the top-left corner of the rectangle on vertical axis.
    pub y: i32,
    /// Length of the rectangle over horizontal axis, starting from X.
    pub width: u32,
    /// Length of the rectangle over vertical axis, starting from Y.
    pub height: u32,
}

impl IRect {
    #[allow(dead_code)] // Used in tests.
    pub fn new(x: i32, y: i32, width: u32, height: u32) -> Self {
        IRect {
            x,
            y,
            width,
            height,
        }
    }
}

/// A rectangle in floating point space.
#[derive(Debug, Clone, PartialEq)]
pub(crate) struct FRect {
    /// Position of the top-left corner of the rectangle on horizontal axis.
    pub x: f32,
    /// Position of the top-left corner of the rectangle on vertical axis.
    pub y: f32,
    /// Length of the rectangle over horizontal axis, starting from X.
    pub width: f32,
    /// Length of the rectangle over vertical axis, starting from Y.
    pub height: f32,
}

impl FRect {
    pub fn new(x: f32, y: f32, width: f32, height: f32) -> Self {
        FRect {
            x,
            y,
            width,
            height,
        }
    }
}

/// A size of arbitrary entity in unsigned integer space.
#[derive(Debug, Clone, Eq, PartialEq)]
pub(crate) struct USize {
    /// Width of the entity.
    pub width: u32,
    /// Height of the entity.
    pub height: u32,
}

impl USize {
    pub fn new(width: u32, height: u32) -> Self {
        USize { width, height }
    }
}
