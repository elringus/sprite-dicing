// use image::{ImageBuffer, RgbaImage};
// use sprite_dicing::{DicedSprite, Prefs, RawArtifacts, RawSprite};
//
// /// Whether specified source sprites can be reproduced using specified diced artifacts.
// pub fn reproducible(source: &[RawSprite], diced: RawArtifacts, prefs: &Prefs) -> bool {
//     _ = source;
//     _ = diced;
//     _ = prefs;
//     true
// }
//
// fn reproduce(diced: &DicedSprite, atlases: &[RgbaImage], ppu: f32) -> RgbaImage {
//     let atlas = &atlases[diced.atlas_index];
//     let width = (diced.rect.width * ppu) as u32;
//     let height = (diced.rect.height * ppu) as u32;
//     let mut img = ImageBuffer::new(width, height);
//     img
// }
