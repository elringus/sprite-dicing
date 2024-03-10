//! Common data models.

use image::{DynamicImage, SubImage};

/// Preferences for a dicing operation.
pub struct Prefs {
    /// The size of a single diced unit, in pixels. Larger values result in less generated mesh
    /// overhead, but may also diminish number of reused texture regions.
    pub unit_size: u16,
    /// The size of border, in pixels, to add between adjacent diced units inside atlas textures.
    /// Increase to prevent texture bleeding artifacts. Larger values consume more texture space,
    /// but yield better anti-bleeding results.
    pub padding: u16,
    /// Relative inset (in 0.0-1.0 range) of the diced units UV coordinates. Can be used in
    /// addition to (or instead of) [padding] to prevent texture bleeding artifacts. Won't
    /// consume texture space, but higher values could visually distort the rendered sprite.
    pub uv_inset: f32,
    /// Improves compression ratio by discarding fully-transparent dices, but may also change
    /// sprite dimensions. Disable to preserve original sprite texture dimensions.
    pub trim_transparent: bool,
    /// Maximum size (width or height) of a single generated atlas texture; will generate
    /// multiple textures when the limit is reached.
    pub atlas_size_limit: u16,
    /// The generated atlas textures will always be square. Less efficient, but required for
    /// PVRTC compression.
    pub atlas_square: bool,
    /// The generated atlas textures will always have width and height be power of two.
    /// Extremely inefficient, but required by some older GPUs.
    pub atlas_pot: bool,
    /// Pixel per unit ratio to use when evaluating positions of the generated mesh vertices.
    /// Higher values will make sprite larger in conventional space units.
    pub ppu: u16,
    /// Relative position of the sprite origin point on the generated mesh.
    /// Used as a fallback default when pivot in [SourceSprite] is not specified.
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
            ppu: 100,
            pivot: Pivot { x: 0.5, y: 0.5 },
        }
    }
}

/// Original sprite specified as input for a dicing operation.
pub struct SourceSprite<'a> {
    /// Unique identifier of the sprite among others in a dicing operation.
    pub id: String,
    /// Texture containing all the pixels of the sprite.
    pub texture: &'a DynamicImage,
    /// Relative position of the sprite origin point on the generated mesh. When not specified,
    /// will use default pivot specified in [Prefs].
    pub pivot: Option<Pivot>,
}

/// Result of a dicing operation.
pub struct DiceResult {
    /// Generated atlas textures containing unique pixel content of the diced sprites.
    pub atlases: Vec<DynamicImage>,
    /// Generated diced sprites containing mesh data and refs to the associated atlas.
    pub sprites: Vec<DicedSprite>,
}

/// Generated dicing product of a [SourceSprite] containing mesh data and reference to the
/// associated atlas texture required to reconstruct and render sprite at runtime.
pub struct DicedSprite {
    /// ID of the source sprite based on which this sprite is generated.
    pub id: String,
    /// Index of atlas texture in [DiceResult] containing all the unique pixels for this sprite.
    pub atlas_index: usize,
    /// Local position of the generated sprite mesh vertices.
    pub vertices: Vec<VertexPosition>,
    /// Atlas texture coordinates mapped to the [vertices] vector.
    pub uvs: Vec<TextureCoordinate>,
    /// Mesh face (triangle) indices to the [vertices] and [uvs] vectors.
    pub indices: Vec<usize>,
    /// Relative position of the sprite origin point on the generated mesh.
    pub pivot: Pivot,
}

/// Relative (in 0.0-1.0 range) XY distance of the sprite pivot (origin point), counted
/// from top-left corner of the sprite mesh rectangle.
pub struct Pivot {
    /// Relative distance from the left mesh border (x-axis), where 0 is left border,
    /// 0.5 — center and 1.0 is the right border.
    pub x: f32,
    /// Relative distance from the top mesh border (y-axis), where 0 is top border,
    /// 0.5 — center and 1.0 is the bottom border.
    pub y: f32,
}

/// Represents position of a mesh vertex in a local space coordinated with conventional units.
pub struct VertexPosition {
    /// Position over horizontal (X) axis, in conventional units.
    pub x: f32,
    /// Position over vertical (Y) axis, in conventional units.
    pub y: f32,
}

/// Represents position on a texture, relative to its dimensions.
pub struct TextureCoordinate {
    /// Position over horizontal axis, relative to texture width, in 0.0 to 1.0 range.
    pub u: f32,
    /// Position over vertical axis, relative to texture height, in 0.0 to 1.0 range.
    pub v: f32,
}

/// Product of dicing a [SourceSprite]'s texture.
pub(crate) struct DicedTexture<'a> {
    /// Source input sprite which was diced.
    pub source: &'a SourceSprite<'a>,
    /// Associated diced units.
    pub units: Vec<DicedUnit<'a>>,
    /// Associated diced units with distinct content.
    pub unique_units: Vec<DicedUnit<'a>>,
}

/// A chunk diced from a source texture.
pub(crate) struct DicedUnit<'a> {
    /// Position and dimensions of the unit inside source sprite texture.
    pub rect: PixelRect,
    /// Pixels of the diced unit.
    pub view: SubImage<&'a DynamicImage>,
    /// Pixels of the diced unit, plus colors from the padding rect.
    pub padded_view: SubImage<&'a DynamicImage>,
}

/// A rectangular subset of a sprite texture represented via XY offsets from the top-left
/// corner of the texture rectangle, as well as width and height.
pub(crate) struct PixelRect {
    /// Horizontal (x-axis) offset from the top border of the texture rect, in pixels.
    pub x: u16,
    /// Vertical (y-axis) offset from the left border of the texture rect, in pixels.
    pub y: u16,
    /// Width of the rect, in pixels.
    pub width: u16,
    /// Height of the rect, in pixels.
    pub height: u16,
}
