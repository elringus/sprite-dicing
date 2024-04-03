use crate::common::*;
use image::{ImageBuffer, RgbaImage};
use sprite_dicing::{Artifacts, DicedSprite, Prefs};

/// Asserts source sprites under specified fixture can be reproduced using specified artifacts.
pub fn assert_repro(fixture: &str, arts: Artifacts, prefs: &Prefs) {
    assert_eq!(
        arts.sprites.len(), // All-transparent (clear) sources are ignored.
        RAW[fixture].iter().filter(|(_, i)| !is_clear(i)).count()
    );
    let atlases: Vec<_> = arts.atlases.iter().map(from_texture).collect();
    for source in SRC[fixture].iter() {
        let source_raw = &RAW[fixture][&source.id];
        if is_clear(source_raw) {
            continue;
        }
        let diced = arts.sprites.iter().find(|&d| d.id == source.id).unwrap();
        let atlas = &atlases[diced.atlas_index];
        let reproduced = &reproduce(diced, atlas, prefs);
        assert_eq!(source_raw, reproduced);
    }
}

fn reproduce(diced: &DicedSprite, atlas: &RgbaImage, prefs: &Prefs) -> RgbaImage {
    let sprite_width = (diced.rect.width * prefs.ppu) as u32;
    let sprite_height = (diced.rect.height * prefs.ppu) as u32;
    let mut img = ImageBuffer::new(sprite_width, sprite_height);
    for (idx, vertex) in diced.vertices.iter().step_by(4).enumerate() {
        // Vertices layout by index:
        // min -> [0] [3]
        //        [1] [2] <- max

        let quad_offset_x = prefs.pivot.x * sprite_width as f32;
        let quad_offset_y = prefs.pivot.y * sprite_height as f32;
        let quad_min_x = (vertex.x * prefs.ppu + quad_offset_x) as u32;
        let quad_min_y = (vertex.y * prefs.ppu + quad_offset_y) as u32;
        let quad_max_x = (diced.vertices[idx + 2].x * prefs.ppu + quad_offset_x) as u32;
        let quad_max_y = (diced.vertices[idx + 2].y * prefs.ppu + quad_offset_y) as u32;

        // Offsetting UVs to the center of each pixel to get the integer index on round.
        let uv_offset_x = (prefs.unit_size as f32 / atlas.width() as f32) / 2.0;
        let uv_offset_y = (prefs.unit_size as f32 / atlas.height() as f32) / 2.0;
        let uv_min_x = diced.uvs[idx].u + uv_offset_x;
        let uv_min_y = diced.uvs[idx].v + uv_offset_y;
        let uv_max_x = diced.uvs[idx + 2].u - uv_offset_x;
        let uv_max_y = diced.uvs[idx + 2].v - uv_offset_y;

        for x in quad_min_x..=quad_max_x {
            for y in quad_min_y..=quad_max_y {
                let atlas_u = lerp(uv_min_x, uv_max_x, x as f32 / quad_max_x as f32);
                let atlas_v = lerp(uv_min_y, uv_max_y, y as f32 / quad_max_y as f32);
                let atlas_x = (atlas_u * (atlas.width() - 1) as f32).round() as u32;
                let atlas_y = (atlas_v * (atlas.height() - 1) as f32).round() as u32;
                let src_pixel = atlas.get_pixel(atlas_x, atlas_y);
                img.put_pixel(x, y, *src_pixel);
            }
        }
    }
    img
}

fn lerp(a: f32, b: f32, w: f32) -> f32 {
    a + w * (b - a)
}
