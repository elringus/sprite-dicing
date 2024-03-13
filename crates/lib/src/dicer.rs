use crate::models::*;
use std::cmp;
use std::collections::{hash_map::DefaultHasher, HashSet};
use std::hash::{Hash, Hasher};

/// Chops source sprite textures and collects unique units.
pub(crate) fn dice(src: &[SourceSprite], prefs: &Prefs) -> Result<Vec<DicedTexture>> {
    if prefs.unit_size == 0 {
        return Err(Error::Spec("Unit size can't be zero."));
    }
    Ok(src.iter().map(|s| dice_src(&new_ctx(s, prefs))).collect())
}

struct Context<'a> {
    size: u16,
    pad: u16,
    trim: bool,
    id: &'a str,
    tex: &'a Texture,
}

// Padding may result in negative x/y values, hence using this instead of PixelRect for the
// transient calculations. This is an impl. detail and shouldn't leak outside the module.
struct IntRect {
    pub x: i32,
    pub y: i32,
    pub width: u16,
    pub height: u16,
}

fn new_ctx<'a>(src: &'a SourceSprite, prefs: &Prefs) -> Context<'a> {
    Context {
        size: prefs.unit_size,
        pad: prefs.padding,
        trim: prefs.trim_transparent,
        id: &src.id,
        tex: &src.texture,
    }
}

fn dice_src(ctx: &Context) -> DicedTexture {
    let mut units = Vec::new();
    let id = ctx.id.to_owned();
    let unit_count_x = ctx.tex.width.div_ceil(ctx.size);
    let unit_count_y = ctx.tex.height.div_ceil(ctx.size);

    for x in 0..unit_count_x {
        for y in 0..unit_count_y {
            if let Some(unit) = dice_at(x, y, ctx) {
                units.push(unit);
            }
        }
    }

    let unique = count_unique(&units);
    DicedTexture { id, units, unique }
}

fn dice_at(unit_x: u16, unit_y: u16, ctx: &Context) -> Option<DicedUnit> {
    let unit_rect = IntRect {
        x: unit_x as i32 * ctx.size as i32,
        y: unit_y as i32 * ctx.size as i32,
        width: ctx.size,
        height: ctx.size,
    };

    if ctx.trim && get_pixels(&unit_rect, ctx.tex).iter().all(|p| p.a == 0) {
        return None;
    }

    let rect = crop_over_borders(&unit_rect, ctx.tex);
    let padded_rect = pad_rect(&unit_rect, ctx.pad);
    let pixels = get_pixels(&padded_rect, ctx.tex);
    let hash = hash(&pixels);
    Some(DicedUnit { rect, pixels, hash })
}

fn get_pixels(rect: &IntRect, tex: &Texture) -> Vec<Pixel> {
    let end_x = rect.x + rect.width as i32;
    let end_y = rect.y + rect.height as i32;
    let mut pixels = vec![Pixel::default(); (rect.width * rect.height) as usize];
    let mut idx = 0;
    for y in rect.y..end_y {
        for x in rect.x..end_x {
            pixels[idx] = get_pixel(x, y, tex);
            idx += 1;
        }
    }
    pixels
}

fn get_pixel(x: i32, y: i32, tex: &Texture) -> Pixel {
    let x = saturate(x, tex.width - 1);
    let y = saturate(y, tex.height - 1);
    tex.pixels[(x + tex.width * y) as usize]
}

fn pad_rect(rect: &IntRect, pad: u16) -> IntRect {
    IntRect {
        x: rect.x - pad as i32,
        y: rect.y - pad as i32,
        width: rect.width + pad * 2,
        height: rect.height + pad * 2,
    }
}

fn crop_over_borders(rect: &IntRect, tex: &Texture) -> PixelRect {
    PixelRect {
        x: rect.x as u16,
        y: rect.y as u16,
        width: cmp::min(rect.width, tex.width - rect.x as u16),
        height: cmp::min(rect.height, tex.height - rect.y as u16),
    }
}

fn hash(pixels: &[Pixel]) -> u64 {
    let mut hasher = DefaultHasher::new();
    for pixel in pixels {
        pixel.r.hash(&mut hasher);
        pixel.g.hash(&mut hasher);
        pixel.b.hash(&mut hasher);
        pixel.a.hash(&mut hasher);
    }
    hasher.finish()
}

fn count_unique(units: &[DicedUnit]) -> usize {
    let mut set = HashSet::new();
    for unit in units {
        set.insert(unit.hash);
    }
    set.len()
}

fn saturate(n: i32, max: u16) -> u16 {
    if n < 0 {
        0
    } else if n > max as i32 {
        max
    } else {
        n as u16
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::fixtures::*;

    #[test]
    fn can_dice_with_defaults() {
        assert!(dice(&[src(&B)], &Prefs::default()).is_ok());
    }

    #[test]
    fn errs_when_unit_size_zero() {
        assert!(dice(&[src(&R)], &pref(0, 0, true)).is_err());
    }

    #[test]
    fn unit_count_equal_double_texture_size_divided_by_unit_size_square() {
        assert_eq!(3, dice1(&RGB1X3, 1, 0).units.len());
        assert_eq!(4, dice1(&RGB4X4, 2, 0).units.len());
        assert_eq!(1, dice1(&RGB4X4, 4, 0).units.len());
    }

    #[test]
    fn unit_count_doesnt_depend_on_padding() {
        let pad_0_count = dice1(&RGB4X4, 1, 0).units.len();
        let pad_1_count = dice1(&RGB4X4, 1, 1).units.len();
        assert_eq!(pad_0_count, pad_1_count);
    }

    #[test]
    fn when_unit_size_is_larger_than_texture_single_unit_is_diced() {
        assert_eq!(1, dice1(&RGB3X1, 5, 0).units.len());
        assert_eq!(1, dice1(&RGB4X4, 128, 0).units.len());
    }

    #[test]
    fn transparent_dices_are_ignored_when_trim_enabled() {
        let prf = &pref(1, 0, true);
        assert!(dice(&[src(&TTTT)], prf).unwrap()[0].units.is_empty());
        assert!(dice(&[src(&BGRT)], prf).unwrap().iter().all(is_opaque));
        assert!(dice(&[src(&BTGR)], prf).unwrap().iter().all(is_opaque));
    }

    #[test]
    fn transparent_dices_are_preserved_when_trim_disabled() {
        let prf = &pref(1, 0, false);
        assert!(!dice(&[src(&TTTT)], prf).unwrap()[0].units.is_empty());
        assert!(!dice(&[src(&BGRT)], prf).unwrap().iter().all(is_opaque));
        assert!(!dice(&[src(&BTGR)], prf).unwrap().iter().all(is_opaque));
    }

    fn dice1(tex: &Texture, size: u16, pad: u16) -> DicedTexture {
        dice(&[src(tex)], &pref(size, pad, true)).unwrap()[0].to_owned()
    }

    fn pref(size: u16, pad: u16, trim: bool) -> Prefs {
        Prefs {
            unit_size: size,
            padding: pad,
            trim_transparent: trim,
            ..Prefs::default()
        }
    }

    fn src(tex: &Texture) -> SourceSprite {
        SourceSprite {
            id: "test".to_string(),
            texture: tex.to_owned(),
            pivot: None,
        }
    }

    fn is_opaque(tex: &DicedTexture) -> bool {
        tex.units.iter().all(|u| u.pixels.iter().all(|p| p.a > 0))
    }
}
