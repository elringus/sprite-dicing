use crate::models::*;
use std::collections::{HashMap, HashSet};

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

    let mut atlases = Vec::new();
    let mut ctx = new_ctx(diced, prefs);
    while !ctx.to_pack.is_empty() {
        atlases.push(pack_next(&mut ctx)?);
    }

    Ok(atlases)
}

struct Context {
    inset: f32,
    square: bool,
    pot: bool,
    size_limit: u32,
    unit_size: u32,
    pad: u32,
    padded_unit_size: u32,
    unit_capacity: u32,
    to_pack: Vec<DicedTexture>,
}

fn new_ctx(diced: Vec<DicedTexture>, prefs: &Prefs) -> Context {
    let padded_unit_size = prefs.unit_size + prefs.padding * 2;
    let unit_capacity = (prefs.atlas_size_limit / padded_unit_size).pow(2);
    Context {
        inset: prefs.uv_inset,
        square: prefs.atlas_square,
        pot: prefs.atlas_pot,
        size_limit: prefs.atlas_size_limit,
        unit_size: prefs.unit_size,
        pad: prefs.padding,
        padded_unit_size,
        unit_capacity,
        to_pack: diced,
    }
}

/// Current atlas packing context.
struct Packed {
    /// Indexes of diced textures (via ctx.to_pack) packed into current atlas.
    textures: HashSet<usize>,
    /// Units packed into current atlas mapped by hashes.
    units: HashMap<u64, UnitRef>,
}

/// Reference to a diced unit of a diced texture.
struct UnitRef {
    /// Index of the diced texture (via ctx.to_pack) containing referenced unit.
    texture_idx: usize,
    /// Index of the referenced diced unit inside diced texture.
    unit_idx: usize,
}

impl UnitRef {
    fn new(texture_idx: usize, unit_idx: usize) -> Self {
        UnitRef {
            texture_idx,
            unit_idx,
        }
    }
}

struct BakedAtlas {
    texture: Texture,
    rects: HashMap<u64, FRect>,
}

fn pack_next(ctx: &mut Context) -> Result<Atlas> {
    let mut pck = Packed {
        textures: HashSet::new(),
        units: HashMap::new(),
    };

    while let Some(texture_idx) = find_packable_texture(ctx, &pck) {
        pck.textures.insert(texture_idx);
        let units = ctx.to_pack[texture_idx].units.iter().enumerate();
        let refs = units.map(|(i, u)| (u.hash, UnitRef::new(texture_idx, i)));
        pck.units.extend(refs);
    }

    if pck.textures.is_empty() {
        return Err(Error::Spec("Can't fit any texture; increase atlas size."));
    }

    let atlas_size = eval_atlas_size(ctx, pck.units.len() as u32);
    let backed_atlas = bake_atlas(ctx, &pck, &atlas_size);

    Ok(Atlas {
        texture: backed_atlas.texture,
        rects: backed_atlas.rects,
        packed: extract_packed_textures(ctx, &pck),
    })
}

fn find_packable_texture(ctx: &Context, pck: &Packed) -> Option<usize> {
    let mut optimal_texture_idx: Option<usize> = None;
    let mut min_units_to_pack = u32::MAX;

    for (idx, texture) in ctx.to_pack.iter().enumerate() {
        if pck.textures.contains(&idx) {
            continue;
        }
        let units_to_pack = texture
            .unique
            .iter()
            .filter(|u| !pck.units.contains_key(u))
            .count() as u32;
        if units_to_pack < min_units_to_pack {
            optimal_texture_idx = Some(idx);
            min_units_to_pack = units_to_pack;
        }
    }

    optimal_texture_idx?;
    if (pck.units.len() as u32 + min_units_to_pack) <= ctx.unit_capacity {
        optimal_texture_idx
    } else {
        None
    }
}

fn eval_atlas_size(ctx: &Context, units_count: u32) -> USize {
    let size = units_count.pow(2);

    if ctx.pot {
        let size = (size * ctx.padded_unit_size).next_power_of_two();
        return USize::new(size, size);
    }

    if ctx.square {
        let size = size * ctx.padded_unit_size;
        return USize::new(size, size);
    }

    let mut size = USize::new(size, size);
    for width in size.width..0 {
        let height = units_count.div_ceil(width);
        if height * ctx.padded_unit_size > ctx.size_limit {
            break;
        }
        if width * height < size.width * size.height {
            size = USize::new(width, height);
        }
    }

    USize::new(
        size.width * ctx.padded_unit_size,
        size.height * ctx.padded_unit_size,
    )
}

