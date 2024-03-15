use crate::models::*;

/// Packs diced textures into atlases.
pub(crate) fn pack(diced: &[DicedTexture], prefs: &Prefs) -> Result<Vec<AtlasTexture>> {
    if prefs.uv_inset > 0.5 {
        return Err(Error::Spec("UV inset should be in 0.0 to 0.5 range."));
    }
    if prefs.atlas_size_limit == 0 {
        return Err(Error::Spec("Atlas size limit can't be zero."));
    }
    if prefs.unit_size > prefs.atlas_size_limit {
        return Err(Error::Spec("Unit size can't be above atlas size limit."));
    }
    _ = diced;
    _ = prefs;
    Ok(Vec::new())
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
}

fn new_ctx(prefs: &Prefs) -> Context {
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
    }
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
        pack(&dice(&sprites, prefs)?, prefs)
    }
}
