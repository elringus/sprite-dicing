//! C/C++ application binary interface of the library.

use sprite_dicing::{AtlasFormat, DicedSprite, Pivot, Prefs, RawSprite, Rect, Uv, Vertex};
use std::ffi::{c_char, CStr, CString};
use std::mem;

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CSourceSprite {
    id: *const c_char,
    bytes: CSlice<u8>,
    format: *const c_char,
    has_pivot: bool,
    pivot_x: f32,
    pivot_y: f32,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CPrefs {
    unit_size: u32,
    padding: u32,
    uv_inset: f32,
    trim_transparent: bool,
    atlas_size_limit: u32,
    atlas_square: bool,
    atlas_pot: bool,
    atlas_format: u8,
    ppu: f32,
    pivot_x: f32,
    pivot_y: f32,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CDicedSprite {
    pub id: *const c_char,
    pub atlas_index: u64,
    pub vertices: CSlice<CVertex>,
    pub uvs: CSlice<CUv>,
    pub indices: CSlice<u64>,
    pub rect: CRect,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CVertex {
    pub x: f32,
    pub y: f32,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CUv {
    pub u: f32,
    pub v: f32,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CRect {
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CArtifacts {
    atlases: CSlice<CSlice<u8>>,
    sprites: CSlice<CDicedSprite>,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CSlice<T> {
    ptr: *const T,
    len: u64,
}

/// ABI wrapper over [sprite_dicing::dice_raw].
///
/// # Safety
///
/// Returned [CSlice]-governed memory is expected to be de-allocated by the caller.
#[no_mangle]
pub unsafe extern "C" fn dice(sprites: CSlice<CSourceSprite>, prefs: CPrefs) -> CArtifacts {
    let sprites: Vec<_> = to_slice(sprites).iter().map(|s| to_sprite(s)).collect();
    let format = to_format(prefs.atlas_format);
    let prefs = to_prefs(prefs);
    let raw = sprite_dicing::dice_raw(&sprites, &prefs, &format).unwrap();
    let atlases = to_c_slice(raw.atlases.into_iter().map(to_c_slice).collect());
    let sprites = to_c_slice(raw.sprites.iter().map(|s| to_c_sprite(s)).collect());
    CArtifacts { atlases, sprites }
}

unsafe fn to_sprite<'a>(c: &CSourceSprite) -> RawSprite<'a> {
    RawSprite {
        id: to_str(c.id).to_owned(),
        bytes: to_slice(c.bytes),
        format: to_str(c.format).to_owned(),
        pivot: if c.has_pivot {
            Some(Pivot {
                x: c.pivot_x,
                y: c.pivot_y,
            })
        } else {
            None
        },
    }
}

unsafe fn to_c_sprite(sprite: &DicedSprite) -> CDicedSprite {
    CDicedSprite {
        id: to_c_str(&sprite.id),
        atlas_index: sprite.atlas_index as u64,
        vertices: to_c_slice(sprite.vertices.iter().map(to_c_vertex).collect()),
        uvs: to_c_slice(sprite.uvs.iter().map(to_c_uv).collect()),
        indices: to_c_slice(sprite.indices.iter().map(|i| *i as u64).collect()),
        rect: to_c_rect(&sprite.rect),
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
            x: c.pivot_x,
            y: c.pivot_y,
        },
    }
}

unsafe fn to_format(c: u8) -> AtlasFormat {
    match c {
        0 => AtlasFormat::Png,
        1 => AtlasFormat::Jpeg,
        2 => AtlasFormat::Webp,
        3 => AtlasFormat::Tga,
        4 => AtlasFormat::Tiff,
        _ => AtlasFormat::Png,
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
