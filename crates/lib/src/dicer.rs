use crate::models::*;
use image::DynamicImage;

struct Context<'a> {
    size: u16,
    pad: u16,
    trim: bool,
    tex: &'a DynamicImage,
    units: Vec<DicedUnit<'a>>,
    diced: Vec<DicedTexture<'a>>,
}

/// Chops source sprite textures and collects unique units.
pub(crate) fn dice<'a>(
    sources: &Vec<SourceSprite>,
    prefs: &Prefs,
) -> Result<Vec<DicedTexture<'a>>, &'static str> {
    let mut ctx = create_context(prefs)?;
    _ = sources;
    Ok(ctx.diced)
}

fn create_context<'a>(prefs: &Prefs) -> Result<Context<'a>, &'static str> {
    if prefs.unit_size < 1 {
        return Err("Unit size can't be zero.");
    }
    Ok(Context {
        size: prefs.unit_size,
        pad: prefs.padding,
        trim: prefs.trim_transparent,
        tex: DynamicImage::default(),
        units: Vec::new(),
        diced: Vec::new(),
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::fixtures as fx;
    use image::DynamicImage;

    type Result = std::result::Result<Box<dyn std::any::Any>, Box<dyn std::any::Any>>;

    #[test]
    fn errs_when_unit_size_zero() {
        assert!(dice(&src(&fx::B), &pref(0, 0, false)).is_err());
    }

    #[test]
    fn keeps_source_ref() -> Result {
        assert_eq!(*fx::B, *dice(&src(&fx::B), &Prefs::default())?[0].source.texture)
    }

    #[test]
    fn foo() -> Result {
        dice(&src(&fx::B), &Prefs::default())
    }

    fn pref(size: u16, pad: u16, trim: bool) -> Prefs {
        Prefs {
            unit_size: size,
            padding: pad,
            trim_transparent: trim,
            ..Prefs::default()
        }
    }

    fn src(tex: &DynamicImage) -> Vec<SourceSprite> {
        vec![SourceSprite {
            id: "test".to_string(),
            texture: tex,
            pivot: None,
        }]
    }
}
