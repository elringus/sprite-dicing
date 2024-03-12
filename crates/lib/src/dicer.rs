use crate::models::*;
use image::{DynamicImage, GenericImageView, RgbaImage, SubImage};
use std::cmp;
use std::collections::{hash_map::DefaultHasher, HashSet};
use std::hash::{Hash, Hasher};

/// Chops source sprite textures and collects unique units.
pub(crate) fn dice(src: &[SourceSprite], prefs: &Prefs) -> Result<Vec<DicedTexture>> {
    if prefs.unit_size < 1 {
        return Err(Error::Spec("Unit size can't be zero."));
    }
    Ok(src.iter().map(|s| dice_src(&new_ctx(s, prefs))).collect())
}

struct Context<'a> {
    size: u32,
    pad: u32,
    trim: bool,
    id: &'a str,
    tex: &'a DynamicImage,
}

fn new_ctx<'a>(src: &'a SourceSprite<'a>, prefs: &Prefs) -> Context<'a> {
    Context {
        size: prefs.unit_size,
        pad: prefs.padding,
        trim: prefs.trim_transparent,
        id: &src.id,
        tex: src.texture,
    }
}

fn dice_src(ctx: &Context) -> DicedTexture {
    let mut units = Vec::new();
    let id = ctx.id.to_owned();
    let unit_count_x = ctx.tex.width().div_ceil(ctx.size);
    let unit_count_y = ctx.tex.height().div_ceil(ctx.size);

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

fn dice_at(x: u32, y: u32, ctx: &Context) -> Option<DicedUnit> {
    let x = x * ctx.size;
    let y = y * ctx.size;

    let unit_rect = PixelRect {
        x,
        y,
        width: ctx.size,
        height: ctx.size,
    };

    let unit_view = view(&unit_rect, ctx.tex);
    if ctx.trim && all_pixels_transparent(&unit_view) {
        return None;
    }

    let rect = crop_over_borders(&unit_rect, ctx.tex);
    let padded_rect = crop_over_borders(&pad_rect(&unit_rect, ctx.pad), ctx.tex);
    let img = view(&padded_rect, ctx.tex).to_image();
    let hash = hash(&img);
    Some(DicedUnit { rect, img, hash })
}

fn view<'a>(rect: &PixelRect, tex: &'a DynamicImage) -> SubImage<&'a DynamicImage> {
    tex.view(rect.x, rect.y, rect.width, rect.height)
}

fn all_pixels_transparent(view: &SubImage<&DynamicImage>) -> bool {
    !view.pixels().any(|p| p.2[3] > 0)
}

fn pad_rect(rect: &PixelRect, pad: u32) -> PixelRect {
    PixelRect {
        x: rect.x.saturating_sub(pad),
        y: rect.y.saturating_sub(pad),
        width: rect.width + pad * 2,
        height: rect.height + pad * 2,
    }
}

fn crop_over_borders(rect: &PixelRect, tex: &DynamicImage) -> PixelRect {
    PixelRect {
        x: rect.x,
        y: rect.y,
        width: cmp::min(rect.width, tex.width() - rect.x),
        height: cmp::min(rect.height, tex.height() - rect.y),
    }
}

fn hash(img: &RgbaImage) -> u64 {
    let mut hasher = DefaultHasher::new();
    img.hash(&mut hasher);
    hasher.finish()
}

fn count_unique(units: &[DicedUnit]) -> u32 {
    let mut set = HashSet::new();
    for unit in units {
        set.insert(unit.hash);
    }
    set.len() as u32
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::fixtures as fx;
    use image::DynamicImage;

    #[test]
    fn errs_when_unit_size_zero() {
        assert!(dice(&[src(&fx::B)], &pref(0, 0, true)).is_err());
    }

    #[test]
    fn unit_count_equal_double_texture_size_divided_by_unit_size_square() {
        assert_eq!(3, dice1(&fx::RGB1X3, 1, 0).units.len());
        assert_eq!(4, dice1(&fx::RGB4X4, 2, 0).units.len());
        assert_eq!(1, dice1(&fx::RGB4X4, 4, 0).units.len());
    }

    #[test]
    fn unit_count_doesnt_depend_on_padding() {
        let pad_0_count = dice1(&fx::RGB4X4, 1, 0).units.len();
        let pad_1_count = dice1(&fx::RGB4X4, 1, 1).units.len();
        assert_eq!(pad_0_count, pad_1_count);
    }

    #[test]
    fn when_unit_size_is_larger_than_texture_single_unit_is_diced() {
        assert_eq!(1, dice1(&fx::RGB3X1, 5, 0).units.len());
        assert_eq!(1, dice1(&fx::RGB4X4, 128, 0).units.len());
    }

    fn dice1(tex: &DynamicImage, size: u32, pad: u32) -> DicedTexture {
        dice(&[src(tex)], &pref(size, pad, true)).unwrap()[0].to_owned()
    }

    fn pref(size: u32, pad: u32, trim: bool) -> Prefs {
        Prefs {
            unit_size: size,
            padding: pad,
            trim_transparent: trim,
            ..Prefs::default()
        }
    }

    fn src(tex: &DynamicImage) -> SourceSprite {
        SourceSprite {
            id: "test".to_string(),
            texture: tex,
            pivot: None,
        }
    }
}
