use crate::models::*;
use std::cmp;
use std::collections::HashSet;
use std::hash::{Hash, Hasher};

/// Chops source sprite textures and collects unique units.
pub(crate) fn dice(src: &[SourceSprite], prefs: &Prefs) -> Result<Vec<DicedTexture>> {
    if prefs.unit_size == 0 {
        return Err(Error::Spec("Unit size can't be zero."));
    }
    if prefs.padding > prefs.unit_size {
        return Err(Error::Spec("Unit size can't be above atlas size limit."));
    }
    Ok(src.iter().map(|s| dice_it(&new_ctx(s, prefs))).collect())
}

struct Context<'a> {
    size: u32,
    pad: u32,
    trim: bool,
    id: &'a str,
    tex: &'a Texture,
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

fn dice_it(ctx: &Context) -> DicedTexture {
    let mut units = Vec::new();
    let unit_count_x = ctx.tex.width.div_ceil(ctx.size);
    let unit_count_y = ctx.tex.height.div_ceil(ctx.size);

    for x in 0..unit_count_x {
        for y in 0..unit_count_y {
            if let Some(unit) = dice_at(x, y, ctx) {
                units.push(unit);
            }
        }
    }

    let id = ctx.id.to_owned();
    let unique = units.iter().map(|u| u.hash).collect::<HashSet<_>>();
    DicedTexture { id, units, unique }
}

fn dice_at(unit_x: u32, unit_y: u32, ctx: &Context) -> Option<DicedUnit> {
    let unit_rect = IRect {
        x: unit_x as i32 * ctx.size as i32,
        y: unit_y as i32 * ctx.size as i32,
        width: ctx.size,
        height: ctx.size,
    };

    let unit_pixels = get_pixels(&unit_rect, ctx.tex);
    if ctx.trim && unit_pixels.iter().all(|p| p.a == 0) {
        return None;
    }

    let hash = hash(&unit_pixels);
    let rect = crop_over_borders(&unit_rect, ctx.tex);
    let padded_rect = pad_rect(&unit_rect, ctx.pad);
    let pixels = get_pixels(&padded_rect, ctx.tex);
    Some(DicedUnit { rect, pixels, hash })
}

