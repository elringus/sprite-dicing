use crate::models::*;
use anyhow::{bail, Result};
use image::{DynamicImage, GenericImageView, RgbaImage, SubImage};
use std::cmp;
use std::hash::{DefaultHasher, Hash, Hasher};

/// Chops source sprite textures and collects unique units.
pub(crate) fn dice(src: &[SourceSprite], prefs: &Prefs) -> Result<Vec<DicedTexture>> {
    if prefs.unit_size < 1 {
        bail!("Unit size can't be zero.")
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
    DicedTexture { id, units }
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

    let rect = crop_over_borders(&unit_rect, x, y, ctx.tex);
    let padded_rect = pad_rect(&unit_rect, ctx.pad);
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
        x: rect.x - pad,
        y: rect.y - pad,
        width: rect.width + pad * 2,
        height: rect.height + pad * 2,
    }
}

fn crop_over_borders(rect: &PixelRect, x: u32, y: u32, tex: &DynamicImage) -> PixelRect {
    PixelRect {
        x,
        y,
        width: cmp::min(rect.width, tex.width() - x),
        height: cmp::min(rect.height, tex.height() - y),
    }
}

fn hash(img: &RgbaImage) -> u64 {
    let mut hasher = DefaultHasher::new();
    img.hash(&mut hasher);
    hasher.finish()
}

#[cfg(test)]
mod tests {
    use image::DynamicImage;

    use crate::fixtures as fx;

    use super::*;

    #[test]
    fn errs_when_unit_size_zero() {
        assert!(dice(&src(&fx::B), &pref(0, 0, false)).is_err());
    }

    #[test]
    fn foo() {
        dice(&src(&fx::B), &Prefs::default()).unwrap();
    }

    fn pref(size: u32, pad: u32, trim: bool) -> Prefs {
        Prefs {
            unit_size: size,
            padding: pad,
            trim_transparent: trim,
            ..Prefs::default()
        }
    }

    fn src(tex: &DynamicImage) -> Vec<SourceSprite> {
        vec![SourceSprite {
            id: "test".to_string(),
            texture: tex,
            pivot: None,
        }]
    }
}
