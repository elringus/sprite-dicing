use crate::models::*;
use std::cmp;
use std::collections::{HashMap, HashSet};

/// Merges units that always occur together into blocks and adjacent solid units into rects.
pub(crate) fn merge(diced: &[DicedTexture], prefs: &Prefs) -> Vec<DicedTexture> {
    let mut merged = Vec::with_capacity(diced.len());
    let mut ctx = new_ctx(diced, prefs);
    for (idx, texture) in diced.iter().enumerate() {
        Progress::report(prefs, 2, idx, diced.len(), "Merging diced units");
        merged.push(merge_it(idx, texture, &mut ctx));
    }
    merged
}

struct Context<'a> {
    /// Size of a unit, in pixels.
    unit: u32,
    /// Size of the padding stored around the units, in pixels.
    pad: u32,
    /// Max side of a block, in pixels.
    limit: u32,
    /// Units mapped by texture index and position.
    units: HashMap<(usize, u32, u32), &'a DicedUnit>,
    /// Occurrences of the units, as texture index and unit, indexed by unit ID.
    occurrences: Vec<Vec<(usize, &'a DicedUnit)>>,
    /// Color of the units that are a single color (padding included), indexed by unit ID.
    solid: Vec<Option<Pixel>>,
    /// Positions of the solid occurrences coalesced so far, keyed as the units.
    coalesced: HashSet<(usize, u32, u32)>,
    /// IDs of the merged units, anchor first, indexed by block ID.
    blocks: Vec<Vec<usize>>,
    /// Pack rects of the blocks, indexed by block ID.
    pack_rects: Vec<URect>,
    /// ID of the block the unit is merged into, indexed by unit ID.
    block_ids: Vec<Option<usize>>,
}

fn new_ctx<'a>(diced: &'a [DicedTexture], prefs: &Prefs) -> Context<'a> {
    let all = || {
        diced
            .iter()
            .enumerate()
            .flat_map(|(idx, t)| t.units.iter().map(move |u| (idx, u)))
    };
    let count = all().map(|(_, u)| u.id + 1).max().unwrap_or(0);
    let mut units = HashMap::new();
    let mut occurrences = vec![vec![]; count];
    for (idx, unit) in all() {
        units.insert((idx, unit.src_rect.x, unit.src_rect.y), unit);
        occurrences[unit.id].push((idx, unit));
    }
    let solid = occurrences
        .iter()
        .map(|o| get_solid(o, prefs.padding))
        .collect();
    Context {
        unit: prefs.unit_size,
        pad: prefs.padding,
        limit: prefs.atlas_size_limit / 2,
        units,
        occurrences,
        solid,
        coalesced: HashSet::new(),
        blocks: vec![],
        pack_rects: vec![],
        block_ids: vec![None; count],
    }
}

fn merge_it(idx: usize, texture: &DicedTexture, ctx: &mut Context) -> DicedTexture {
    let mut units = vec![];
    for unit in texture.units.iter() {
        if ctx.block_ids[unit.id].is_none() {
            let ids = grow(unit.id, ctx);
            for &id in ids.iter() {
                ctx.block_ids[id] = Some(ctx.blocks.len());
            }
            ctx.pack_rects.push(eval_pack_rect(&ids, ctx));
            ctx.blocks.push(ids);
        }
        let block_id = ctx.block_ids[unit.id].unwrap();
        if let Some(color) = ctx.solid[unit.id] {
            if let Some(src_rect) = coalesce(idx, unit, ctx) {
                units.push(new_solid(block_id, src_rect, color));
            }
        } else if ctx.blocks[block_id][0] == unit.id {
            units.push(new_unit(block_id, idx, &unit.src_rect, ctx));
        }
    }

    let mut unique = units.iter().map(|u| u.id).collect::<Vec<_>>();
    unique.sort_unstable();
    unique.dedup();

    DicedTexture {
        id: texture.id.to_owned(),
        size: texture.size,
        pivot: texture.pivot.to_owned(),
        units,
        unique,
    }
}

