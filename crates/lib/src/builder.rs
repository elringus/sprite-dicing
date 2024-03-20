use crate::models::*;
use std::collections::HashMap;

/// Builds data required to reconstruct diced sprites at runtime: mesh, uvs, etc.
pub(crate) fn build(packed: &[Atlas], prefs: &Prefs) -> Result<Vec<DicedSprite>> {
    if prefs.ppu == 0 {
        return Err(Error::Spec("PPU can't be zero."));
    }

    let mut sprites = vec![];
    for (atlas_idx, atlas) in packed.iter().enumerate() {
        for diced_tex in atlas.packed.iter() {
            let ctx = new_ctx(atlas, atlas_idx, diced_tex, prefs);
            sprites.push(pack_it(ctx));
        }
    }

    Ok(sprites)
}

struct Context<'a> {
    ppu: f32,
    default_pivot: &'a Pivot,
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
        ppu: prefs.ppu as f32,
        default_pivot: &prefs.pivot,
        atlas_idx,
        diced,
        uv_rects: &atlas.rects,
        vertices: vec![],
        uvs: vec![],
        indices: vec![],
    }
}

fn pack_it(mut ctx: Context) -> DicedSprite {
    for unit in ctx.diced.units.iter() {
        let uv_rect = &ctx.uv_rects[&unit.hash];
        build_unit(&mut ctx, &unit.rect, uv_rect);
    }

    let rect = eval_boundaries(&ctx);
    let pivot = eval_pivot(&ctx, &rect);
    offset_vertices_over_pivot(&mut ctx, &rect, &pivot);

    DicedSprite {
        id: ctx.diced.id.to_owned(),
        atlas_index: ctx.atlas_idx,
        vertices: ctx.vertices,
        uvs: ctx.uvs,
        indices: ctx.indices,
        rect,
        pivot,
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

fn eval_pivot(ctx: &Context, rect: &Rect) -> Pivot {
    match &ctx.diced.pivot {
        Some(source_pivot) => Pivot {
            x: (source_pivot.x / ctx.ppu - rect.x) / rect.width,
            y: (source_pivot.y / ctx.ppu - rect.y) / rect.height,
        },
        _ => ctx.default_pivot.to_owned(),
    }
}

fn offset_vertices_over_pivot(ctx: &mut Context, sprite_rect: &Rect, pivot: &Pivot) {
    // TODO: -sprite_rect.x/y is confusing, remove minus and negate the expression.
    let origin_x = -sprite_rect.x / sprite_rect.width;
    let origin_y = -sprite_rect.y / sprite_rect.height;
    let delta_x = sprite_rect.width * pivot.x - sprite_rect.width * origin_x;
    let delta_y = sprite_rect.height * pivot.y - sprite_rect.height * origin_y;
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
    #[should_panic(expected = "PPU can't be zero.")]
    fn errs_when_ppu_zero() {
        let prefs = Prefs {
            ppu: 0,
            ..defaults()
        };
        build(vec![&R1X1, &B1X1], &prefs);
    }

    #[test]
    fn sprite_vertices_form_quad() {
        let vertices = &build(vec![&B1X1], &defaults())[0].vertices;

        let top_left = &vertices[0];
        assert_eq!(top_left.x, 0.0);
        assert_eq!(top_left.y, 0.0);

        let bottom_left = &vertices[1];
        assert_eq!(bottom_left.x, 0.0);
        assert_eq!(bottom_left.y, 1.0);

        let bottom_right = &vertices[2];
        assert_eq!(bottom_right.x, 1.0);
        assert_eq!(bottom_right.y, 1.0);

        let top_right = &vertices[3];
        assert_eq!(top_right.x, 1.0);
        assert_eq!(top_right.y, 0.0);
    }

    #[test]
    fn vertices_are_scaled_by_ppu() {
        let prefs = Prefs {
            ppu: 1,
            ..defaults()
        };
        let bottom_right = &build(vec![&B1X1], &prefs)[0].vertices[2];
        assert_eq!(bottom_right.x, 1.0);
        assert_eq!(bottom_right.y, 1.0);

        let prefs = Prefs {
            ppu: 2,
            ..defaults()
        };
        let bottom_right = &build(vec![&B1X1], &prefs)[0].vertices[2];
        assert_eq!(bottom_right.x, 0.5);
        assert_eq!(bottom_right.y, 0.5);
    }

    #[test] // TODO: Test mesh offset instead. Remove DicedSprite.pivot (it's same as source).
    fn default_pivot_is_applied() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.66, y: -0.66 },
            ..defaults()
        };
        assert_eq!(build(vec![&B1X1], &prefs)[0].pivot, prefs.pivot);
    }

    #[test] // TODO: - || -
    fn custom_pivot_overrides_default() {
        let prefs = Prefs {
            pivot: Pivot { x: 0.66, y: -0.66 },
            ..defaults()
        };
        assert_eq!(
            build(vec![&(&B1X1, (0.1, 0.2))], &prefs)[0].pivot,
            Pivot { x: 0.1, y: 0.2 }
        );
    }

    #[test]
    fn sprite_rect_size_equals_source_texture_divided_by_ppu() {
        let prefs = Prefs {
            ppu: 10,
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
    fn when_trimmed_sprite_rect_size_proportion_is_changed() {
        let prefs = Prefs {
            ppu: 10,
            trim_transparent: true,
            ..defaults()
        };
        assert_eq!(
            build(vec![&BTGT], &prefs)[0].rect,
            Rect::new(0.0, 0.0, 0.1, 0.2)
        );
    }

    fn build(src: Vec<&dyn AnySource>, prefs: &Prefs) -> Vec<DicedSprite> {
        let sprites = src.into_iter().map(|s| s.sprite()).collect::<Vec<_>>();
        let diced = crate::dicer::dice(&sprites, prefs).unwrap();
        let packed = crate::packer::pack(diced, prefs).unwrap();
        crate::builder::build(&packed, prefs).unwrap()
    }

    fn defaults() -> Prefs {
        Prefs {
            ppu: 1,
            unit_size: 1,
            padding: 0,
            trim_transparent: false,
            ..Prefs::default()
        }
    }
}
