use crate::models::*;

/// Chops source sprite textures and collects unique units.
pub(crate) struct Dicer {
    dice_size: u16,
    padding: u16,
    trim_transparent: bool,
}

pub(crate) fn new(prefs: &Prefs) -> Result<Dicer, &'static str> {
    if prefs.unit_size < 1 {
        return Err("Unit size can't be zero.");
    }
    Ok(Dicer {
        dice_size: prefs.unit_size,
        padding: prefs.padding,
        trim_transparent: prefs.trim_transparent,
    })
}

impl Dicer {
    pub fn dice<'a>(&self, source: &'a SourceSprite) -> DicedTexture<'a> {
        let units: Vec<DicedUnit> = Vec::new();
        let unique_units: Vec<DicedUnit> = Vec::new();

        DicedTexture {
            source,
            units,
            unique_units,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::fixtures as fx;
    use image::DynamicImage;

    #[test]
    fn errs_when_unit_size_zero() {
        assert!(new(&Prefs {
            unit_size: 0,
            ..Prefs::default()
        })
        .is_err());
    }

    #[test]
    fn keeps_source_ref() {
        assert_eq!(*fx::B, *dice(1, 0, false, &src(&fx::B)).source.texture);
    }

    #[test]
    fn foo() {
        dice(1, 0, false, &src(&fx::B));
    }

    fn dice<'a>(size: u16, pad: u16, trim: bool, src: &'a SourceSprite) -> DicedTexture<'a> {
        let prefs = Prefs {
            unit_size: size,
            padding: pad,
            trim_transparent: trim,
            ..Prefs::default()
        };
        new(&prefs).unwrap().dice(src)
    }

    fn src(tex: &DynamicImage) -> SourceSprite {
        SourceSprite {
            id: "test".to_string(),
            texture: tex,
            pivot: None,
        }
    }
}