/// Grows a block anchored at the unit by whole columns and rows of co-occurring units,
/// while the block takes no more atlas space than the units; returns IDs of the merged units.
fn grow(anchor: usize, ctx: &Context) -> Vec<usize> {
    let mut size = USize::new(1, 1);
    let mut ids = vec![anchor];
    loop {
        let wider = USize::new(size.width + 1, size.height);
        let taller = USize::new(size.width, size.height + 1);
        let grown = [wider, taller].into_iter().find_map(|s| {
            let grown = find_units(anchor, &s, ctx)?;
            let added = grown.iter().filter(|id| !ids.contains(id));
            let separate =
                eval_area(&ids, ctx) + added.map(|&id| eval_area(&[id], ctx)).sum::<u64>();
            (eval_area(&grown, ctx) <= separate).then_some((s, grown))
        });
        match grown {
            Some(grown) => (size, ids) = grown,
            None => return ids,
        }
    }
}

/// Finds IDs of the units filling a block with specified size (in units) anchored at the unit, in
/// row-major order; none when a unit is missing, merged, solid or doesn't co-occur with the anchor.
fn find_units(anchor: usize, size: &USize, ctx: &Context) -> Option<Vec<usize>> {
    if cmp::max(size.width, size.height) * ctx.unit > ctx.limit {
        return None;
    }
    let (tex, first) = ctx.occurrences[anchor][0];
    let mut ids = Vec::with_capacity((size.width * size.height) as usize);
    for j in 0..size.height {
        for i in 0..size.width {
            let (dx, dy) = (i * ctx.unit, j * ctx.unit);
            let id = ctx
                .units
                .get(&(tex, first.src_rect.x + dx, first.src_rect.y + dy))?
                .id;
            let shifted = ctx.occurrences[anchor]
                .iter()
                .map(|&(t, u)| (t, u.src_rect.x + dx, u.src_rect.y + dy));
            let actual = ctx.occurrences[id]
                .iter()
                .map(|&(t, u)| (t, u.src_rect.x, u.src_rect.y));
            if ctx.block_ids[id].is_some() || ctx.solid[id].is_some() || !shifted.eq(actual) {
                return None;
            }
            ids.push(id);
        }
    }
    Some(ids)
}

/// Evaluates atlas area taken by the units stored as a single block.
fn eval_area(ids: &[usize], ctx: &Context) -> u64 {
    let pack_rect = eval_pack_rect(ids, ctx);
    USize::new(
        pack_rect.width + ctx.pad * 2,
        pack_rect.height + ctx.pad * 2,
    )
    .area()
}

/// Unions pack rects of the units with specified IDs over all their occurrences into a block.
fn eval_pack_rect(ids: &[usize], ctx: &Context) -> URect {
    ids.iter()
        .flat_map(|&id| {
            let (dx, dy) = eval_offset(id, ids[0], ctx);
            ctx.occurrences[id].iter().map(move |(_, u)| {
                URect::new(
                    u.pack_rect.x + dx,
                    u.pack_rect.y + dy,
                    u.pack_rect.width,
                    u.pack_rect.height,
                )
            })
        })
        .reduce(|a, b| a.union(&b))
        .unwrap()
}

/// Evaluates position of the unit relative to the anchor unit inside a block.
fn eval_offset(id: usize, anchor: usize, ctx: &Context) -> (u32, u32) {
    let (rect, origin) = (
        ctx.occurrences[id][0].1.src_rect,
        ctx.occurrences[anchor][0].1.src_rect,
    );
    (rect.x - origin.x, rect.y - origin.y)
}

/// Builds unit of the block occurring at specified anchor rect of the texture.
fn new_unit(id: usize, tex: usize, at: &URect, ctx: &Context) -> DicedUnit {
    let ids = &ctx.blocks[id];
    let members = ids
        .iter()
        .map(|&member| {
            let (dx, dy) = eval_offset(member, ids[0], ctx);
            ctx.units[&(tex, at.x + dx, at.y + dy)]
        })
        .collect::<Vec<_>>();
    let src_rect = members
        .iter()
        .map(|m| m.src_rect)
        .reduce(|a, b| a.union(&b))
        .unwrap();
    let width = src_rect.width + ctx.pad * 2;
    let height = src_rect.height + ctx.pad * 2;
    let mut pixels = vec![Pixel::default(); (width * height) as usize];
    for member in members {
        let stride = member.src_rect.width + ctx.pad * 2;
        for (idx, pixel) in member.pixels.iter().enumerate() {
            let x = member.src_rect.x - src_rect.x + idx as u32 % stride;
            let y = member.src_rect.y - src_rect.y + idx as u32 / stride;
            pixels[(x + y * width) as usize] = *pixel;
        }
    }
    DicedUnit {
        id,
        src_rect,
        pack_rect: ctx.pack_rects[id],
        pixels,
    }
}

