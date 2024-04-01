use image::{ImageBuffer, RgbaImage};
use sprite_dicing::{DicedSprite, Prefs, RawArtifacts, RawSprite};

/// Asserts specified source sprites can be reproduced using specified diced artifacts.
pub fn assert_repro(sources: &[RawSprite], arts: RawArtifacts, prefs: &Prefs) {
    for source in sources.iter() {
        let diced = arts.sprites.iter().find(|&d| d.id == source.id).unwrap();
        let atlas = bytes_to_img(&arts.atlases[diced.atlas_index]);
        let reproduced = &reproduce(diced, &atlas, prefs.ppu);
        let source_img = &bytes_to_img(source.bytes);
        assert_eq!(source_img, reproduced);
    }
}

fn reproduce(diced: &DicedSprite, atlas: &RgbaImage, ppu: f32) -> RgbaImage {
    let width = (diced.rect.width * ppu) as u32;
    let height = (diced.rect.height * ppu) as u32;
    let mut img = ImageBuffer::new(width, height);
    for (idx, vertex) in diced.vertices.iter().enumerate() {
        let uv = &diced.uvs[idx];
        let src_pixel = atlas.get_pixel((uv.u * ppu) as u32, (uv.v * ppu) as u32);
        img.put_pixel((vertex.x * ppu) as u32, (vertex.y * ppu) as u32, *src_pixel);
    }
    img
}

fn bytes_to_img(bytes: &[u8]) -> RgbaImage {
    image::load_from_memory(bytes)
        .unwrap()
        .as_rgba8()
        .unwrap()
        .to_owned()
}
