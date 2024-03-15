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
        atlases.push(pack_it(&mut ctx)?);
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

fn pack_it(ctx: &mut Context) -> Result<AtlasTexture> {
    let mut packed_textures = Vec::new();
    let mut packed_units = HashSet::new();

    while let Some(id) = find_packable(ctx, &packed_units) {
        let idx = ctx.to_pack.iter().position(|t| t.id == id).unwrap();
        packed_units.extend(ctx.to_pack[idx].units.iter().map(|u| u.hash));
        packed_textures.extend(ctx.to_pack.drain(idx..=idx));
    }

    if packed_textures.is_empty() {
        return Err(Error::Spec("Can't fit any texture; increase atlas size."));
    }

    let (atlas_width, atlas_height) = eval_atlas_size(packed_units.len() as u32, ctx);
    let mut texture = Texture::new(atlas_width, atlas_height);
    let uv_by_hash = bake_units(&mut texture, &packed_textures, packed_units, ctx);

    Ok(AtlasTexture {
        texture,
        packed: packed_textures,
        uv_by_hash,
    })
}

fn find_packable<'a>(ctx: &Context, packed_units: &'a HashSet<u64>) -> Option<&'a str> {
    None
}

fn eval_atlas_size(units_count: u32, ctx: &Context) -> (u32, u32) {
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

fn bake_units(
    atlas: &mut Texture,
    packed_textures: &Vec<DicedTexture>,
    packed_units: HashSet<u64>,
    ctx: &Context,
) -> HashMap<u64, TextureCoordinate> {
    HashMap::new()
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