fn get_pixels(rect: &IRect, tex: &Texture) -> Vec<Pixel> {
    let end_x = rect.x + rect.width as i32;
    let end_y = rect.y + rect.height as i32;
    let size = (rect.width * rect.height) as usize;
    let mut pixels = vec![Pixel::default(); size];
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

fn pad_rect(rect: &IRect, pad: u32) -> IRect {
    IRect {
        x: rect.x - pad as i32,
        y: rect.y - pad as i32,
        width: rect.width + pad * 2,
        height: rect.height + pad * 2,
    }
}

fn crop_over_borders(rect: &IRect, tex: &Texture) -> URect {
    URect {
        x: rect.x as u32,
        y: rect.y as u32,
        width: cmp::min(rect.width, tex.width - rect.x as u32),
        height: cmp::min(rect.height, tex.height - rect.y as u32),
    }
}

fn hash(pixels: &[Pixel]) -> u64 {
    // Using std::hash::DefaultHasher breaks build for macOS.
    let mut hasher = std::collections::hash_map::DefaultHasher::new();
    for pixel in pixels {
        pixel.r.hash(&mut hasher);
        pixel.g.hash(&mut hasher);
        pixel.b.hash(&mut hasher);
        pixel.a.hash(&mut hasher);
    }
    hasher.finish()
}

fn saturate(n: i32, max: u32) -> u32 {
    if n < 0 {
        0
    } else if n > max as i32 {
        max
    } else {
        n as u32
    }
}

#[cfg(test)]
mod tests {
    use crate::dicer::dice;
    use crate::fixtures::*;
    use crate::models::*;

    #[test]
    fn can_dice_with_defaults() {
        assert!(dice(&[src(&B1X1)], &Prefs::default()).is_ok());
    }

    #[test]
    fn errs_when_unit_size_zero() {
        assert!(dice(&[src(&R1X1)], &pref(0, 0, true))
            .is_err_and(|e| e.to_string() == "Unit size can't be zero."));
    }

    #[test]
    fn errs_when_padding_is_above_unit_size() {
        assert!(dice(&[src(&R1X1)], &pref(1, 2, true))
            .is_err_and(|e| e.to_string() == "Unit size can't be above atlas size limit."));
    }

    #[test]
    fn unit_count_equal_double_texture_size_divided_by_unit_size_square() {
        assert_eq!(dice1(&RGB1X3, 1, 0).units.len(), 3);
        assert_eq!(dice1(&RGB4X4, 2, 0).units.len(), 4);
        assert_eq!(dice1(&RGB4X4, 4, 0).units.len(), 1);
    }

    #[test]
    fn unit_count_doesnt_depend_on_padding() {
        let pad_0_count = dice1(&RGB4X4, 1, 0).units.len();
        let pad_1_count = dice1(&RGB4X4, 1, 1).units.len();
        assert_eq!(pad_0_count, pad_1_count);
    }

    #[test]
    fn when_unit_size_is_larger_than_texture_single_unit_is_diced() {
        assert_eq!(dice1(&RGB3X1, 5, 0).units.len(), 1);
        assert_eq!(dice1(&RGB4X4, 128, 0).units.len(), 1);
    }

    #[test]
    fn transparent_dices_are_ignored_when_trim_enabled() {
        let prf = &pref(1, 0, true);
        assert!(dice(&[src(&CCCC)], prf).unwrap()[0].units.is_empty());
        assert!(dice(&[src(&BGRC)], prf).unwrap().iter().all(is_opaque));
        assert!(dice(&[src(&BCGR)], prf).unwrap().iter().all(is_opaque));
    }

    #[test]
    fn transparent_dices_are_preserved_when_trim_disabled() {
        let prf = &pref(1, 0, false);
        assert!(!dice(&[src(&CCCC)], prf).unwrap()[0].units.is_empty());
        assert!(!dice(&[src(&BGRC)], prf).unwrap().iter().all(is_opaque));
        assert!(!dice(&[src(&BCGR)], prf).unwrap().iter().all(is_opaque));
    }

    #[test]
    fn content_hash_of_equal_pixels_is_equal() {
        let units = dice1(&BGRC, 1, 0).units;
        for unit in dice1(&BCGR, 1, 0).units {
            assert!(units.iter().any(|u| u.hash == unit.hash));
        }
    }

    #[test]
    fn content_hash_of_distinct_pixels_is_not_equal() {
        assert_ne!(
            dice1(&B1X1, 1, 0).units[0].hash,
            dice1(&R1X1, 1, 0).units[0].hash
        );
    }

    #[test]
    fn content_hash_ignores_padding() {
        let no_pad = dice1(&RGB4X4, 1, 0).units;
        for padded in dice1(&RGB4X4, 1, 1).units {
            assert!(no_pad.iter().any(|u| u.hash == padded.hash))
        }
    }

    #[test]
    fn unit_rects_are_mapped_top_left_to_bottom_right() {
        let units = &dice(&[src(&BGRC)], &pref(1, 0, false)).unwrap()[0].units;
        assert!(has(units, B, URect::new(0, 0, 1, 1)));
        assert!(has(units, G, URect::new(1, 0, 1, 1)));
        assert!(has(units, R, URect::new(0, 1, 1, 1)));
        assert!(has(units, C, URect::new(1, 1, 1, 1)));
        fn has(units: &[DicedUnit], pixel: Pixel, rect: URect) -> bool {
            units.iter().any(|u| u.pixels[0] == pixel && u.rect == rect)
        }
    }

    #[test]
    fn when_no_content_padded_pixels_are_repeated() {
        #[rustfmt::skip]
        assert_eq!(
            dice1(&B1X1, 1, 1).units[0].pixels,
            vec![B, B, B,
                 B, B, B,
                 B, B, B]);
    }

    #[test]
    fn padded_pixels_are_neighbors() {
        let pixels = dice1(&BGRC, 1, 1)
            .units
            .into_iter()
            .map(|u| u.pixels)
            .collect::<Vec<_>>();
        #[rustfmt::skip]
        assert!(pixels.contains(&vec![
            B, B, G,
            B, B, G,
            R, R, C]));
    }

    #[test]
    fn diced_texture_contains_identical_units() {
        assert_eq!(16, dice1(&RGB4X4, 1, 0).units.len());
        assert_eq!(16, dice1(&UIC4X4, 1, 0).units.len());
    }

    #[test]
    fn unique_doesnt_count_identical_units() {
        assert_eq!(3, dice1(&RGB4X4, 1, 0).unique.len());
        assert_eq!(16, dice1(&UIC4X4, 1, 0).unique.len());
    }

    fn dice1(tex: &Texture, size: u32, pad: u32) -> DicedTexture {
        let pref = pref(size, pad, true);
        dice(&[src(tex)], &pref).unwrap().pop().unwrap()
    }

    fn pref(size: u32, pad: u32, trim: bool) -> Prefs {
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
