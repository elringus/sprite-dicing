use crate::models::*;

/// Chops source sprite textures and collects unique units.
pub(crate) struct Dicer {
    dice_size: u16,
    padding: u16,
    trim_transparent: bool,
}

pub(crate) fn new(prefs: &Prefs) -> Dicer {
    Dicer {
        dice_size: prefs.dice_size,
        padding: prefs.padding,
        trim_transparent: prefs.trim_transparent,
    }
}

impl Dicer {
    pub fn dice<'a>(&self, source: &'a SourceSprite) -> DicedTexture<'a> {
        DicedTexture {
            source,
            units: Vec::new(),
            unique_units: Vec::new(),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::fixtures as fx;
    use image::Rgba;

    #[test]
    fn keeps_source_ref() {
        let source = SourceSprite {
            id: "foo".to_string(),
            texture: Default::default(),
            pivot: None,
        };
        assert_eq!("foo", new(&Prefs::default()).dice(&source).source.id);
    }

    #[test]
    fn foo() {
        assert_eq!(Rgba([0, 0, 255, 255]), fx::B[(0, 0)]);
    }
}