fn bake_atlas(ctx: &Context, pck: &Packed, size: &USize) -> BakedAtlas {
    let units_per_row = size.width / ctx.padded_unit_size;
    let mut rects = HashMap::new();
    let mut texture = Texture {
        width: size.width,
        height: size.height,
        pixels: vec![Pixel::default(); (size.width * size.height) as usize],
    };

    for (unit_idx, (unit_hash, unit_ref)) in pck.units.iter().enumerate() {
        let row = unit_idx as u32 / units_per_row;
        let column = unit_idx as u32 % units_per_row;
        let unit = &ctx.to_pack[unit_ref.texture_idx].units[unit_ref.unit_idx];
        set_pixels(ctx, &unit.pixels, column, row, &mut texture);

        let rect = get_uv(ctx, column, row, size);
        let rect = inset_uv(ctx, rect);
        let rect = scale_uv(ctx, rect, unit);
        rects.insert(*unit_hash, rect);
    }

    BakedAtlas { texture, rects }
}

fn set_pixels(ctx: &Context, pixels: &[Pixel], column: u32, row: u32, atlas: &mut Texture) {
    let mut from_idx = 0;
    let start_x = column * ctx.padded_unit_size;
    let start_y = row * ctx.padded_unit_size;
    for y in start_y..(start_y + ctx.padded_unit_size) {
        for x in start_x..(start_x + ctx.padded_unit_size) {
            let into_idx = (x + atlas.width * y) as usize;
            atlas.pixels[into_idx] = pixels[from_idx];
            from_idx += 1;
        }
    }
}

fn get_uv(ctx: &Context, column: u32, row: u32, atlas_size: &USize) -> FRect {
    let width = ctx.unit_size as f32 / atlas_size.width as f32;
    let height = ctx.unit_size as f32 / atlas_size.height as f32;
    let x = (column * ctx.padded_unit_size + ctx.pad) as f32 / atlas_size.width as f32;
    let y = (row * ctx.padded_unit_size + ctx.pad) as f32 / atlas_size.height as f32;
    FRect::new(x, y, width, height)
}

fn inset_uv(ctx: &Context, rect: FRect) -> FRect {
    let d = ctx.inset * (rect.width / 2.0);
    let dx2 = d * 2.0;
    FRect::new(rect.x + d, rect.y + d, rect.width - dx2, rect.height - dx2)
}

fn scale_uv(ctx: &Context, rect: FRect, unit: &DicedUnit) -> FRect {
    let mx = unit.rect.width as f32 / ctx.unit_size as f32;
    let my = unit.rect.height as f32 / ctx.unit_size as f32;
    FRect::new(rect.x, rect.y, rect.width * mx, rect.height * my)
}

fn extract_packed_textures(ctx: &mut Context, pck: &Packed) -> Vec<DicedTexture> {
    let mut packed = Vec::new();
    let mut idx = ctx.to_pack.len() - 1;
    loop {
        if pck.textures.contains(&idx) {
            packed.push(ctx.to_pack.swap_remove(idx));
        }
        if idx == 0 {
            break;
        }
        idx -= 1;
    }
    packed
}

#[cfg(test)]
mod tests {
    use crate::dicer::dice;
    use crate::fixtures::*;
    use crate::models::*;
    use crate::packer::pack;

    #[test]
    fn can_pack_with_defaults() {
        pck(vec![&R1X1, &B1X1], &Prefs::default());
    }

    #[test]
    #[should_panic(expected = "UV inset should be in 0.0 to 0.5 range.")]
    fn errs_when_inset_above_05() {
        let prefs = Prefs {
            uv_inset: 0.85,
            ..Prefs::default()
        };
        pck(vec![&RGB4X4], &prefs);
    }

    #[test]
    #[should_panic(expected = "Atlas size limit can't be zero.")]
    fn errs_when_limit_is_zero() {
        let prefs = Prefs {
            atlas_size_limit: 0,
            ..Prefs::default()
        };
        pck(vec![&RGB4X4], &prefs);
    }

    #[test]
    #[should_panic(expected = "Unit size can't be above atlas size limit.")]
    fn errs_when_unit_size_above_limit() {
        let prefs = Prefs {
            unit_size: 2,
            atlas_size_limit: 1,
            ..Prefs::default()
        };
        pck(vec![&RGB4X4], &prefs);
    }

    #[test]
    fn when_empty_input_empty_vec_is_returned() {
        assert_eq!(pck(vec![], &Prefs::default()).len(), 0);
    }

    fn pck(src: Vec<&Texture>, prefs: &Prefs) -> Vec<Atlas> {
        let sprites = src
            .into_iter()
            .map(|t| SourceSprite {
                id: "test".to_string(),
                texture: t.to_owned(),
                pivot: None,
            })
            .collect::<Vec<_>>();
        pack(dice(&sprites, prefs).unwrap(), prefs).unwrap()
    }
}
