use std::ffi::c_char;
use std::ptr::null;

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CSourceSprite {
    pub id: *const c_char,
    pub texture: CTexture,
    pub has_pivot: bool,
    pub pivot: CPivot,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CTexture {
    pub width: u32,
    pub height: u32,
    pub pixels: CSlice<CPixel>,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CPixel {
    pub r: u8,
    pub g: u8,
    pub b: u8,
    pub a: u8,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CPrefs {
    pub unit_size: u32,
    pub padding: u32,
    pub uv_inset: f32,
    pub trim_transparent: bool,
    pub atlas_size_limit: u32,
    pub atlas_square: bool,
    pub atlas_pot: bool,
    pub ppu: f32,
    pub pivot: CPivot,
    pub has_progress_callback: bool,
    pub progress_callback: unsafe extern "C" fn(CProgress),
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CResult {
    pub error: *const c_char,
    pub ok: CArtifacts,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CArtifacts {
    pub atlases: CSlice<CTexture>,
    pub sprites: CSlice<CDicedSprite>,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CDicedSprite {
    pub id: *const c_char,
    pub atlas_index: u64,
    pub vertices: CSlice<CVertex>,
    pub uvs: CSlice<CUv>,
    pub indices: CSlice<u64>,
    pub rect: CRect,
    pub pivot: CPivot,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CVertex {
    pub x: f32,
    pub y: f32,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CUv {
    pub u: f32,
    pub v: f32,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CRect {
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CPivot {
    pub x: f32,
    pub y: f32,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CProgress {
    pub ratio: f32,
    pub activity: *const c_char,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct CSlice<T> {
    pub ptr: *const T,
    pub len: u64,
}

impl<T> CSlice<T> {
    pub const fn empty() -> CSlice<T> {
        CSlice {
            ptr: null(),
            len: 0,
        }
    }
}
