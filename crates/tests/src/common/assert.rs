use crate::common::*;
use image::{ImageBuffer, RgbaImage};
use sprite_dicing::{Artifacts, DicedSprite, Prefs, SourceSprite};
use std::collections::HashMap;

/// Asserts source sprites under specified fixture can be reproduced using specified artifacts,
/// both pixel-aligned and, when padding is set, under bilinear filtering.
pub fn assert_repro(fixture: &str, arts: Artifacts, prefs: &Prefs) {
    assert_eq!(
        arts.sprites.len(), // All-transparent (clear) sources are ignored.
        RAW[fixture].iter().filter(|(_, i)| !is_clear(i)).count()
    );
    let atlases: Vec<_> = arts.atlases.iter().map(from_texture).collect();
    let excluded = eval_excluded(&SRC[fixture], prefs);
    for (idx, source) in SRC[fixture].iter().enumerate() {
        let source_raw = &RAW[fixture][&source.id];
        if is_clear(source_raw) {
            continue;
        }
        let diced = arts.sprites.iter().find(|&d| d.id == source.id).unwrap();
        let atlas = &atlases[diced.atlas_index];
        let mesh = mesh(diced, atlas, prefs);
        assert_eq!(source_raw, &reproduce(&mesh, atlas, source_raw));
        if prefs.padding > 0 {
            assert_filtered(source_raw, &mesh, atlas, &excluded[idx]);
        }
    }
}

/// Mesh vertex in pixels: position on the source texture and on the atlas texture.
#[derive(Clone, Copy)]
struct Vertex {
    x: f32,
    y: f32,
    u: f32,
    v: f32,
}

fn mesh(diced: &DicedSprite, atlas: &RgbaImage, prefs: &Prefs) -> Vec<[Vertex; 3]> {
    // Vertices are offset by the pivot and, when trimmed, by the sprite rect position.
    let mut offset_x = diced.pivot.x * diced.rect.width;
    let mut offset_y = diced.pivot.y * diced.rect.height;
    if prefs.trim_transparent {
        offset_x += diced.rect.x;
        offset_y += diced.rect.y;
    }
    let vertex = |i: &usize| Vertex {
        x: (diced.vertices[*i].x + offset_x) * prefs.ppu,
        y: (diced.vertices[*i].y + offset_y) * prefs.ppu,
        u: diced.uvs[*i].u * atlas.width() as f32,
        v: diced.uvs[*i].v * atlas.height() as f32,
    };
    let triangle = |t: &[usize]| [vertex(&t[0]), vertex(&t[1]), vertex(&t[2])];
    diced.indices.chunks(3).map(triangle).collect()
}

/// Rasterizes the mesh over the grid of positions with specified step and offset: maps
/// each position inside a triangle to the atlas by interpolating the UVs and draws it.
fn rasterize(
    mesh: &[[Vertex; 3]],
    step: f32,
    offset: f32,
    mut draw: impl FnMut(f32, f32, f32, f32),
) {
    let grid = |min: f32, max: f32| {
        let (first, last) = (
            ((min - offset) / step).ceil(),
            ((max - offset) / step).floor(),
        );
        (first as i64..=last as i64).map(move |i| i as f32 * step + offset)
    };
    let edge =
        |p: &Vertex, q: &Vertex, x: f32, y: f32| (q.x - p.x) * (y - p.y) - (q.y - p.y) * (x - p.x);
    for [a, b, c] in mesh {
        for y in grid(a.y.min(b.y).min(c.y), a.y.max(b.y).max(c.y)) {
            for x in grid(a.x.min(b.x).min(c.x), a.x.max(b.x).max(c.x)) {
                let (wa, wb, wc) = (edge(b, c, x, y), edge(c, a, x, y), edge(a, b, x, y));
                let area = wa + wb + wc;
                // Positions on a shared edge belong to both triangles despite rounding.
                let eps = area.abs() * 1e-3;
                let inside = (wa >= -eps && wb >= -eps && wc >= -eps)
                    || (wa <= eps && wb <= eps && wc <= eps);
                if inside && area != 0.0 {
                    let u = (wa * a.u + wb * b.u + wc * c.u) / area;
                    let v = (wa * a.v + wb * b.v + wc * c.v) / area;
                    draw(x, y, u, v);
                }
            }
        }
    }
}

fn reproduce(mesh: &[[Vertex; 3]], atlas: &RgbaImage, source: &RgbaImage) -> RgbaImage {
    let mut img = ImageBuffer::new(source.width(), source.height());
    rasterize(mesh, 1.0, 0.5, |x, y, u, v| {
        img.put_pixel(x as u32, y as u32, *atlas.get_pixel(u as u32, v as u32));
    });
    img
}

