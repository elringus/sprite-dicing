use crate::layout::*;
use crate::models::*;
use std::collections::HashMap;

/// Packs diced textures into atlases.
pub(crate) fn pack(diced: Vec<DicedTexture>, prefs: &Prefs) -> Result<Vec<Atlas>> {
    if prefs.uv_inset > 0.5 {
        return Err(Error::Spec("UV inset should be in 0.0 to 0.5 range."));
    }
    if prefs.atlas_size_limit == 0 {
        return Err(Error::Spec("Atlas size limit can't be zero."));
    }
    if prefs.unit_size > prefs.atlas_size_limit {
        return Err(Error::Spec("Unit size can't be above atlas size limit."));
    }

    let total = diced.len();
    let mut atlases = vec![];
    let mut ctx = new_ctx(diced, prefs);
    while !ctx.to_pack.is_empty() {
        Progress::report(prefs, 3, total - ctx.to_pack.len(), total, "Packing units");
        atlases.push(pack_it(&mut ctx)?);
    }

    Ok(atlases)
}

struct Context {
    /// Relative inset of the unit UVs.
    inset: f32,
    /// Whether the atlases have to be square.
    square: bool,
    /// Whether the atlas sides have to be powers of two.
    pot: bool,
    /// Max side of an atlas, in pixels.
    limit: u32,
    /// Size of the padding stored around the units, in pixels.
    pad: u32,
    /// Total textures left to pack.
    to_pack: Vec<DicedTexture>,
    /// Visible rects of all the units, indexed by unit ID.
    visible: Vec<URect>,
    /// Whether the unit is packed into current atlas, indexed by unit ID.
    packed: Vec<bool>,
}

fn new_ctx(diced: Vec<DicedTexture>, prefs: &Prefs) -> Context {
    let units = || diced.iter().flat_map(|t| t.units.iter());
    let count = units().map(|u| u.id + 1).max().unwrap_or(0);
    let mut rects: Vec<Option<URect>> = vec![None; count];
    for unit in units() {
        let rect = &mut rects[unit.id];
        *rect = Some(rect.map_or(unit.visible, |r| r.union(&unit.visible)));
    }
    Context {
        inset: prefs.uv_inset,
        square: prefs.atlas_square,
        pot: prefs.atlas_pot,
        limit: prefs.atlas_size_limit,
        pad: prefs.padding,
        to_pack: diced,
        visible: rects.into_iter().map(|r| r.unwrap()).collect(),
        packed: vec![],
    }
}

/// Packs the next atlas.
fn pack_it(ctx: &mut Context) -> Result<Atlas> {
    ctx.packed = vec![false; ctx.visible.len()];

    let mut layout = None;
    while let Some(units) = find_packable_units(ctx) {
        match eval_layout_with(ctx, &units) {
            Some(fit) => layout = Some(fit),
            None => break,
        }
        for id in units {
            ctx.packed[id] = true;
        }
    }

    let Some(layout) = layout else {
        return Err(Error::Spec(
            "Can't fit single texture; increase atlas size limit.",
        ));
    };

    let packed = extract_packed_textures(ctx);
    let (texture, units) = bake_atlas(ctx, &layout, &packed);

    Ok(Atlas {
        texture,
        units,
        packed,
    })
}

/// Finds units not yet packed of the texture that adds the least padded area to current atlas;
/// returns none when all the textures are packed.
fn find_packable_units(ctx: &Context) -> Option<Vec<usize>> {
    let mut best: Option<(u64, Vec<usize>)> = None;
    for texture in ctx.to_pack.iter() {
        let new = texture
            .unique
            .iter()
            .copied()
            .filter(|&id| !ctx.packed[id])
            .collect::<Vec<_>>();
        if new.is_empty() {
            continue;
        }
        let area = new
            .iter()
            .map(|&id| padded_size(ctx, id))
            .map(|s| s.area())
            .sum::<u64>();
        if best.as_ref().is_none_or(|(min, _)| area < *min) {
            best = Some((area, new));
        }
    }
    best.map(|(_, new)| new)
}

/// Evaluates layout of the units packed into current atlas together with the specified ones.
fn eval_layout_with(ctx: &Context, units: &[usize]) -> Option<Layout> {
    let packed = (0..ctx.packed.len()).filter(|&id| ctx.packed[id]);
    let sizes = packed
        .chain(units.iter().copied())
        .map(|id| (id, padded_size(ctx, id)))
        .collect::<Vec<_>>();
    eval_layout(&sizes, ctx.limit, ctx.square, ctx.pot)
}

/// Size of the unit on the atlas, with padding.
fn padded_size(ctx: &Context, id: usize) -> USize {
    let rect = &ctx.visible[id];
    USize::new(rect.width + ctx.pad * 2, rect.height + ctx.pad * 2)
}

