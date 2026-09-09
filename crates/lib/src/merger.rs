use crate::models::*;
use std::cmp;
use std::collections::HashMap;

/// Merges units that always occur together into blocks, as long as the blocks
/// don't take more atlas space than the units.
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
    /// Units mapped by texture index and cell position.
    cells: HashMap<(usize, u32, u32), &'a DicedUnit>,
    /// Texture index and unit of all the occurrences of the units, indexed by unit ID.
    occurrences: Vec<Vec<(usize, &'a DicedUnit)>>,
    /// IDs of the merged units, anchor first, indexed by block ID.
    blocks: Vec<Vec<usize>>,
    /// ID of the block the unit is merged into, indexed by unit ID.
    block_ids: Vec<Option<usize>>,
}

fn new_ctx<'a>(diced: &'a [DicedTexture], prefs: &Prefs) -> Context<'a> {
    let units = || {
        diced
            .iter()
            .enumerate()
            .flat_map(|(idx, t)| t.units.iter().map(move |u| (idx, u)))
    };
    let count = units().map(|(_, u)| u.id + 1).max().unwrap_or(0);
    let mut cells = HashMap::new();
    let mut occurrences = vec![vec![]; count];
    for (idx, unit) in units() {
        cells.insert((idx, unit.cell.x, unit.cell.y), unit);
        occurrences[unit.id].push((idx, unit));
    }
    Context {
        unit: prefs.unit_size,
        pad: prefs.padding,
        limit: prefs.atlas_size_limit / 2,
        cells,
        occurrences,
        blocks: vec![],
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
            ctx.blocks.push(ids);
        }
        let block_id = ctx.block_ids[unit.id].unwrap();
        if ctx.blocks[block_id][0] == unit.id {
            units.push(new_unit(block_id, idx, &unit.cell, ctx));
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
            let grown = find_cells(anchor, &s, ctx)?;
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

/// Finds IDs of the units filling a block with specified size in cells anchored at the unit,
/// in row-major order; none when a cell is empty, merged or doesn't co-occur with the anchor.
fn find_cells(anchor: usize, size: &USize, ctx: &Context) -> Option<Vec<usize>> {
    if cmp::max(size.width, size.height) * ctx.unit > ctx.limit {
        return None;
    }
    let (tex, first) = ctx.occurrences[anchor][0];
    let mut ids = Vec::with_capacity((size.width * size.height) as usize);
    for j in 0..size.height {
        for i in 0..size.width {
            let (dx, dy) = (i * ctx.unit, j * ctx.unit);
            let id = ctx
                .cells
                .get(&(tex, first.cell.x + dx, first.cell.y + dy))?
                .id;
            let shifted = ctx.occurrences[anchor]
                .iter()
                .map(|&(t, u)| (t, u.cell.x + dx, u.cell.y + dy));
            let actual = ctx.occurrences[id]
                .iter()
                .map(|&(t, u)| (t, u.cell.x, u.cell.y));
            if ctx.block_ids[id].is_some() || !shifted.eq(actual) {
                return None;
            }
            ids.push(id);
        }
    }
    Some(ids)
}

/// Evaluates atlas area taken by the units stored as a single block.
fn eval_area(ids: &[usize], ctx: &Context) -> u64 {
    let visible = eval_visible(ids, ctx);
    USize::new(visible.width + ctx.pad * 2, visible.height + ctx.pad * 2).area()
}

/// Unions visible rects of the units with specified IDs over all their occurrences into a block.
fn eval_visible(ids: &[usize], ctx: &Context) -> URect {
    ids.iter()
        .flat_map(|&id| {
            let (dx, dy) = eval_offset(id, ids[0], ctx);
            ctx.occurrences[id].iter().map(move |(_, u)| {
                URect::new(
                    u.visible.x + dx,
                    u.visible.y + dy,
                    u.visible.width,
                    u.visible.height,
                )
            })
        })
        .reduce(|a, b| a.union(&b))
        .unwrap()
}

/// Evaluates position of the unit relative to the anchor unit inside a block.
fn eval_offset(id: usize, anchor: usize, ctx: &Context) -> (u32, u32) {
    let (cell, origin) = (
        ctx.occurrences[id][0].1.cell,
        ctx.occurrences[anchor][0].1.cell,
    );
    (cell.x - origin.x, cell.y - origin.y)
}

/// Builds unit of the block occurring at specified anchor cell of the texture.
fn new_unit(id: usize, tex: usize, at: &URect, ctx: &Context) -> DicedUnit {
    let ids = &ctx.blocks[id];
    let members = ids
        .iter()
        .map(|&member| {
            let (dx, dy) = eval_offset(member, ids[0], ctx);
            ctx.cells[&(tex, at.x + dx, at.y + dy)]
        })
        .collect::<Vec<_>>();
    let cell = members
        .iter()
        .map(|m| m.cell)
        .reduce(|a, b| a.union(&b))
        .unwrap();
    let width = cell.width + ctx.pad * 2;
    let height = cell.height + ctx.pad * 2;
    let mut pixels = vec![Pixel::default(); (width * height) as usize];
    for member in members {
        let stride = member.cell.width + ctx.pad * 2;
        for (idx, pixel) in member.pixels.iter().enumerate() {
            let x = member.cell.x - cell.x + idx as u32 % stride;
            let y = member.cell.y - cell.y + idx as u32 / stride;
            pixels[(x + y * width) as usize] = *pixel;
        }
    }
    DicedUnit {
        id,
        cell,
        visible: eval_visible(ids, ctx),
        pixels,
    }
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
        assert_eq!(unit.cell, URect::new(0, 0, 2, 2));
        assert_eq!(unit.visible, URect::new(0, 0, 2, 2));
        assert_eq!(unit.pixels, vec![R, G, B, Y]);
    }

    #[test]
    fn units_with_distinct_occurrences_are_not_merged() {
        let merged = merge(vec![&RGBY, &B1X1], &defaults());
        let cells = merged[0].units.iter().map(|u| u.cell).collect::<Vec<_>>();
        assert_eq!(
            cells,
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
        assert_eq!(merged[0].units[0].cell, URect::new(0, 0, 2, 2));
        assert_eq!(merged[1].units[0].cell, URect::new(1, 1, 2, 2));
        assert_eq!(merged[0].units[0].pixels, merged[1].units[0].pixels);
    }

    #[test]
    fn blocks_grow_wider_before_taller() {
        let merged = merge(vec![&BGRT], &defaults());
        let cells = merged[0].units.iter().map(|u| u.cell).collect::<Vec<_>>();
        assert_eq!(cells, vec![URect::new(0, 0, 2, 1), URect::new(0, 1, 1, 1)]);
    }

    #[test]
    fn clipped_cells_are_merged() {
        let prefs = Prefs {
            unit_size: 2,
            ..defaults()
        };
        let merged = merge(vec![&RGB3X1], &prefs);
        assert_eq!(merged[0].units.len(), 1);
        assert_eq!(merged[0].units[0].cell, URect::new(0, 0, 3, 1));
        assert_eq!(merged[0].units[0].pixels, vec![G, R, B]);
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
    fn visible_rect_of_block_covers_members() {
        let prefs = Prefs {
            unit_size: 2,
            ..defaults()
        };
        let mut tex = dot(4, 2, 0, 0);
        tex.pixels[3] = M;
        let merged = merge(vec![&tex], &prefs);
        assert_eq!(merged[0].units.len(), 1);
        assert_eq!(merged[0].units[0].cell, URect::new(0, 0, 4, 2));
        assert_eq!(merged[0].units[0].visible, URect::new(0, 0, 4, 2));
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
            padding: 0,
            ..Prefs::default()
        }
    }
}
