use crate::models::*;
use std::collections::{HashMap, HashSet};

/// Packs diced textures into atlases.
pub(crate) fn pack(diced: Vec<DicedTexture>, prefs: &Prefs) -> Result<Vec<AtlasTexture>> {
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

fn pack_next(ctx: &mut Context) -> Result<AtlasTexture> {
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

    let (atlas_width, atlas_height) = eval_atlas_size(ctx, pck.units.len() as u32);
    let mut texture = Texture::new(atlas_width, atlas_height);
    let uv_by_hash = bake_units(ctx, &pck, &mut texture);
    let packed = extract_packed_textures(ctx, &pck);

    Ok(AtlasTexture {
        texture,
        packed,
        uv_by_hash,
    })
}

fn find_packable_texture(ctx: &Context, pck: &Packed) -> Option<usize> {
    Some(0)
}

fn eval_atlas_size(ctx: &Context, units_count: u32) -> (u32, u32) {
    let size = units_count.pow(2);

    if ctx.pot {
        let size = (size * ctx.padded_unit_size).next_power_of_two();
        return (size, size);
    }

    if ctx.square {
        let size = size * ctx.padded_unit_size;
        return (size, size);
    }

    let mut size = (size, size);
    for width in size.0..0 {
        let height = units_count.div_ceil(width);
        if height * ctx.padded_unit_size > ctx.size_limit {
            break;
        }
        if width * height < size.0 * size.1 {
            size = (width, height);
        }
    }

    (size.0 * ctx.padded_unit_size, size.1 * ctx.padded_unit_size)
}

fn bake_units(ctx: &Context, pck: &Packed, atlas: &mut Texture) -> HashMap<u64, TextureCoordinate> {
    HashMap::new()
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
        assert!(pck(vec![&R1X1, &B1X1], &Prefs::default()).is_ok());
    }

    #[test]
    fn errs_when_inset_above_05() {
        let prefs = Prefs {
            uv_inset: 0.85,
            ..Prefs::default()
        };
        assert!(pck(vec![&RGB4X4], &prefs).is_err());
    }

    #[test]
    fn errs_when_limit_is_zero() {
        let prefs = Prefs {
            atlas_size_limit: 0,
            ..Prefs::default()
        };
        assert!(pck(vec![&RGB4X4], &prefs).is_err());
    }

    #[test]
    fn errs_when_unit_size_above_limit() {
        let prefs = Prefs {
            unit_size: 2,
            atlas_size_limit: 1,
            ..Prefs::default()
        };
        assert!(pck(vec![&RGB4X4], &prefs).is_err());
    }

    fn pck(src: Vec<&Texture>, prefs: &Prefs) -> Result<Vec<AtlasTexture>> {
        let sprites = src
            .into_iter()
            .map(|t| SourceSprite {
                id: "test".to_string(),
                texture: t.to_owned(),
                pivot: None,
            })
            .collect::<Vec<_>>();
        pack(dice(&sprites, prefs)?, prefs)
    }
}
