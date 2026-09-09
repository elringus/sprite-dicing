use crate::models::*;
use std::cmp;
use std::collections::HashMap;
use std::hash::{DefaultHasher, Hash, Hasher};

/// Chops source sprite textures and collects unique units.
pub(crate) fn dice(sprites: &[SourceSprite], prefs: &Prefs) -> Result<Vec<DicedTexture>> {
    if prefs.unit_size == 0 {
        return Err(Error::Spec("Unit size can't be zero."));
    }
    if prefs.padding > prefs.unit_size {
        return Err(Error::Spec("Padding can't be above unit size."));
    }

    let mut textures = vec![];
    let mut ctx = new_ctx(prefs);
    for (idx, sprite) in sprites.iter().enumerate() {
        Progress::report(prefs, 1, idx, sprites.len(), "Dicing source textures");
        if let Some(texture) = dice_it(sprite, &mut ctx) {
            textures.push(texture);
        }
    }

    Ok(textures)
}

struct Context {
    /// Size of a unit, in pixels.
    size: u32,
    /// Size of the padding stored around the units, in pixels.
    pad: u32,
    /// IDs of the units diced so far, mapped by hash of the unit pixels.
    ids: HashMap<u64, Vec<usize>>,
    /// Size and unpadded pixels of the units diced so far, indexed by unit ID.
    pixels: Vec<(USize, Vec<Pixel>)>,
}

fn new_ctx(prefs: &Prefs) -> Context {
    Context {
        size: prefs.unit_size,
        pad: prefs.padding,
        ids: HashMap::new(),
        pixels: vec![],
    }
}

fn dice_it(sprite: &SourceSprite, ctx: &mut Context) -> Option<DicedTexture> {
    let tex = &sprite.texture;
    let mut units = vec![];
    for y in (0..tex.height).step_by(ctx.size as usize) {
        for x in (0..tex.width).step_by(ctx.size as usize) {
            units.extend(dice_at(x, y, sprite, ctx));
        }
    }

    if units.is_empty() {
        return None;
    }

    let mut unique = units.iter().map(|u| u.id).collect::<Vec<_>>();
    unique.sort_unstable();
    unique.dedup();

    Some(DicedTexture {
        id: sprite.id.to_owned(),
        size: USize::new(tex.width, tex.height),
        pivot: sprite.pivot.to_owned(),
        units,
        unique,
    })
}

/// Dices the unit with specified top-left position; returns none when it's fully transparent.
fn dice_at(x: u32, y: u32, sprite: &SourceSprite, ctx: &mut Context) -> Option<DicedUnit> {
    let tex = &sprite.texture;
    let rect = URect {
        x,
        y,
        width: cmp::min(ctx.size, tex.width - x),
        height: cmp::min(ctx.size, tex.height - y),
    };
    let pixels = get_pixels(&rect, 0, tex);
    if pixels.iter().all(|p| p.a() == 0) {
        return None;
    }

    Some(DicedUnit {
        id: get_id(rect.size(), pixels, ctx),
        pack_rect: eval_pack_rect(&rect, tex),
        pixels: get_pixels(&rect, ctx.pad, tex),
        src_rect: rect,
    })
}

/// Returns ID of a unit with equal size and pixels diced before, or assigns the next one.
fn get_id(size: USize, pixels: Vec<Pixel>, ctx: &mut Context) -> usize {
    let ids = ctx.ids.entry(hash(&size, &pixels)).or_default();
    for &id in ids.iter() {
        let (known_size, known_pixels) = &ctx.pixels[id];
        if *known_size == size && *known_pixels == pixels {
            return id;
        }
    }
    let id = ctx.pixels.len();
    ids.push(id);
    ctx.pixels.push((size, pixels));
    id
}

/// Evaluates the part of the rect to pack: bounds of the non-transparent pixels in the rect
/// and its one-pixel neighborhood, expanded by one pixel and clipped to the rect, so that
/// filtered rendering samples the same neighbors as in the source; relative to the rect.
fn eval_pack_rect(rect: &URect, tex: &Texture) -> URect {
    let (left, top) = (rect.x as i32, rect.y as i32);
    let (right, bottom) = (left + rect.width as i32, top + rect.height as i32);
    let (mut min_x, mut min_y) = (i32::MAX, i32::MAX);
    let (mut max_x, mut max_y) = (i32::MIN, i32::MIN);
    for y in (top - 1)..=bottom {
        for x in (left - 1)..=right {
            if get_pixel(x, y, tex).a() > 0 {
                min_x = cmp::min(min_x, x);
                min_y = cmp::min(min_y, y);
                max_x = cmp::max(max_x, x);
                max_y = cmp::max(max_y, y);
            }
        }
    }
    let x = cmp::max(min_x - 1, left);
    let y = cmp::max(min_y - 1, top);
    URect {
        x: (x - left) as u32,
        y: (y - top) as u32,
        width: (cmp::min(max_x + 2, right) - x) as u32,
        height: (cmp::min(max_y + 2, bottom) - y) as u32,
    }
}

