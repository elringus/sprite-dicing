use crate::models::*;
use std::collections::HashMap;

/// Builds data required to reconstruct diced sprites at runtime: mesh, uvs, etc.
pub(crate) fn build(packed: &[Atlas], prefs: &Prefs) -> Result<Vec<DicedSprite>> {
    if prefs.ppu <= 0.0 {
        return Err(Error::Spec("PPU can't be zero or negative."));
    }

    let total = packed.iter().map(|a| a.packed.len()).sum();
    let mut sprites = Vec::with_capacity(total);

    for (atlas_idx, atlas) in packed.iter().enumerate() {
        for diced_tex in atlas.packed.iter() {
            Progress::report(prefs, 4, sprites.len(), total, "Building diced sprites");
            let ctx = new_ctx(atlas, atlas_idx, diced_tex, prefs);
            sprites.push(build_it(ctx));
        }
    }

    Ok(sprites)
}

struct Context<'a> {
    /// Pixels per conventional unit of the mesh.
    ppu: f32,
    /// Whether to trim transparent areas around the sprite.
    trim: bool,
    /// Pivot to use when the texture doesn't have its own.
    default_pivot: &'a Pivot,
    /// Index of the atlas the texture is packed into.
    atlas_idx: usize,
    /// The texture to build the sprite from.
    diced: &'a DicedTexture,
    /// Units of the atlas, mapped by unit ID.
    units: &'a HashMap<usize, PackedUnit>,
    /// Mesh vertices built so far, in conventional units.
    vertices: Vec<Vertex>,
    /// Atlas texture coordinates of the vertices built so far.
    uvs: Vec<Uv>,
    /// Mesh triangles built so far, as indices of the vertices.
    indices: Vec<usize>,
}

fn new_ctx<'a>(
    atlas: &'a Atlas,
    atlas_idx: usize,
    diced: &'a DicedTexture,
    prefs: &'a Prefs,
) -> Context<'a> {
    Context {
        ppu: prefs.ppu,
        trim: prefs.trim_transparent,
        default_pivot: &prefs.pivot,
        atlas_idx,
        diced,
        units: &atlas.units,
        vertices: vec![],
        uvs: vec![],
        indices: vec![],
    }
}

fn build_it(mut ctx: Context) -> DicedSprite {
    for unit in ctx.diced.units.iter() {
        let packed = &ctx.units[&unit.id];
        let src_rect = match unit.is_solid() {
            true => unit.src_rect, // the texel is drawn over the whole source rect
            false => URect {
                x: unit.src_rect.x + packed.pack_rect.x,
                y: unit.src_rect.y + packed.pack_rect.y,
                width: packed.pack_rect.width,
                height: packed.pack_rect.height,
            },
        };
        build_unit(&mut ctx, &src_rect, &packed.uv);
    }

    let pivot = ctx.diced.pivot.as_ref().unwrap_or(ctx.default_pivot);
    let rect = eval_rect(&ctx, pivot);
    offset_vertices(&mut ctx, &rect, pivot);

    DicedSprite {
        id: ctx.diced.id.to_owned(),
        atlas_index: ctx.atlas_idx,
        vertices: ctx.vertices,
        uvs: ctx.uvs,
        indices: ctx.indices,
        rect,
        pivot: pivot.to_owned(),
    }
}

/// Builds a quad drawing the unit occurrence at specified source rect.
fn build_unit(ctx: &mut Context, src_rect: &URect, uv_rect: &FRect) {
    let src_rect = scale_rect(ctx, src_rect);
    build_quad(ctx, &src_rect, uv_rect);
}

/// Converts the rect from pixels to conventional units.
fn scale_rect(ctx: &Context, rect: &URect) -> FRect {
    FRect {
        x: rect.x as f32 / ctx.ppu,
        y: rect.y as f32 / ctx.ppu,
        width: rect.width as f32 / ctx.ppu,
        height: rect.height as f32 / ctx.ppu,
    }
}