/// Returns the textures packed into current atlas out of the textures left to pack, in order.
fn extract_packed_textures(ctx: &mut Context) -> Vec<DicedTexture> {
    let mut packed = vec![];
    let mut left = vec![];
    for texture in ctx.to_pack.drain(..) {
        if texture.unique.iter().all(|&id| ctx.packed[id]) {
            packed.push(texture);
        } else {
            left.push(texture);
        }
    }
    ctx.to_pack = left;
    packed
}

/// Renders the laid out units into the atlas texture and maps their UVs.
fn bake_atlas(
    ctx: &Context,
    layout: &Layout,
    packed: &[DicedTexture],
) -> (Texture, HashMap<usize, PackedUnit>) {
    let mut texture = Texture {
        width: layout.size.width,
        height: layout.size.height,
        pixels: vec![Pixel::default(); (layout.size.width * layout.size.height) as usize],
    };

    let mut occurrences: HashMap<usize, Vec<&DicedUnit>> = HashMap::new();
    for unit in packed.iter().flat_map(|t| t.units.iter()) {
        occurrences.entry(unit.id).or_default().push(unit);
    }

    let mut units = HashMap::new();
    for (id, rect) in layout.rects.iter() {
        let visible = ctx.visible[*id];
        set_pixels(ctx, &mut texture, rect, &visible, &occurrences[id]);
        let uv = eval_uv(ctx, rect, &layout.size);
        units.insert(*id, PackedUnit { visible, uv });
    }

    (texture, units)
}

/// Copies the visible part of the unit (with padding) into the atlas rect.
fn set_pixels(
    ctx: &Context,
    atlas: &mut Texture,
    rect: &URect,
    visible: &URect,
    occurrences: &[&DicedUnit],
) {
    let stride = occurrences[0].cell.width + ctx.pad * 2;
    let mut pixels = Vec::with_capacity(occurrences.len());
    for y in 0..rect.height {
        for x in 0..rect.width {
            let from_idx = (visible.x + x + (visible.y + y) * stride) as usize;
            pixels.clear();
            pixels.extend(occurrences.iter().map(|o| o.pixels[from_idx]));
            atlas.pixels[(rect.x + x + atlas.width * (rect.y + y)) as usize] = median(&mut pixels);
        }
    }
}

/// Per-channel lower median of the pixels.
fn median(pixels: &mut [Pixel]) -> Pixel {
    let mut raw = [0; 4];
    for (channel, value) in raw.iter_mut().enumerate() {
        pixels.sort_unstable_by_key(|p| p.to_raw()[channel]);
        *value = pixels[(pixels.len() - 1) / 2].to_raw()[channel];
    }
    Pixel::from_raw(raw)
}

/// Evaluates UV rect of the visible part inside the padded atlas rect, inset per axis.
fn eval_uv(ctx: &Context, rect: &URect, atlas: &USize) -> FRect {
    let x = (rect.x + ctx.pad) as f32 / atlas.width as f32;
    let y = (rect.y + ctx.pad) as f32 / atlas.height as f32;
    let width = (rect.width - ctx.pad * 2) as f32 / atlas.width as f32;
    let height = (rect.height - ctx.pad * 2) as f32 / atlas.height as f32;
    let dx = ctx.inset * (width / 2.0);
    let dy = ctx.inset * (height / 2.0);
    FRect::new(x + dx, y + dy, width - dx * 2.0, height - dy * 2.0)
}

#[cfg(test)]
mod tests {
    use crate::fixtures::*;
    use crate::models::*;

    #[test]
    fn can_pack_with_defaults() {
        pack(vec![&R1X1, &B1X1], &Prefs::default());
    }

    #[test]
    #[should_panic(expected = "UV inset should be in 0.0 to 0.5 range.")]
    fn errs_when_inset_above_05() {
        let prefs = Prefs {
            uv_inset: 0.85,
            ..defaults()
        };
        pack(vec![&BGRT], &prefs);
    }

    #[test]
    #[should_panic(expected = "Atlas size limit can't be zero.")]
    fn errs_when_limit_is_zero() {
        let prefs = Prefs {
            atlas_size_limit: 0,
            ..defaults()
        };
        pack(vec![&BGRT], &prefs);
    }

    #[test]
    #[should_panic(expected = "Unit size can't be above atlas size limit.")]
    fn errs_when_unit_size_above_limit() {
        let prefs = Prefs {
            unit_size: 2,
            atlas_size_limit: 1,
            ..defaults()
        };
        pack(vec![&BGRT], &prefs);
    }

    #[test]
    fn when_empty_input_empty_vec_is_returned() {
        assert_eq!(pack(vec![], &Prefs::default()).len(), 0);
    }