/// Builds unit of the solid block occurring at specified rect of the texture.
fn new_solid(id: usize, src_rect: URect, color: Pixel) -> DicedUnit {
    DicedUnit {
        id,
        src_rect,
        pack_rect: URect::new(0, 0, src_rect.width, src_rect.height),
        pixels: vec![color],
    }
}

/// Returns the single solid color when the specified units are uniformly colored
/// and their median padding has the same color; None otherwise.
fn get_solid(units: &[(usize, &DicedUnit)], pad: u32) -> Option<Pixel> {
    let (_, first) = units[0];
    let stride = first.src_rect.width + pad * 2;
    let color = first.pixels[(pad + pad * stride) as usize];
    let mut pixels = Vec::with_capacity(units.len());
    let solid = (0..first.pixels.len()).all(|idx| {
        pixels.clear();
        pixels.extend(units.iter().map(|(_, u)| u.pixels[idx]));
        Pixel::median(&mut pixels) == color
    });
    solid.then_some(color)
}

/// Coalesces the occurrence of the solid unit with the adjacent occurrences not coalesced
/// yet, extending right and then down; returns the rect, or none when the occurrence is
/// coalesced already.
fn coalesce(tex: usize, unit: &DicedUnit, ctx: &mut Context) -> Option<URect> {
    let free = |x, y| {
        let at = (tex, x, y);
        ctx.units.get(&at).is_some_and(|u| u.id == unit.id) && !ctx.coalesced.contains(&at)
    };
    if !free(unit.src_rect.x, unit.src_rect.y) {
        return None;
    }
    let mut rect = unit.src_rect;
    let (width, height) = (unit.src_rect.width, unit.src_rect.height);
    while free(rect.x + rect.width, rect.y) {
        rect.width += width;
    }
    let columns = |rect: &URect| (rect.x..rect.x + rect.width).step_by(width as usize);
    while columns(&rect).all(|x| free(x, rect.y + rect.height)) {
        rect.height += height;
    }
    for y in (rect.y..rect.y + rect.height).step_by(height as usize) {
        for x in columns(&rect) {
            ctx.coalesced.insert((tex, x, y));
        }
    }
    Some(rect)
}

#[cfg(test)]
mod tests {
    use crate::fixtures::*;
    use crate::models::*;

    #[test]
    fn co_occurring_units_are_merged() {
        let merged = merge(vec![&RGBY], &defaults());
        assert_eq!(merged[0].units.len(), 1);
        assert_eq!(merged[0].unique, vec![0]);
        let unit = &merged[0].units[0];
        assert_eq!(unit.id, 0);
        assert_eq!(unit.src_rect, URect::new(0, 0, 2, 2));
        assert_eq!(unit.pack_rect, URect::new(0, 0, 2, 2));
        assert!(!unit.is_solid());
    }

    #[test]
    fn units_with_distinct_occurrences_are_not_merged() {
        let merged = merge(vec![&RGBY, &B1X1], &defaults());
        let rects = merged[0]
            .units
            .iter()
            .map(|u| u.src_rect)
            .collect::<Vec<_>>();
        assert_eq!(
            rects,
            vec![
                URect::new(0, 0, 2, 1),
                URect::new(0, 1, 1, 1),
                URect::new(1, 1, 1, 1)
            ]
        );
        assert_eq!(merged[0].unique, vec![0, 1, 2]);
        assert_eq!(merged[1].units[0].id, 1);
    }

    #[test]
    fn units_occurring_at_distinct_offsets_are_not_merged() {
        let reordered = Texture {
            width: 3,
            height: 1,
            pixels: vec![R, G, B],
        };
        let merged = merge(vec![&RGB3X1, &reordered], &defaults());
        assert_eq!(merged[0].units.len(), 3);
        assert_eq!(merged[1].units.len(), 3);
    }

