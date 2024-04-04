//! C/C++ application binary interface of the library.

mod models;

use models::*;
use sprite_dicing::{
    Artifacts, DicedSprite, Error, Pivot, Pixel, Prefs, Progress, Rect, SourceSprite, Texture, Uv,
    Vertex,
};
use std::ffi::{c_char, CStr, CString};
use std::mem;

/// C ABI wrapper over [sprite_dicing::dice].
///
/// # Safety
///
/// Returned [CSlice]-governed memory is expected to be de-allocated by the caller.
#[no_mangle]
pub unsafe extern "C" fn dice(sprites: CSlice<CSourceSprite>, prefs: CPrefs) -> CResult {
    let prefs = to_prefs(prefs);
    let sprites: Vec<_> = to_slice(sprites).iter().map(|s| to_sprite(s)).collect();
    let result = sprite_dicing::dice(&sprites, &prefs);

    if let Ok(artifacts) = result {
        to_c_ok(artifacts)
    } else {
        to_c_err(result.err().unwrap())
    }
}

fn to_prefs(c: CPrefs) -> Prefs {
    Prefs {
        unit_size: c.unit_size,
        padding: c.padding,
        uv_inset: c.uv_inset,
        trim_transparent: c.trim_transparent,
        atlas_size_limit: c.atlas_size_limit,
        atlas_square: c.atlas_square,
        atlas_pot: c.atlas_pot,
        ppu: c.ppu,
        pivot: Pivot {
            x: c.pivot.x,
            y: c.pivot.y,
        },
        on_progress: if c.has_progress_callback {
            Some(Box::new(move |p| unsafe {
                (c.progress_callback)(to_c_progress(p))
            }))
        } else {
            None
        },
    }
}

fn to_c_err(e: Error) -> CResult {
    let error = to_c_str(&e.to_string());
    let ok = CArtifacts {
        atlases: CSlice::empty(),
        sprites: CSlice::empty(),
    };
    CResult { error, ok }
}

fn to_c_ok(arts: Artifacts) -> CResult {
    let atlases = to_c_slice(arts.atlases.iter().map(to_c_texture).collect());
    let sprites = to_c_slice(arts.sprites.iter().map(to_c_sprite).collect());
    CResult {
        error: to_c_str(""),
        ok: CArtifacts { atlases, sprites },
    }
}

unsafe fn to_sprite(c: &CSourceSprite) -> SourceSprite {
    SourceSprite {
        id: to_str(c.id).to_owned(),
        texture: to_texture(&c.texture),
        pivot: if c.has_pivot {
            Some(Pivot {
                x: c.pivot.x,
                y: c.pivot.y,
            })
        } else {
            None
        },
    }
}

fn to_c_sprite(sprite: &DicedSprite) -> CDicedSprite {
    CDicedSprite {
        id: to_c_str(&sprite.id),
        atlas_index: sprite.atlas_index as u64,
        vertices: to_c_slice(sprite.vertices.iter().map(to_c_vertex).collect()),
        uvs: to_c_slice(sprite.uvs.iter().map(to_c_uv).collect()),
        indices: to_c_slice(sprite.indices.iter().map(|i| *i as u64).collect()),
        rect: to_c_rect(&sprite.rect),
        pivot: to_c_pivot(&sprite.pivot),
    }
}

unsafe fn to_texture(c: &CTexture) -> Texture {
    Texture {
        width: c.width,
        height: c.height,
        pixels: to_slice(c.pixels).iter().map(to_pixel).collect(),
    }
}

fn to_c_texture(tex: &Texture) -> CTexture {
    CTexture {
        width: tex.width,
        height: tex.height,
        pixels: to_c_slice(tex.pixels.iter().map(to_c_pixel).collect()),
    }
}

fn to_pixel(c: &CPixel) -> Pixel {
    Pixel::new(c.r, c.g, c.b, c.a)
}

fn to_c_pixel(pix: &Pixel) -> CPixel {
    CPixel {
        r: pix.r(),
        g: pix.g(),
        b: pix.b(),
        a: pix.a(),
    }
}

fn to_c_vertex(v: &Vertex) -> CVertex {
    CVertex { x: v.x, y: v.y }
}

fn to_c_uv(uv: &Uv) -> CUv {
    CUv { u: uv.u, v: uv.v }
}

fn to_c_rect(rect: &Rect) -> CRect {
    CRect {
        x: rect.x,
        y: rect.y,
        width: rect.width,
        height: rect.height,
    }
}

fn to_c_pivot(p: &Pivot) -> CPivot {
    CPivot { x: p.x, y: p.y }
}

fn to_c_progress(p: Progress) -> CProgress {
    CProgress {
        ratio: p.ratio,
        activity: to_c_str(&p.activity),
    }
}

unsafe fn to_str<'a>(c: *const c_char) -> &'a str {
    CStr::from_ptr(c).to_str().unwrap()
}

fn to_c_str(str: &str) -> *const c_char {
    CString::new(str).unwrap().into_raw()
}

unsafe fn to_slice<'a, T>(c: CSlice<T>) -> &'a [T] {
    std::slice::from_raw_parts(c.ptr, c.len as usize)
}

fn to_c_slice<T>(vec: Vec<T>) -> CSlice<T> {
    let cslice = CSlice {
        ptr: vec.as_ptr(),
        len: vec.len() as u64,
    };
    mem::forget(vec);
    cslice
}