    #[test]
    fn when_content_doesnt_fit_multiple_atlases_are_produced() {
        let prefs = Prefs {
            atlas_size_limit: 1,
            ..defaults()
        };
        assert_eq!(pack(vec![&B1X1, &R1X1], &prefs).len(), 2);
    }

    #[test]
    #[should_panic(expected = "Can't fit single texture; increase atlas size limit.")]
    fn errs_when_content_from_single_texture_doesnt_fit() {
        let prefs = Prefs {
            atlas_size_limit: 1,
            ..defaults()
        };
        pack(vec![&BGRT], &prefs);
    }

    #[test]
    fn packed_textures_keep_input_order() {
        let atlases = pack(vec![&RGBY, &B1X1, &C1X1], &defaults());
        let counts = atlases[0]
            .packed
            .iter()
            .map(|t| t.units.len())
            .collect::<Vec<_>>();
        assert_eq!(counts, vec![4, 1, 1]);
    }

    #[test]
    fn when_square_is_optimal_atlas_is_square() {
        let prefs = Prefs {
            atlas_size_limit: 4,
            ..defaults()
        };
        let atlas = pack(vec![&RGBY, &B1X1], &prefs).pop().unwrap();
        assert_eq!(atlas.texture.width, 2);
        assert_eq!(atlas.texture.height, 2);
    }

    #[test]
    fn when_square_is_not_optimal_atlas_is_not_square() {
        let prefs = Prefs {
            atlas_size_limit: 4,
            ..defaults()
        };
        let atlas = pack(vec![&RGBY, &C1X1], &prefs).pop().unwrap();
        assert_eq!(atlas.texture.width, 3);
        assert_eq!(atlas.texture.height, 2);
    }

    #[test]
    fn when_square_is_not_optimal_but_forced_atlas_is_square() {
        let prefs = Prefs {
            atlas_size_limit: 4,
            atlas_square: true,
            ..defaults()
        };
        let atlas = pack(vec![&RGBY, &C1X1], &prefs).pop().unwrap();
        assert_eq!(atlas.texture.width, 3);
        assert_eq!(atlas.texture.height, 3);
    }

    #[test]
    fn when_pot_forced_atlas_is_power_of_two() {
        let prefs = Prefs {
            atlas_size_limit: 4,
            atlas_pot: true,
            ..defaults()
        };
        let atlas = pack(vec![&RGBY, &C1X1], &prefs).pop().unwrap();
        assert_eq!(atlas.texture.width, 4);
        assert_eq!(atlas.texture.height, 4);
    }

    #[test]
    fn unused_pixels_are_clear() {
        let prefs = Prefs {
            atlas_size_limit: 4,
            atlas_pot: true,
            ..defaults()
        };
        let atlas = pack(vec![&RGBY, &C1X1], &prefs).pop().unwrap();
        let clear = atlas.texture.pixels.into_iter().filter(|p| p.eq(&T));
        assert_eq!(clear.count(), 11);
    }

    #[test]
    fn units_are_stored_once_per_atlas() {
        let atlas = pack(vec![&RGB4X4, &PLT4X4], &defaults()).pop().unwrap();
        assert_eq!(atlas.units.len(), 19);
        assert_eq!(atlas.texture.width * atlas.texture.height, 19);
    }

    #[test]
    fn trimmed_units_are_packed_as_rects() {
        let prefs = Prefs {
            unit_size: 5,
            ..defaults()
        };
        let atlas = pack(vec![&dot(5, 5, 2, 2)], &prefs).pop().unwrap();
        assert_eq!(atlas.texture.width, 3);
        assert_eq!(atlas.texture.height, 3);
        assert_eq!(atlas.texture.pixels[4], M);
        assert_eq!(atlas.units[&0].visible, URect::new(1, 1, 3, 3));
        assert_eq!(atlas.units[&0].uv, FRect::new(0.0, 0.0, 1.0, 1.0));
    }

    #[test]
    fn visible_rect_covers_all_occurrences() {
        let prefs = Prefs {
            unit_size: 3,
            ..defaults()
        };
        let alone = dot(3, 3, 0, 0);
        let atlas = pack(vec![&alone], &prefs).pop().unwrap();
        assert_eq!(atlas.units[&0].visible, URect::new(0, 0, 2, 2));
        let mut with_neighbor = dot(6, 3, 0, 0);
        with_neighbor.pixels[3 + 2 * 6] = M;
        let atlas = pack(vec![&alone, &with_neighbor], &prefs).pop().unwrap();
        assert_eq!(atlas.units[&0].visible, URect::new(0, 0, 3, 3));
        assert_eq!(atlas.units[&1].visible, URect::new(0, 1, 2, 2));
    }

    #[test]
    fn uvs_are_mapped() {
        let atlas = pack(vec![&R1X1], &defaults()).pop().unwrap();
        let rect = &atlas.units.values().next().unwrap().uv;
        assert_eq!(*rect, FRect::new(0.0, 0.0, 1.0, 1.0));
    }