    #[test]
    fn merged_units_share_id_across_textures() {
        let mut shifted = Texture {
            width: 3,
            height: 3,
            pixels: vec![T; 9],
        };
        shifted.pixels[4..6].copy_from_slice(&[R, G]);
        shifted.pixels[7..9].copy_from_slice(&[B, Y]);
        let merged = merge(vec![&RGBY, &shifted], &defaults());
        assert_eq!(merged[0].units.len(), 1);
        assert_eq!(merged[1].units.len(), 1);
        assert_eq!(merged[0].units[0].id, merged[1].units[0].id);
        assert_eq!(merged[0].units[0].src_rect, URect::new(0, 0, 2, 2));
        assert_eq!(merged[1].units[0].src_rect, URect::new(1, 1, 2, 2));
    }

    #[test]
    fn blocks_grow_wider_before_taller() {
        let merged = merge(vec![&BGRT], &defaults());
        let rects = merged[0]
            .units
            .iter()
            .map(|u| u.src_rect)
            .collect::<Vec<_>>();
        assert_eq!(rects, vec![URect::new(0, 0, 2, 1), URect::new(0, 1, 1, 1)]);
    }

    #[test]
    fn clipped_units_are_merged() {
        let prefs = Prefs {
            unit_size: 2,
            ..defaults()
        };
        let merged = merge(vec![&RGB3X1], &prefs);
        assert_eq!(merged[0].units.len(), 1);
        assert_eq!(merged[0].units[0].src_rect, URect::new(0, 0, 3, 1));
        #[rustfmt::skip]
        assert_eq!(
            merged[0].units[0].pixels,
            vec![G, G, R, B, B,
                 G, G, R, B, B,
                 G, G, R, B, B]);
    }

    #[test]
    fn block_pixels_include_padding() {
        let prefs = Prefs {
            padding: 1,
            ..defaults()
        };
        let merged = merge(vec![&RGBY], &prefs);
        #[rustfmt::skip]
        assert_eq!(
            merged[0].units[0].pixels,
            vec![R, R, G, G,
                 R, R, G, G,
                 B, B, Y, Y,
                 B, B, Y, Y]);
    }

    #[test]
    fn pack_rect_of_block_covers_members() {
        let prefs = Prefs {
            unit_size: 2,
            ..defaults()
        };
        let mut tex = dot(4, 2, 0, 0);
        tex.pixels[3] = M;
        let merged = merge(vec![&tex], &prefs);
        assert_eq!(merged[0].units.len(), 1);
        assert_eq!(merged[0].units[0].src_rect, URect::new(0, 0, 4, 2));
        assert_eq!(merged[0].units[0].pack_rect, URect::new(0, 0, 4, 2));
    }

    #[test]
    fn merging_stops_when_atlas_area_grows() {
        let prefs = Prefs {
            unit_size: 16,
            ..defaults()
        };
        let full = noise(32, 16, 0);
        let mut sparse = full.clone();
        for y in 0..16 {
            for x in 14..31 {
                sparse.pixels[x + y * 32] = T;
            }
        }
        assert_eq!(merge(vec![&full], &prefs)[0].units.len(), 1);
        assert_eq!(merge(vec![&sparse], &prefs)[0].units.len(), 2);
    }

    #[test]
    fn blocks_are_capped_at_half_atlas_size_limit() {
        let prefs = Prefs {
            atlas_size_limit: 4,
            ..defaults()
        };
        assert_eq!(merge(vec![&RGBY], &prefs)[0].units.len(), 1);
        let prefs = Prefs {
            atlas_size_limit: 2,
            ..defaults()
        };
        assert_eq!(merge(vec![&RGBY], &prefs)[0].units.len(), 4);
    }

