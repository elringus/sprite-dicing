//! Common data models.

use image::{GenericImageView, Rgba};

/// Preferences for a dicing operation.
pub struct Prefs {
    /// The size of a single diced unit. Larger values result in less generated mesh overhead,
    /// but may also diminish number of reused texture regions.
    dice_size: u16,
    /// The size of a pixel border to add between adjacent diced units inside atlas textures.
    /// Increase to prevent texture bleeding artifacts. Larger values consume more texture space,
    /// but yield better anti-bleeding results.
    padding: u16,
    /// Improves compression ratio by discarding fully-transparent dices, but may also change
    /// sprite dimensions. Disable to preserve original sprite texture dimensions.
    trim_transparent: bool,
    /// Maximum size (width or height) of a single generated atlas texture; will generate
    /// multiple textures when the limit is reached.
    atlas_size_limit: u16,
    /// The generated atlas textures will always be square. Less efficient, but required for
    /// PVRTC compression.
    atlas_square: bool,
    /// The generated atlas textures will always have width and height of power of two.
    /// Extremely inefficient, but may be required by some older GPUs.
    atlas_pot: bool,
}

/// Result of a dicing operation.
pub struct DiceResult {
    /// Generated atlas textures containing unique pixel content of the diced sprites.
    pub atlases: Vec<Box<dyn GenericImageView<Pixel = Rgba<u8>>>>,
    /// Generated diced sprites containing local sprite rects and refs to atlas content.
    pub sprites: Vec<DicedSprite>,
}

/// Original (non-diced) sprite specified as input for a dicing operation.
pub struct SourceSprite {
    /// Unique identifier of the sprite among others in a dicing operation.
    pub id: String,
    /// Texture containing all the pixels of the sprite.
    pub texture: Box<dyn GenericImageView<Pixel = Rgba<u8>>>,
}

/// Generated dicing product of a [SourceSprite].
pub struct DicedSprite {
    /// ID of the source sprite based on which this sprite is generated.
    pub id: String,
    /// Generated atlas texture containing all the pixels for this sprite.
    pub atlas: Box<dyn GenericImageView<Pixel = Rgba<u8>>>,
    /// Generated dices of the sprite.
    pub dices: Vec<Dice>,
}

/// A rect inside original sprite associated with a rect inside generated atlas texture
/// with the pixels content.
pub struct Dice {
    /// Position and dimensions of the dice inside original sprite.
    pub local: Rect,
    /// Rect inside associated atlas texture with pixels content of the dice.
    pub atlas: Rect,
}

/// A rectangular subset of a sprite texture represented via XY offsets from the top-left
/// corner of the texture rectangle, as well as width and height.
pub struct Rect {
    /// Horizontal (x-axis) offset from the top border of the texture rect, in pixels.
    pub x: u16,
    /// Vertical (y-axis) offset from the left border of the texture rect, in pixels.
    pub y: u16,
    /// Width of the rect, in pixels.
    pub width: u16,
    /// Height of the rect, in pixels.
    pub height: u16,
}
