use crate::models::*;
use std::collections::HashMap;

/// Builds data required to reconstruct diced sprites at runtime: mesh, uvs, etc.
pub(crate) fn build(packed: &[Atlas], prefs: &Prefs) -> Result<Vec<DicedSprite>> {
    if prefs.ppu <= 0.0 {
        return Err(Error::Spec("PPU can't be zero or negative."));
    }

    let mut sprites = vec![];
    for (atlas_idx, atlas) in packed.iter().enumerate() {
        for diced_tex in atlas.packed.iter() {
            let ctx = new_ctx(atlas, atlas_idx, diced_tex, prefs);
            sprites.push(build_it(ctx));
        }
    }

    Ok(sprites)
}

struct Context<'a> {
    ppu: f32,
    trim: bool,
    default_pivot: &'a Pivot,
    atlas_idx: usize,
    diced: &'a DicedTexture,
    uv_rects: &'a HashMap<u64, FRect>,
    vertices: Vec<Vertex>,
    uvs: Vec<Uv>,
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
        uv_rects: &atlas.rects,
        vertices: vec![],
        uvs: vec![],
        indices: vec![],
    }
}

fn build_it(mut ctx: Context) -> DicedSprite {
    for unit in ctx.diced.units.iter() {
        let uv_rect = &ctx.uv_rects[&unit.hash];
        build_unit(&mut ctx, &unit.rect, uv_rect);
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
    }
}

fn build_unit(ctx: &mut Context, unit_rect: &URect, uv_rect: &FRect) {
    let unit_rect = scale_unit_rect(ctx, unit_rect);
    build_quad(ctx, &unit_rect, uv_rect);
}

fn scale_unit_rect(ctx: &Context, unit_rect: &URect) -> FRect {
    FRect {
        x: unit_rect.x as f32 / ctx.ppu,
        y: unit_rect.y as f32 / ctx.ppu,
        width: unit_rect.width as f32 / ctx.ppu,
        height: unit_rect.height as f32 / ctx.ppu,
    }
}

fn build_quad(ctx: &mut Context, unit_rect: &FRect, uv_rect: &FRect) {
    let i = ctx.vertices.len();

    let x_min = unit_rect.x;
    let y_min = unit_rect.y;
    let x_max = unit_rect.x + unit_rect.width;
    let y_max = unit_rect.y + unit_rect.height;

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

fn eval_rect(ctx: &Context, pivot: &Pivot) -> Rect {
    if ctx.trim {
        eval_fit_rect(ctx)
    } else {
        eval_full_rect(ctx, pivot)
    }
}

fn eval_fit_rect(ctx: &Context) -> Rect {
    let mut min_x = f32::INFINITY;
    let mut min_y = f32::INFINITY;
    let mut max_x = f32::NEG_INFINITY;
    let mut max_y = f32::NEG_INFINITY;

    for vertex in ctx.vertices.iter() {
        min_x = min_x.min(vertex.x);
        min_y = min_y.min(vertex.y);
        max_x = max_x.max(vertex.x);
        max_y = max_y.max(vertex.y);
    }

    let width = (max_x - min_x).abs();
    let height = (max_y - min_y).abs();
    Rect::new(min_x, min_y, width, height)
}

fn eval_full_rect(ctx: &Context, pivot: &Pivot) -> Rect {
    let width = ctx.diced.size.width as f32 / ctx.ppu;
    let height = ctx.diced.size.height as f32 / ctx.ppu;
    let x = -pivot.x * width;
    let y = -pivot.y * height;
    Rect::new(x, y, width, height)
}

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
    fn per_sprite_pivot_overrides_default() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.0, y: 0.0 },
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&(&B1X1, (0.5, 0.5))], &prefs)[0]);
        assert_eq!(quad.bottom_right, Vertex::new(0.5, 0.5));
    }

    #[test]
    fn per_sprite_pivot_doesnt_leak_to_others() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.0, y: 0.0 },
            ..defaults()
        };
        let sprites = &build(vec![&R1X1, &(&B1X1, (0.5, 0.5))], &prefs);
        let quad1 = Quad::from_1x1(&sprites[1]);
        let quad2 = Quad::from_1x1(&sprites[0]);
        assert_eq!(quad1.bottom_right, Vertex::new(1.0, 1.0));
        assert_eq!(quad2.bottom_right, Vertex::new(0.5, 0.5));
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
    fn transparent_sprites_are_ignored() {
        let prefs = Prefs {
            trim_transparent: true,
            ..defaults()
        };
        assert!(&build(vec![&TTTT], &prefs).is_empty());
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
        let packed = crate::packer::pack(diced, prefs).unwrap();
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
