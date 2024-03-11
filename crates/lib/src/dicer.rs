use crate::models::*;
use image::DynamicImage;
use std::sync::OnceLock;

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
    sources: &'a [SourceSprite],
    prefs: &Prefs,
) -> Result<Vec<DicedTexture<'a>>, &'static str> {
    let mut ctx = create_context(prefs)?;
    ctx.diced.push(DicedTexture {
        source: &sources[0],
        units: vec![],
        unique_units: vec![],
    });
    Ok(ctx.diced)
}

fn create_context<'a>(prefs: &Prefs) -> Result<Context<'a>, &'static str> {
    if prefs.unit_size < 1 {
        return Err("Unit size can't be zero.");
    }
    static DEFAULT_TEX: OnceLock<DynamicImage> = OnceLock::new();
    Ok(Context {
        size: prefs.unit_size,
        pad: prefs.padding,
        trim: prefs.trim_transparent,
        tex: DEFAULT_TEX.get_or_init(DynamicImage::default),
        units: Vec::new(),
        diced: Vec::new(),
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::fixtures as fx;
    use image::DynamicImage;

    #[test]
    fn errs_when_unit_size_zero() {
        assert!(dice(&src(&fx::B), &pref(0, 0, false)).is_err());
    }

    #[test]
    fn keeps_source_ref() {
        let src = src(&fx::B);
        let diced = dice(&src, &Prefs::default()).unwrap();
        assert_eq!(*fx::B, *diced[0].source.texture);
    }

    #[test]
    fn foo() {
        dice(&src(&fx::B), &Prefs::default()).unwrap();
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
