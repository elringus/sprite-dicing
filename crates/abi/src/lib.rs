//! C/C++ application binary interface of the library.

use sprite_dicing::{AtlasFormat, Pivot, Prefs, RawSprite};
use std::ffi::{c_char, CStr, CString};

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CSprite {
    id: *const c_char,
    bytes: CSlice<u8>,
    extension: *const c_char,
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
    atlas_format: CAtlasFormat,
    ppu: f32,
    pivot_x: f32,
    pivot_y: f32,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub enum CAtlasFormat {
    Png,
    Jpeg,
    Webp,
    Tga,
    Tiff,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CArtifacts {
    atlases: CSlice<CSlice<u8>>,
    sprites: *const c_char,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CSlice<T> {
    ptr: *const T,
    len: usize,
}

/// ABI wrapper over [sprite_dicing::dice_raw].
///
/// # Safety
///
/// Inherent to C/C++ ABI.
#[no_mangle]
pub unsafe extern "C" fn dice(sprites: CSlice<CSprite>, prefs: CPrefs) -> CArtifacts {
    let sprites: Vec<_> = to_slice(sprites).iter().map(|s| to_sprite(s)).collect();
    let format = to_format(prefs.atlas_format);
    let prefs = to_prefs(prefs);
    let raw = sprite_dicing::dice_raw(&sprites, &prefs, &format).unwrap();
    let atlases = to_cslice(&raw.atlases.iter().map(|a| to_cslice(a)).collect::<Vec<_>>());
    let sprites = to_cstr(&raw.sprites);
    CArtifacts { atlases, sprites }
}

unsafe fn to_sprite<'a>(c: &CSprite) -> RawSprite<'a> {
    RawSprite {
        id: to_str(c.id).to_owned(),
        bytes: to_slice(c.bytes),
        extension: to_str(c.extension).to_owned(),
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

unsafe fn to_prefs(c: CPrefs) -> Prefs {
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

unsafe fn to_format(c: CAtlasFormat) -> AtlasFormat {
    match c {
        CAtlasFormat::Png => AtlasFormat::Png,
        CAtlasFormat::Jpeg => AtlasFormat::Jpeg,
        CAtlasFormat::Webp => AtlasFormat::Webp,
        CAtlasFormat::Tga => AtlasFormat::Tga,
        CAtlasFormat::Tiff => AtlasFormat::Tiff,
    }
}

unsafe fn to_str<'a>(c: *const c_char) -> &'a str {
    CStr::from_ptr(c).to_str().unwrap()
}

unsafe fn to_cstr(str: &str) -> *const c_char {
    CString::new(str).unwrap().into_raw()
}

unsafe fn to_slice<'a, T>(c: CSlice<T>) -> &'a [T] {
    std::slice::from_raw_parts(c.ptr, c.len)
}

unsafe fn to_cslice<T>(slice: &[T]) -> CSlice<T> {
    CSlice {
        ptr: slice.as_ptr(),
        len: slice.len(),
    }
}