/// Builds a quad with specified position and UV rects.
fn build_quad(ctx: &mut Context, rect: &FRect, uv_rect: &FRect) {
    let i = ctx.vertices.len();

    let x_min = rect.x;
    let y_min = rect.y;
    let x_max = rect.x + rect.width;
    let y_max = rect.y + rect.height;

    let u_min = uv_rect.x;
    let v_min = uv_rect.y;
    let u_max = uv_rect.x + uv_rect.width;
    let v_max = uv_rect.y + uv_rect.height;

    ctx.vertices.extend([
        Vertex { x: x_min, y: y_min },
        Vertex { x: x_min, y: y_max },
        Vertex { x: x_max, y: y_max },
        Vertex { x: x_max, y: y_min },
    ]);

    ctx.uvs.extend([
        Uv { u: u_min, v: v_min },
        Uv { u: u_min, v: v_max },
        Uv { u: u_max, v: v_max },
        Uv { u: u_max, v: v_min },
    ]);

    ctx.indices.extend([i, i + 1, i + 2, i + 2, i + 3, i]);
}

/// Evaluates rect of the sprite.
fn eval_rect(ctx: &Context, pivot: &Pivot) -> Rect {
    if ctx.trim {
        eval_fit_rect(ctx)
    } else {
        eval_full_rect(ctx, pivot)
    }
}

/// Evaluates bounds of the units.
fn eval_fit_rect(ctx: &Context) -> Rect {
    let mut min_x = u32::MAX;
    let mut min_y = u32::MAX;
    let mut max_x = 0;
    let mut max_y = 0;

    for unit in ctx.diced.units.iter() {
        min_x = min_x.min(unit.src_rect.x);
        min_y = min_y.min(unit.src_rect.y);
        max_x = max_x.max(unit.src_rect.x + unit.src_rect.width);
        max_y = max_y.max(unit.src_rect.y + unit.src_rect.height);
    }

    let x = min_x as f32 / ctx.ppu;
    let y = min_y as f32 / ctx.ppu;
    let width = (max_x - min_x) as f32 / ctx.ppu;
    let height = (max_y - min_y) as f32 / ctx.ppu;
    Rect::new(x, y, width, height)
}

/// Evaluates rect of the whole texture with the pivot at the origin.
fn eval_full_rect(ctx: &Context, pivot: &Pivot) -> Rect {
    let width = ctx.diced.size.width as f32 / ctx.ppu;
    let height = ctx.diced.size.height as f32 / ctx.ppu;
    let x = -pivot.x * width;
    let y = -pivot.y * height;
    Rect::new(x, y, width, height)
}