/// Asserts the mesh renders with bilinear filtering exactly as the source at quarter-pixel
/// positions, except at the excluded pixels. Interpolated atlas positions are snapped to the
/// quarter grid, which is exact in f32, so the blend weights stay bit-equal to the source's.
fn assert_filtered(source: &RgbaImage, mesh: &[[Vertex; 3]], atlas: &RgbaImage, excluded: &[bool]) {
    let (width, height) = (source.width() * 2, source.height() * 2);
    let mut samples = vec![None; (width * height) as usize];
    rasterize(mesh, 0.5, 0.25, |x, y, u, v| {
        let snap = |p: f32| {
            let snapped = (p * 4.0).round() / 4.0;
            assert!(
                (p - snapped).abs() < 0.01,
                "Atlas position {p} is off the pixel grid."
            );
            snapped
        };
        let idx = (x * 2.0) as u32 + (y * 2.0) as u32 * width;
        samples[idx as usize] = Some(bilinear(atlas, snap(u), snap(v)));
    });
    for j in 0..height {
        for i in 0..width {
            let (x, y) = (i as f32 / 2.0 + 0.25, j as f32 / 2.0 + 0.25);
            if excluded[(x as u32 + y as u32 * source.width()) as usize] {
                continue;
            }
            let actual = samples[(i + j * width) as usize].unwrap_or([0.0; 4]);
            assert_eq!(bilinear(source, x, y), actual, "Differs at ({x}, {y}).");
        }
    }
}

fn bilinear(img: &RgbaImage, x: f32, y: f32) -> [f32; 4] {
    let (fx, fy) = (x - 0.5, y - 0.5);
    let (x0, y0) = (fx.floor(), fy.floor());
    let (tx, ty) = (fx - x0, fy - y0);
    let get = |dx: f32, dy: f32| {
        let x = ((x0 + dx) as i64).clamp(0, img.width() as i64 - 1) as u32;
        let y = ((y0 + dy) as i64).clamp(0, img.height() as i64 - 1) as u32;
        img.get_pixel(x, y).0
    };
    let (a, b, c, d) = (get(0.0, 0.0), get(1.0, 0.0), get(0.0, 1.0), get(1.0, 1.0));
    let mut out = [0.0; 4];
    for (ch, value) in out.iter_mut().enumerate() {
        let top = a[ch] as f32 * (1.0 - tx) + b[ch] as f32 * tx;
        let bottom = c[ch] as f32 * (1.0 - tx) + d[ch] as f32 * tx;
        *value = top * (1.0 - ty) + bottom * ty;
    }
    out
}

/// Pixels of each source excluded from the filtered check: the one-pixel border inside grid
/// cells without content, where the fringe of the neighbors is not drawn, and inside the
/// cells of units occurring with different padding, where the median padding is blended in.
fn eval_excluded(sources: &[SourceSprite], prefs: &Prefs) -> Vec<Vec<bool>> {
    let (unit, pad) = (prefs.unit_size as i64, prefs.padding as i64);
    let mut masks = vec![];
    // Cells with equal pixels, keyed by the pixels: (source index, x, y, w, h, padded pixels).
    type Cell = (usize, i64, i64, i64, i64, Vec<u8>);
    let mut occurrences: HashMap<Vec<u8>, Vec<Cell>> = HashMap::new();
    for (idx, source) in sources.iter().enumerate() {
        let tex = &source.texture;
        let (width, height) = (tex.width as i64, tex.height as i64);
        let pixel = |x: i64, y: i64| {
            let (x, y) = (x.clamp(0, width - 1), y.clamp(0, height - 1));
            tex.pixels[(x + y * width) as usize].to_raw()
        };
        let mut mask = vec![false; (width * height) as usize];
        for cy in (0..height).step_by(unit as usize) {
            for cx in (0..width).step_by(unit as usize) {
                let (w, h) = ((width - cx).min(unit), (height - cy).min(unit));
                let mut cell = vec![];
                let mut padded = vec![];
                for y in (cy - pad)..(cy + h + pad) {
                    for x in (cx - pad)..(cx + w + pad) {
                        padded.extend(pixel(x, y));
                        if (cx..cx + w).contains(&x) && (cy..cy + h).contains(&y) {
                            cell.extend(pixel(x, y));
                        }
                    }
                }
                if cell.chunks(4).all(|p| p[3] == 0) {
                    mark_border(&mut mask, width, cx, cy, w, h);
                } else {
                    let mut key = vec![w as u8, h as u8];
                    key.extend(cell);
                    let occurrence = (idx, cx, cy, w, h, padded);
                    occurrences.entry(key).or_default().push(occurrence);
                }
            }
        }
        masks.push(mask);
    }
    for units in occurrences.values() {
        if units.iter().any(|u| u.5 != units[0].5) {
            for (idx, cx, cy, w, h, _) in units {
                let width = sources[*idx].texture.width as i64;
                mark_border(&mut masks[*idx], width, *cx, *cy, *w, *h);
            }
        }
    }
    masks
}

fn mark_border(mask: &mut [bool], width: i64, cx: i64, cy: i64, w: i64, h: i64) {
    for y in cy..cy + h {
        for x in cx..cx + w {
            if x == cx || x == cx + w - 1 || y == cy || y == cy + h - 1 {
                mask[(x + y * width) as usize] = true;
            }
        }
    }
}
