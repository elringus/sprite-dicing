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
    default_pivot: Option<&'a Pivot>,
    atlas_idx: usize,
    diced: &'a DicedTexture,
    uv_rects: &'a HashMap<u64, FRect>,
    vertices: Vec<Vertex>,
    uvs: Vec<UV>,
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
        default_pivot: prefs.pivot.as_ref(),
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

    let mut rect = eval_boundaries(&ctx);

    if ctx.diced.pivot.is_some() || ctx.default_pivot.is_some() {
        let pivot = ctx
            .diced
            .pivot
            .as_ref()
            .unwrap_or_else(|| ctx.default_pivot.unwrap());
        offset_vertices_over_pivot(&mut ctx, &rect, pivot);
        rect = eval_boundaries(&ctx);
    }

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
    let ratio = 1.0 / ctx.ppu;
    FRect {
        x: unit_rect.x as f32 * ratio,
        y: unit_rect.y as f32 * ratio,
        width: unit_rect.width as f32 * ratio,
        height: unit_rect.height as f32 * ratio,
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
        UV { u: u_min, v: v_min },
        UV { u: u_min, v: v_max },
        UV { u: u_max, v: v_max },
        UV { u: u_max, v: v_min },
    ]);

    ctx.indices.extend([i, i + 1, i + 2, i + 2, i + 3, i]);
}

// TODO: Just use unit_rect?
fn eval_boundaries(ctx: &Context) -> Rect {
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

    Rect {
        x: min_x,
        y: min_y,
        width: (max_x - min_x).abs(),
        height: (max_y - min_y).abs(),
    }
}

fn offset_vertices_over_pivot(ctx: &mut Context, sprite_rect: &Rect, pivot: &Pivot) {
    let offset_x = (pivot.x - sprite_rect.x) / sprite_rect.width;
    let offset_y = (pivot.y - sprite_rect.y) / sprite_rect.height;
    let origin_x = sprite_rect.x / sprite_rect.width;
    let origin_y = sprite_rect.y / sprite_rect.height;
    let delta_x = sprite_rect.width * offset_x + sprite_rect.width * origin_x;
    let delta_y = sprite_rect.height * offset_y + sprite_rect.height * origin_y;
    for idx in 0..ctx.vertices.len() {
        ctx.vertices[idx].x -= delta_x;
        ctx.vertices[idx].y -= delta_y;
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
        build(vec![&R1X1, &B1X1], &prefs);
    }

    #[test]
    fn sprite_vertices_form_quad() {
        let quad = Quad::from_1x1(&build(vec![&B1X1], &defaults())[0]);
        assert_eq!(quad.top_left.x, 0.0);
        assert_eq!(quad.top_left.y, 0.0);

        assert_eq!(quad.bottom_left.x, 0.0);
        assert_eq!(quad.bottom_left.y, 1.0);

        assert_eq!(quad.bottom_right.x, 1.0);
        assert_eq!(quad.bottom_right.y, 1.0);

        assert_eq!(quad.top_right.x, 1.0);
        assert_eq!(quad.top_right.y, 0.0);
    }

    #[test]
    fn vertices_are_scaled_by_ppu() {
        let prefs = Prefs {
            ppu: 1.0,
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&B1X1], &prefs)[0]);
        assert_eq!(quad.bottom_right.x, 1.0);
        assert_eq!(quad.bottom_right.y, 1.0);

        let prefs = Prefs {
            ppu: 2.0,
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&B1X1], &prefs)[0]);
        assert_eq!(quad.bottom_right.x, 0.5);
        assert_eq!(quad.bottom_right.y, 0.5);
    }

    #[test]
    fn zero_pivot_doesnt_offset_vertices() {
        let prefs = Prefs {
            pivot: Some(Pivot { x: 0.0, y: 0.0 }),
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&B1X1], &prefs)[0]);
        assert_eq!(quad.bottom_right.x, 1.0);
        assert_eq!(quad.bottom_right.y, 1.0);
    }

    #[test]
    fn non_zero_pivot_offsets_vertices() {
        let prefs = Prefs {
            pivot: Some(Pivot { x: 0.5, y: 0.5 }),
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&B1X1], &prefs)[0]);
        assert_eq!(quad.bottom_right.x, 0.5);
        assert_eq!(quad.bottom_right.y, 0.5);
    }

    #[test]
    fn sprite_pivot_overrides_default() {
        let prefs = Prefs {
            pivot: Some(Pivot { x: 0.0, y: 0.0 }),
            ..defaults()
        };
        let quad = Quad::from_1x1(&build(vec![&(&B1X1, (0.5, 0.5))], &prefs)[0]);
        assert_eq!(quad.bottom_right.x, 0.5);
        assert_eq!(quad.bottom_right.y, 0.5);
    }

    #[test]
    fn sprite_pivot_doesnt_leak_to_others() {
        let prefs = Prefs {
            pivot: Some(Pivot { x: 0.0, y: 0.0 }),
            ..defaults()
        };
        let sprites = &build(vec![&R1X1, &(&B1X1, (0.5, 0.5))], &prefs);
        let quad1 = Quad::from_1x1(&sprites[1]);
        let quad2 = Quad::from_1x1(&sprites[0]);
        assert_eq!(quad1.bottom_right.x, 1.0);
        assert_eq!(quad1.bottom_right.y, 1.0);
        assert_eq!(quad2.bottom_right.x, 0.5);
        assert_eq!(quad2.bottom_right.y, 0.5);
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
    fn sprite_rect_reflects_trimming() {
        let prefs = Prefs {
            trim_transparent: true,
            ..defaults()
        };
        assert_eq!(
            build(vec![&BTGT], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 1.0, 2.0)
        );
    }

    #[test]
    fn sprite_rect_reflects_pivot_offset() {
        assert_eq!(
            build(vec![&(&BGRT, (0.5, 0.5))], &defaults())[0].rect,
            Rect::new(-0.5, -0.5, 2.0, 2.0)
        );
        assert_eq!(
            build(vec![&(&RGB4X4, (0.5, 0.5))], &defaults())[0].rect,
            Rect::new(-0.5, -0.5, 4.0, 4.0)
        );
    }

    #[test]
    fn when_transparent_and_trim_enabled_sprite_is_ignored() {
        let prefs = Prefs {
            trim_transparent: true,
            ..defaults()
        };
        assert!(&build(vec![&TTTT], &prefs).is_empty());
    }

    #[test]
    fn when_transparent_and_trim_disabled_sprite_is_build_normally() {
        let prefs = Prefs {
            trim_transparent: false,
            ..defaults()
        };
        let sprite = &build(vec![&TTTT], &prefs)[0];
        assert_eq!(sprite.rect, Rect::new(0.0, 0.0, 2.0, 2.0));
        assert_eq!(sprite.vertices.len(), 16);
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
            ..Prefs::default()
        }
    }
}