/// Moves the vertices so that the pivot point of the sprite rect is at the origin.
fn offset_vertices(ctx: &mut Context, rect: &Rect, pivot: &Pivot) {
    let mut offset_x = pivot.x * rect.width;
    let mut offset_y = pivot.y * rect.height;

    if ctx.trim {
        offset_x += rect.x;
        offset_y += rect.y;
    }

    for idx in 0..ctx.vertices.len() {
        ctx.vertices[idx].x -= offset_x;
        ctx.vertices[idx].y -= offset_y;
    }
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
    #[should_panic(expected = "PPU can't be zero or negative.")]
    fn errs_when_ppu_zero() {
        let prefs = Prefs {
            ppu: 0.0,
            ..defaults()
        };
        build(vec![&G1X1, &B1X1], &prefs);
    }

    #[test]
    fn sprite_vertices_form_quad() {
        let quad = Quad::from_1x1(&build(vec![&B1X1], &defaults())[0]);
        assert_eq!(quad.top_left, Vertex::new(0.0, 0.0));
        assert_eq!(quad.bottom_left, Vertex::new(0.0, 1.0));
        assert_eq!(quad.bottom_right, Vertex::new(1.0, 1.0));
        assert_eq!(quad.top_right, Vertex::new(1.0, 0.0));
    }

    #[test]
    fn vertices_are_scaled_by_ppu() {
        let prefs = Prefs {
            ppu: 1.0,
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&B1X1], &prefs)[0]);
        assert_eq!(quad.bottom_right, Vertex::new(1.0, 1.0));

        let prefs = Prefs {
            ppu: 2.0,
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&B1X1], &prefs)[0]);
        assert_eq!(quad.bottom_right, Vertex::new(0.5, 0.5));
    }

    #[test]
    fn zero_pivot_doesnt_offset_vertices() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.0, y: 0.0 },
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&B1X1], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(0.0, 0.0));
        assert_eq!(quad.bottom_right, Vertex::new(1.0, 1.0));
    }

    #[test]
    fn non_zero_pivot_offsets_vertices() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.5, y: 0.5 },
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&B1X1], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(-0.5, -0.5));
        assert_eq!(quad.bottom_right, Vertex::new(0.5, 0.5));
    }

    #[test]
    fn when_trim_enabled_vertices_are_not_offset_over_transparent_areas() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.0, y: 0.0 },
            trim_transparent: true,
            ..defaults()
        };

        let quad = Quad::from_1x1(&build(vec![&TTTM], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(0.0, 0.0));
        assert_eq!(quad.bottom_right, Vertex::new(1.0, 1.0));

        let quad = Quad::from_1x1(&build(vec![&MTTT], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(0.0, 0.0));
        assert_eq!(quad.bottom_right, Vertex::new(1.0, 1.0));

        let quad = Quad::from_1x1(&build(vec![&TTMT], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(0.0, 0.0));
        assert_eq!(quad.bottom_right, Vertex::new(1.0, 1.0));
    }

    #[test]
    fn when_trim_disabled_vertices_are_offset_over_transparent_areas() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.0, y: 0.0 },
            trim_transparent: false,
            ..defaults()
        };

        let quad = Quad::from_1x1(&build(vec![&TTTM], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(1.0, 1.0));
        assert_eq!(quad.bottom_right, Vertex::new(2.0, 2.0));

        let quad = Quad::from_1x1(&build(vec![&MTTT], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(0.0, 0.0));
        assert_eq!(quad.bottom_right, Vertex::new(1.0, 1.0));

        let quad = Quad::from_1x1(&build(vec![&TTMT], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(0.0, 1.0));
        assert_eq!(quad.bottom_right, Vertex::new(1.0, 2.0));
    }

    #[test]
    fn when_trim_enabled_pivot_is_reflected_in_vertices_offset() {
        let prefs = Prefs {
            pivot: Pivot { x: 1.0, y: 1.0 },
            trim_transparent: true,
            ..defaults()
        };

        let quad = Quad::from_1x1(&build(vec![&TTTM], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(-1.0, -1.0));
        assert_eq!(quad.bottom_right, Vertex::new(0.0, 0.0));

        let quad = Quad::from_1x1(&build(vec![&MTTT], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(-1.0, -1.0));
        assert_eq!(quad.bottom_right, Vertex::new(0.0, 0.0));

        let quad = Quad::from_1x1(&build(vec![&TTMT], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(-1.0, -1.0));
        assert_eq!(quad.bottom_right, Vertex::new(0.0, 0.0));
    }

    #[test]
    fn when_trim_disabled_pivot_is_reflected_in_vertices_offset() {
        let prefs = Prefs {
            pivot: Pivot { x: 1.0, y: 1.0 },
            trim_transparent: false,
            ..defaults()
        };

        let quad = Quad::from_1x1(&build(vec![&TTTM], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(-1.0, -1.0));
        assert_eq!(quad.bottom_right, Vertex::new(0.0, 0.0));

        let quad = Quad::from_1x1(&build(vec![&MTTT], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(-2.0, -2.0));
        assert_eq!(quad.bottom_right, Vertex::new(-1.0, -1.0));

        let quad = Quad::from_1x1(&build(vec![&TTMT], &prefs)[0]);
        assert_eq!(quad.top_left, Vertex::new(-2.0, -1.0));
        assert_eq!(quad.bottom_right, Vertex::new(-1.0, 0.0));
    }

    #[test]
    fn when_no_per_sprite_pivot_uses_default() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.0, y: 0.0 },
            ..defaults()
        };
        let sprite = &build(vec![&B1X1], &prefs)[0];
        let quad = Quad::from_1x1(sprite);
        assert_eq!(quad.bottom_right, Vertex::new(1.0, 1.0));
        assert_eq!(sprite.pivot, Pivot::new(0.0, 0.0));
    }

    #[test]
    fn per_sprite_pivot_overrides_default() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.0, y: 0.0 },
            ..defaults()
        };
        let sprite = &build(vec![&(&B1X1, (0.5, 0.5))], &prefs)[0];
        let quad = Quad::from_1x1(sprite);
        assert_eq!(quad.bottom_right, Vertex::new(0.5, 0.5));
        assert_eq!(sprite.pivot, Pivot::new(0.5, 0.5));
    }

    #[test]
    fn per_sprite_pivot_doesnt_leak_to_others() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.0, y: 0.0 },
            ..defaults()
        };
        let sprites = &build(vec![&R1X1, &(&B1X1, (0.5, 0.5))], &prefs);
        let quad1 = Quad::from_1x1(&sprites[0]);
        let quad2 = Quad::from_1x1(&sprites[1]);
        assert_eq!(quad1.bottom_right, Vertex::new(1.0, 1.0));
        assert_eq!(quad2.bottom_right, Vertex::new(0.5, 0.5));
        assert_eq!(sprites[0].pivot, Pivot::new(0.0, 0.0));
        assert_eq!(sprites[1].pivot, Pivot::new(0.5, 0.5));
    }

    #[test]
    fn sprite_rect_size_equals_source_texture_divided_by_ppu() {
        let prefs = Prefs {
            ppu: 10.0,
            ..defaults()
        };
        assert_eq!(
            build(vec![&B1X1], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 0.1, 0.1)
        );
        assert_eq!(
            build(vec![&BGRT], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 0.2, 0.2)
        );
        assert_eq!(
            build(vec![&RGB1X3], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 0.1, 0.3)
        );
        assert_eq!(
            build(vec![&RGB3X1], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 0.3, 0.1)
        );
        assert_eq!(
            build(vec![&RGB4X4], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 0.4, 0.4)
        );
    }

    #[test]
    fn sprite_rect_reflects_pivot_offset() {
        assert_eq!(
            build(vec![&(&BGRT, (0.0, 0.0))], &defaults())[0].rect,
            Rect::new(0.0, 0.0, 2.0, 2.0)
        );
        assert_eq!(
            build(vec![&(&BGRT, (0.5, 0.5))], &defaults())[0].rect,
            Rect::new(-1.0, -1.0, 2.0, 2.0)
        );
        assert_eq!(
            build(vec![&(&RGB4X4, (-1.0, 1.0))], &defaults())[0].rect,
            Rect::new(4.0, -4.0, 4.0, 4.0)
        );
    }

    #[test]
    fn sprite_rect_reflects_trimming() {
        let prefs = Prefs {
            trim_transparent: true,
            ..defaults()
        };
        assert_eq!(
            build(vec![&BTGT], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 1.0, 2.0)
        );
        assert_eq!(
            build(vec![&TTTM], &prefs)[0].rect,
            Rect::new(1.0, 1.0, 1.0, 1.0)
        );
        assert_eq!(
            build(vec![&MTTT], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 1.0, 1.0)
        );
        assert_eq!(
            build(vec![&TTMT], &prefs)[0].rect,
            Rect::new(0.0, 1.0, 1.0, 1.0)
        );
    }

    #[test]
    fn sprite_rect_not_affected_by_pivot_offset_when_trim_enabled() {
        let prefs = Prefs {
            trim_transparent: true,
            ..defaults()
        };
        assert_eq!(
            build(vec![&(&BTGT, (1.0, 1.0))], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 1.0, 2.0)
        );
        assert_eq!(
            build(vec![&(&TTTM, (1.0, 1.0))], &prefs)[0].rect,
            Rect::new(1.0, 1.0, 1.0, 1.0)
        );
        assert_eq!(
            build(vec![&(&MTTT, (1.0, 1.0))], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 1.0, 1.0)
        );
        assert_eq!(
            build(vec![&(&TTMT, (1.0, 1.0))], &prefs)[0].rect,
            Rect::new(0.0, 1.0, 1.0, 1.0)
        );
    }

    #[test]
    fn sprite_rect_affected_by_pivot_offset_when_trim_disabled() {
        let prefs = Prefs {
            trim_transparent: false,
            ..defaults()
        };
        assert_eq!(
            build(vec![&(&BTGT, (1.0, 1.0))], &prefs)[0].rect,
            Rect::new(-2.0, -2.0, 2.0, 2.0)
        );
        assert_eq!(
            build(vec![&(&TTTM, (1.0, 1.0))], &prefs)[0].rect,
            Rect::new(-2.0, -2.0, 2.0, 2.0)
        );
        assert_eq!(
            build(vec![&(&MTTT, (1.0, 1.0))], &prefs)[0].rect,
            Rect::new(-2.0, -2.0, 2.0, 2.0)
        );
        assert_eq!(
            build(vec![&(&TTMT, (1.0, 1.0))], &prefs)[0].rect,
            Rect::new(-2.0, -2.0, 2.0, 2.0)
        );
    }

    #[test]
    fn quads_cover_trimmed_content_while_rect_covers_units() {
        let prefs = Prefs {
            unit_size: 5,
            trim_transparent: true,
            ..defaults()
        };
        let sprite = &build(vec![&dot(5, 5, 2, 2)], &prefs)[0];
        assert_eq!(sprite.rect, Rect::new(0.0, 0.0, 5.0, 5.0));
        assert_eq!(sprite.vertices[0], Vertex::new(1.0, 1.0));
        assert_eq!(sprite.vertices[2], Vertex::new(4.0, 4.0));
    }

    #[test]
    fn same_content_at_different_offsets_is_drawn_at_own_positions() {
        let prefs = Prefs {
            unit_size: 4,
            ..defaults()
        };
        let sprites = build(vec![&dot(4, 4, 1, 1), &dot(4, 4, 2, 2)], &prefs);
        assert_eq!(sprites[0].vertices[0], Vertex::new(0.0, 0.0));
        assert_eq!(sprites[0].vertices[2], Vertex::new(3.0, 3.0));
        assert_eq!(sprites[1].vertices[0], Vertex::new(1.0, 1.0));
        assert_eq!(sprites[1].vertices[2], Vertex::new(4.0, 4.0));
        assert_ne!(sprites[0].uvs, sprites[1].uvs);
    }

    #[test]
    fn solid_occurrences_are_drawn_at_own_rects() {
        let prefs = Prefs {
            unit_size: 2,
            padding: 1,
            ..defaults()
        };
        let sprites = build(vec![&fill(4, 4, R), &fill(4, 2, R)], &prefs);
        assert_eq!(sprites[0].vertices.len(), 4);
        assert_eq!(sprites[0].vertices[2], Vertex::new(4.0, 4.0));
        assert_eq!(sprites[1].vertices.len(), 4);
        assert_eq!(sprites[1].vertices[2], Vertex::new(4.0, 2.0));
        assert_eq!(sprites[0].uvs, sprites[1].uvs);
        assert!(sprites[0].uvs.iter().all(|uv| *uv == sprites[0].uvs[0]));
    }

    #[test]
    fn transparent_sprites_are_ignored() {
        let prefs = Prefs {
            trim_transparent: true,
            ..defaults()
        };
        assert!(&build(vec![&TTTT], &prefs).is_empty());
    }

    #[test]
    fn reports_progress() {
        let progress = sample_progress(|p| drop(build(vec![&BTGT], &p)));
        assert_eq!(progress.ratio, 5.0 / 6.0);
    }

    struct Quad {
        top_left: Vertex,
        bottom_left: Vertex,
        bottom_right: Vertex,
        top_right: Vertex,
    }

    impl Quad {
        fn from_1x1(sprite: &DicedSprite) -> Self {
            assert_eq!(sprite.vertices.len(), 4);
            Quad {
                top_left: Vertex {
                    x: sprite.vertices[0].x,
                    y: sprite.vertices[0].y,
                },
                bottom_left: Vertex {
                    x: sprite.vertices[1].x,
                    y: sprite.vertices[1].y,
                },
                bottom_right: Vertex {
                    x: sprite.vertices[2].x,
                    y: sprite.vertices[2].y,
                },
                top_right: Vertex {
                    x: sprite.vertices[3].x,
                    y: sprite.vertices[3].y,
                },
            }
        }
    }

    fn build(src: Vec<&dyn AnySource>, prefs: &Prefs) -> Vec<DicedSprite> {
        let sprites = src.into_iter().map(|s| s.sprite()).collect::<Vec<_>>();
        let diced = crate::dicer::dice(&sprites, prefs).unwrap();
        let merged = crate::merger::merge(&diced, prefs);
        let packed = crate::packer::pack(merged, prefs).unwrap();
        crate::builder::build(&packed, prefs).unwrap()
    }

    fn defaults() -> Prefs {
        Prefs {
            ppu: 1.0,
            unit_size: 1,
            padding: 0,
            trim_transparent: false,
            pivot: Pivot { x: 0.0, y: 0.0 },
            ..Prefs::default()
        }
    }
}