/// Copies pixels of the rect with specified padding around it, repeating the edge pixels
/// of the texture where the padding is out of bounds.
fn get_pixels(rect: &URect, pad: u32, tex: &Texture) -> Vec<Pixel> {
    let (width, height) = (rect.width + pad * 2, rect.height + pad * 2);
    let (left, top) = (rect.x as i32 - pad as i32, rect.y as i32 - pad as i32);
    let mut pixels = Vec::with_capacity((width * height) as usize);
    for y in top..top + height as i32 {
        for x in left..left + width as i32 {
            pixels.push(get_pixel(x, y, tex));
        }
    }
    pixels
}

/// Returns the pixel at the position clamped to the texture bounds.
fn get_pixel(x: i32, y: i32, tex: &Texture) -> Pixel {
    let x = saturate(x, tex.width - 1);
    let y = saturate(y, tex.height - 1);
    tex.pixels[(x + tex.width * y) as usize]
}

fn hash(size: &USize, pixels: &[Pixel]) -> u64 {
    let mut hasher = DefaultHasher::new();
    size.width.hash(&mut hasher);
    size.height.hash(&mut hasher);
    pixels.hash(&mut hasher);
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
        assert!(
            dice(&[src(&R1X1)], &pref(0, 0))
                .is_err_and(|e| e.to_string() == "Unit size can't be zero.")
        );
    }

    #[test]
    fn errs_when_padding_is_above_unit_size() {
        assert!(
            dice(&[src(&R1X1)], &pref(1, 2))
                .is_err_and(|e| e.to_string() == "Padding can't be above unit size.")
        );
    }

    #[test]
    fn size_equals_source_texture_dimensions() {
        let diced = dice1(&RGB4X4, 4, 0);
        assert_eq!(diced.size.width, 4);
        assert_eq!(diced.size.height, 4);
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
    fn transparent_units_are_ignored() {
        let prf = &pref(1, 0);
        assert!(dice(&[src(&BGRT)], prf).unwrap().iter().all(is_opaque));
        assert!(dice(&[src(&BTGR)], prf).unwrap().iter().all(is_opaque));
    }

    #[test]
    fn transparent_sprites_are_ignored() {
        let prf = &pref(1, 0);
        assert!(dice(&[src(&TTTT)], prf).unwrap().is_empty());
    }

    #[test]
    fn units_with_equal_pixels_share_id() {
        let diced = dice(&[src(&BGRT), src(&BTGR)], &pref(1, 0)).unwrap();
        for unit in diced[1].units.iter() {
            assert!(diced[0].units.iter().any(|u| u.id == unit.id));
        }
    }

    #[test]
    fn units_with_distinct_pixels_have_distinct_ids() {
        let diced = dice(&[src(&B1X1), src(&R1X1)], &pref(1, 0)).unwrap();
        assert_ne!(diced[0].units[0].id, diced[1].units[0].id);
    }

    #[test]
    fn unit_ids_ignore_padding() {
        let no_pad = dice1(&RGB4X4, 1, 0).units;
        for padded in dice1(&RGB4X4, 1, 1).units {
            assert!(no_pad.iter().any(|u| u.id == padded.id))
        }
    }

    #[test]
    fn unit_ids_are_assigned_in_order_of_appearance() {
        let diced = dice(&[src(&RGBY), src(&B1X1)], &pref(1, 0)).unwrap();
        let ids = diced[0].units.iter().map(|u| u.id).collect::<Vec<_>>();
        assert_eq!(ids, vec![0, 1, 2, 3]);
        assert_eq!(diced[1].units[0].id, 2);
    }

    #[test]
    fn unit_identity_includes_clipped_size() {
        let diced = dice(&[src(&RGB3X1), src(&B1X1)], &pref(2, 0)).unwrap();
        assert_eq!(diced[0].units[1].id, diced[1].units[0].id);
        let diced = dice(&[src(&RGB3X1), src(&RGB1X3)], &pref(2, 0)).unwrap();
        assert_ne!(diced[0].units[0].id, diced[1].units[0].id);
        assert_eq!(diced[0].units[1].id, diced[1].units[1].id);
    }

    #[test]
    fn unit_rects_are_mapped_top_left_to_bottom_right() {
        let units = &dice(&[src(&RGBY)], &pref(1, 0)).unwrap()[0].units;
        assert!(has(units, R, URect::new(0, 0, 1, 1)));
        assert!(has(units, G, URect::new(1, 0, 1, 1)));
        assert!(has(units, B, URect::new(0, 1, 1, 1)));
        assert!(has(units, Y, URect::new(1, 1, 1, 1)));
        fn has(units: &[DicedUnit], pixel: Pixel, rect: URect) -> bool {
            units
                .iter()
                .any(|u| u.pixels[0] == pixel && u.src_rect == rect)
        }
    }

    #[test]
    fn units_are_ordered_row_major() {
        let rects = dice1(&RGB4X4, 2, 0)
            .units
            .iter()
            .map(|u| (u.src_rect.x, u.src_rect.y))
            .collect::<Vec<_>>();
        assert_eq!(rects, vec![(0, 0), (2, 0), (0, 2), (2, 2)]);
    }

    #[test]
    fn edge_units_are_clipped_to_texture() {
        let units = dice1(&RGB3X1, 2, 0).units;
        assert_eq!(units[0].src_rect, URect::new(0, 0, 2, 1));
        assert_eq!(units[1].src_rect, URect::new(2, 0, 1, 1));
    }

    #[test]
    fn pack_rect_of_opaque_unit_is_whole_rect() {
        let units = dice1(&RGB4X4, 2, 0).units;
        assert!(units.iter().all(|u| u.pack_rect == URect::new(0, 0, 2, 2)));
    }

    #[test]
    fn pack_rect_is_content_with_fringe() {
        let unit = &dice1(&dot(5, 5, 2, 2), 5, 0).units[0];
        assert_eq!(unit.pack_rect, URect::new(1, 1, 3, 3));
        let unit = &dice1(&dot(5, 5, 0, 0), 5, 0).units[0];
        assert_eq!(unit.pack_rect, URect::new(0, 0, 2, 2));
    }

    #[test]
    fn pack_rect_covers_fringe_of_neighbor_units() {
        let diced = dice1(&dot(4, 4, 1, 1), 2, 0);
        assert_eq!(diced.units.len(), 1);
        assert_eq!(diced.units[0].pack_rect, URect::new(0, 0, 2, 2));
        let mut tex = dot(4, 4, 1, 1);
        tex.pixels[2 + 2 * 4] = M;
        let diced = dice1(&tex, 2, 0);
        assert_eq!(diced.units.len(), 2);
        assert_eq!(diced.units[0].pack_rect, URect::new(0, 0, 2, 2));
        assert_eq!(diced.units[1].pack_rect, URect::new(0, 0, 2, 2));
    }

    #[test]
    fn same_content_at_different_offsets_are_distinct_units() {
        let sources = [src(&dot(4, 4, 1, 1)), src(&dot(4, 4, 2, 2))];
        let diced = dice(&sources, &pref(4, 0)).unwrap();
        assert_ne!(diced[0].units[0].id, diced[1].units[0].id);
        assert_eq!(diced[0].units[0].pack_rect, URect::new(0, 0, 3, 3));
        assert_eq!(diced[1].units[0].pack_rect, URect::new(1, 1, 3, 3));
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
        let pixels = dice1(&BGRT, 1, 1)
            .units
            .into_iter()
            .map(|u| u.pixels)
            .collect::<Vec<_>>();
        #[rustfmt::skip]
        assert!(pixels.contains(&vec![
            B, B, G,
            B, B, G,
            R, R, T]));
    }

    #[test]
    fn padded_pixels_surround_rect() {
        let unit = &dice1(&dot(5, 5, 2, 2), 5, 1).units[0];
        assert_eq!(unit.pixels.len(), 49);
        assert_eq!(unit.pixels[24], M);
        assert_eq!(unit.pixels.iter().filter(|p| **p == M).count(), 1);
    }

    #[test]
    fn diced_texture_contains_identical_units() {
        assert_eq!(16, dice1(&RGB4X4, 1, 0).units.len());
        assert_eq!(16, dice1(&PLT4X4, 1, 0).units.len());
    }

    #[test]
    fn unique_doesnt_count_identical_units() {
        assert_eq!(3, dice1(&RGB4X4, 1, 0).unique.len());
        assert_eq!(16, dice1(&PLT4X4, 1, 0).unique.len());
    }

    #[test]
    fn reports_progress() {
        let progress = sample_progress(|p| drop(dice(&[src(&B1X1)], &p)));
        assert_eq!(progress.ratio, 2.0 / 6.0);
    }

    fn dice1(tex: &Texture, size: u32, pad: u32) -> DicedTexture {
        let pref = pref(size, pad);
        dice(&[src(tex)], &pref).unwrap().pop().unwrap()
    }

    fn pref(size: u32, pad: u32) -> Prefs {
        Prefs {
            unit_size: size,
            padding: pad,
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
        tex.units.iter().all(|u| u.pixels.iter().all(|p| p.a() > 0))
    }
}