    #[test]
    fn uvs_map_content_inside_padding() {
        let prefs = Prefs {
            unit_size: 2,
            padding: 1,
            ..defaults()
        };
        let atlas = pack(vec![&M1X1], &prefs).pop().unwrap();
        assert_eq!(atlas.texture.width, 3);
        assert_eq!(atlas.texture.height, 3);
        let rect = &atlas.units.values().next().unwrap().uv;
        assert_eq!(
            *rect,
            FRect::new(1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0)
        );
    }

    #[test]
    fn inset_uvs_are_scaled() {
        let prefs = Prefs {
            uv_inset: 0.2,
            ..defaults()
        };
        let atlas = pack(vec![&Y1X1], &prefs).pop().unwrap();
        let rect = &atlas.units.values().next().unwrap().uv;
        assert_eq!(*rect, FRect::new(0.1, 0.1, 0.8, 0.8));
    }

    #[test]
    fn inset_is_applied_per_axis() {
        let prefs = Prefs {
            unit_size: 3,
            uv_inset: 0.5,
            atlas_square: true,
            ..defaults()
        };
        let atlas = pack(vec![&RGB3X1], &prefs).pop().unwrap();
        let rect = &atlas.units.values().next().unwrap().uv;
        let height = 1.0f32 / 3.0;
        assert_eq!(*rect, FRect::new(0.25, height / 4.0, 0.5, height / 2.0));
    }

    #[test]
    fn padding_of_deduplicated_unit_is_median_of_occurrences() {
        let reds = [0, 0, 99, 101, 100];
        let mut pixels = vec![];
        for red in reds {
            pixels.extend([M, Pixel::new(red, 0, 0, 255)]);
        }
        let tex = Texture {
            width: 2,
            height: 5,
            pixels,
        };
        let prefs = Prefs {
            padding: 1,
            ..defaults()
        };
        let atlas = pack(vec![&tex], &prefs).pop().unwrap();
        let unit = atlas.packed[0].units[0].id;
        let rect = &atlas.units[&unit].uv;
        let x = (rect.x * atlas.texture.width as f32).round() as u32;
        let y = (rect.y * atlas.texture.height as f32).round() as u32;
        let ring = atlas.texture.pixels[(x + 1 + (y) * atlas.texture.width) as usize];
        assert_eq!(ring, Pixel::new(99, 0, 0, 255));
    }

    #[test]
    fn packed_units_dont_overlap_and_fit_atlas() {
        let prefs = Prefs {
            unit_size: 3,
            padding: 1,
            atlas_size_limit: 24,
            ..Prefs::default()
        };
        let textures = (0..6).map(|i| noise(7 + i, 11 - i, i)).collect::<Vec<_>>();
        let sources = textures.iter().map(|t| t as &dyn AnySource).collect();
        let atlases = pack(sources, &prefs);
        assert!(atlases.len() > 1);
        for atlas in atlases {
            let (w, h) = (atlas.texture.width as f32, atlas.texture.height as f32);
            let padded = atlas
                .units
                .values()
                .map(|u| &u.uv)
                .map(|r| {
                    let x = (r.x * w).round() as i32 - 1;
                    let y = (r.y * h).round() as i32 - 1;
                    let width = (r.width * w).round() as i32 + 2;
                    let height = (r.height * h).round() as i32 + 2;
                    (x, y, width, height)
                })
                .collect::<Vec<_>>();
            for (i, a) in padded.iter().enumerate() {
                assert!(a.0 >= 0 && a.1 >= 0);
                assert!(a.0 + a.2 <= atlas.texture.width as i32);
                assert!(a.1 + a.3 <= atlas.texture.height as i32);
                for b in padded.iter().skip(i + 1) {
                    let apart = a.0 >= b.0 + b.2 || b.0 >= a.0 + a.2;
                    let apart = apart || a.1 >= b.1 + b.3 || b.1 >= a.1 + a.3;
                    assert!(apart, "{a:?} overlaps {b:?}");
                }
            }
        }
    }

    #[test]
    fn reports_progress() {
        let progress = sample_progress(|p| drop(pack(vec![&M1X1], &p)));
        assert_eq!(progress.ratio, 4.0 / 6.0);
    }

    fn pack(src: Vec<&dyn AnySource>, prefs: &Prefs) -> Vec<Atlas> {
        let sprites = src.into_iter().map(|s| s.sprite()).collect::<Vec<_>>();
        let diced = crate::dicer::dice(&sprites, prefs).unwrap();
        crate::packer::pack(diced, prefs).unwrap()
    }

    fn defaults() -> Prefs {
        Prefs {
            unit_size: 1,
            padding: 0,
            ..Prefs::default()
        }
    }
}
