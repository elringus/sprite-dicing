use crate::models::*;

/// Builds data required to reconstruct diced sprites at runtime: mesh, uvs, etc.
pub(crate) fn build(packed: &Vec<Atlas>, prefs: &Prefs) -> Result<Vec<DicedSprite>> {
    if prefs.ppu == 0 {
        return Err(Error::Spec("PPU can't be zero."));
    }

    _ = packed;
    _ = prefs;
    Ok(vec![])
}

struct Context {
    ppu: u32,
    default_pivot: Pivot,
}

fn new_ctx(prefs: &Prefs) -> Context {
    Context {
        ppu: prefs.ppu,
        default_pivot: prefs.pivot.to_owned(),
    }
}

fn pack_it () {
    
}

#[cfg(test)]
mod tests {
    use crate::fixtures::*;
    use crate::models::*;

    #[test]
    fn can_build_with_defaults() {
        build(vec![&R1X1, &B1X1], &Prefs::default());
    }

    #[test]
    #[should_panic(expected = "PPU can't be zero.")]
    fn errs_when_ppu_zero() {
        let prefs = Prefs {
            ppu: 0,
            ..defaults()
        };
        build(vec![&R1X1, &B1X1], &prefs);
    }

    fn build(src: Vec<&Texture>, prefs: &Prefs) -> Vec<DicedSprite> {
        let sprites = src
            .into_iter()
            .map(|t| SourceSprite {
                id: "test".to_string(),
                texture: t.to_owned(),
                pivot: None,
            })
            .collect::<Vec<_>>();
        let diced = crate::dicer::dice(&sprites, prefs).unwrap();
        let packed = crate::packer::pack(diced, prefs).unwrap();
        crate::builder::build(&packed, prefs).unwrap()
    }

    fn defaults() -> Prefs {
        Prefs {
            ppu: 1,
            unit_size: 1,
            padding: 0,
            trim_transparent: false,
            ..Prefs::default()
        }
    }
}