    #[test]
    fn solid_units_are_coalesced_into_rects() {
        let prefs = Prefs {
            unit_size: 2,
            ..defaults()
        };
        let mut tex = fill(6, 6, R);
        for idx in [28, 29, 34, 35] {
            tex.pixels[idx] = G;
        }
        let merged = merge(vec![&tex], &prefs);
        let rects = merged[0]
            .units
            .iter()
            .map(|u| u.src_rect)
            .collect::<Vec<_>>();
        assert_eq!(
            rects,
            vec![
                URect::new(0, 0, 6, 4),
                URect::new(0, 4, 4, 2),
                URect::new(4, 4, 2, 2)
            ]
        );
        let solid = merged[0]
            .units
            .iter()
            .map(|u| u.is_solid())
            .collect::<Vec<_>>();
        assert_eq!(solid, vec![true, true, false]);
        assert_eq!(merged[0].units[0].pack_rect, URect::new(0, 0, 6, 4));
        assert_eq!(merged[0].units[0].pixels, vec![R]);
        assert_eq!(merged[0].unique, vec![0, 1]);
    }

    #[test]
    fn flat_units_with_distinct_neighbors_are_not_solid() {
        let prefs = Prefs {
            unit_size: 2,
            ..defaults()
        };
        #[rustfmt::skip]
        let tex = Texture { width: 6, height: 2, pixels: vec![
            R, R, G, G, B, B,
            R, R, G, G, B, B,
        ]};
        let merged = merge(vec![&tex], &prefs);
        assert_eq!(merged[0].units.len(), 1);
        assert!(!merged[0].units[0].is_solid());
    }

    #[test]
    fn solid_units_are_not_merged_into_blocks() {
        let prefs = Prefs {
            unit_size: 2,
            ..defaults()
        };
        #[rustfmt::skip]
        let tex = Texture { width: 4, height: 2, pixels: vec![
            R, R, R, G,
            R, R, R, B,
        ]};
        let merged = merge(vec![&tex], &prefs);
        let rects = merged[0]
            .units
            .iter()
            .map(|u| u.src_rect)
            .collect::<Vec<_>>();
        assert_eq!(rects, vec![URect::new(0, 0, 2, 2), URect::new(2, 0, 2, 2)]);
        let solid = merged[0]
            .units
            .iter()
            .map(|u| u.is_solid())
            .collect::<Vec<_>>();
        assert_eq!(solid, vec![true, false]);
        assert_eq!(merged[0].unique, vec![0, 1]);
    }

    #[test]
    fn solid_units_share_id_across_textures() {
        let merged = merge(vec![&RGBY, &fill(2, 2, B)], &defaults());
        assert_eq!(merged[1].units.len(), 1);
        assert_eq!(merged[1].units[0].src_rect, URect::new(0, 0, 2, 2));
        assert!(merged[1].units[0].is_solid());
        let blue = merged[0].units.iter().find(|u| u.is_solid()).unwrap();
        assert_eq!(blue.id, merged[1].units[0].id);
        assert_eq!(merged[0].unique, vec![0, 1, 2]);
    }

    #[test]
    fn clipped_units_are_coalesced() {
        let prefs = Prefs {
            unit_size: 2,
            ..defaults()
        };
        let merged = merge(vec![&fill(5, 4, R)], &prefs);
        let rects = merged[0]
            .units
            .iter()
            .map(|u| u.src_rect)
            .collect::<Vec<_>>();
        assert_eq!(rects, vec![URect::new(0, 0, 4, 4), URect::new(4, 0, 1, 4)]);
        assert!(merged[0].units.iter().all(|u| u.is_solid()));
    }

    #[test]
    fn without_padding_flat_units_are_solid() {
        let prefs = Prefs {
            padding: 0,
            ..defaults()
        };
        let merged = merge(vec![&RGBY], &prefs);
        assert_eq!(merged[0].units.len(), 4);
        assert!(merged[0].units.iter().all(|u| u.is_solid()));
        assert!(merged[0].units.iter().all(|u| u.pixels.len() == 1));
    }

    #[test]
    fn reports_progress() {
        let progress = sample_progress(|p| drop(merge(vec![&M1X1], &p)));
        assert_eq!(progress.ratio, 3.0 / 6.0);
    }

    fn merge(src: Vec<&dyn AnySource>, prefs: &Prefs) -> Vec<DicedTexture> {
        let sprites = src.into_iter().map(|s| s.sprite()).collect::<Vec<_>>();
        let diced = crate::dicer::dice(&sprites, prefs).unwrap();
        crate::merger::merge(&diced, prefs)
    }

    fn defaults() -> Prefs {
        Prefs {
            unit_size: 1,
            padding: 1,
            ..Prefs::default()
        }
    }
}
