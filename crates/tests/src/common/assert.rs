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
    // atlases[0].save(format!("D:/{fixture}.png",)).unwrap();
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
    for (idx, _) in diced.vertices.iter().enumerate().step_by(4) {
        // Vertices layout by index:
        // min -> [0] [3]
        //        [1] [2] <- max
        let min_vertex = &diced.vertices[idx];
        let max_vertex = &diced.vertices[idx + 2];
        let quad_offset_x = prefs.pivot.x * sprite_width as f32;
        let quad_offset_y = prefs.pivot.y * sprite_height as f32;
        let quad_min_x = (min_vertex.x * prefs.ppu + quad_offset_x) as u32;
        let quad_min_y = (min_vertex.y * prefs.ppu + quad_offset_y) as u32;
        let quad_max_x = (max_vertex.x * prefs.ppu + quad_offset_x) as u32;
        let quad_max_y = (max_vertex.y * prefs.ppu + quad_offset_y) as u32;

        // Offsetting UVs to the center of each pixel to get the integer index on round.
        let min_uv = &diced.uvs[idx];
        let max_uv = &diced.uvs[idx + 2];
        let uv_offset_x = (1.0 / atlas.width() as f32) / 2.0;
        let uv_offset_y = (1.0 / atlas.height() as f32) / 2.0;
        let uv_min_x = min_uv.u + uv_offset_x;
        let uv_min_y = min_uv.v + uv_offset_y;
        let uv_max_x = max_uv.u - uv_offset_x;
        let uv_max_y = max_uv.v - uv_offset_y;

        let last_ix = quad_max_x - quad_min_x - 1;
        let last_iy = quad_max_y - quad_min_y - 1;

        for (ix, x) in (quad_min_x..quad_max_x).enumerate() {
            for (iy, y) in (quad_min_y..quad_max_y).enumerate() {
                let atlas_u = lerp(uv_min_x, uv_max_x, ix as f32 / last_ix as f32);
                let atlas_v = lerp(uv_min_y, uv_max_y, iy as f32 / last_iy as f32);
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
    let mut w = w;
    if w.is_nan() {
        w = 0.0;
    };
    a + w * (b - a)
}
