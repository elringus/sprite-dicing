//! Data models of `sprite_dicing.h`, mirrored verbatim.

#![allow(non_camel_case_types)]

use std::ffi::{c_char, c_void};

#[repr(C)]
#[derive(Clone, Copy, PartialEq, Default)]
pub struct sd_pixel {
    pub r: u8,
    pub g: u8,
    pub b: u8,
    pub a: u8,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct sd_texture {
    pub width: u32,
    pub height: u32,
    pub pixels: *const sd_pixel,
}

#[repr(C)]
#[derive(Clone, Copy, PartialEq, Debug)]
pub struct sd_pivot {
    pub x: f32,
    pub y: f32,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct sd_source_sprite {
    pub id: *const c_char,
    pub texture: sd_texture,
    pub has_pivot: bool,
    pub pivot: sd_pivot,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct sd_progress {
    pub ratio: f32,
    pub activity: *const c_char,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct sd_prefs {
    pub unit_size: u32,
    pub padding: u32,
    pub uv_inset: f32,
    pub trim_transparent: bool,
    pub atlas_size_limit: u32,
    pub atlas_square: bool,
    pub atlas_pot: bool,
    pub ppu: f32,
    pub pivot: sd_pivot,
    pub on_progress: Option<unsafe extern "C" fn(progress: sd_progress, user_data: *mut c_void)>,
    pub user_data: *mut c_void,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct sd_vertex {
    pub x: f32,
    pub y: f32,
}

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct sd_uv {
    pub u: f32,
    pub v: f32,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct sd_rect {
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct sd_diced_sprite {
    pub id: *const c_char,
    pub atlas_index: usize,
    pub vertices: *const sd_vertex,
    pub uvs: *const sd_uv,
    pub vertex_count: usize,
    pub indices: *const u32,
    pub index_count: usize,
    pub rect: sd_rect,
    pub pivot: sd_pivot,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct sd_result {
    pub error: *const c_char,
    pub atlases: *const sd_texture,
    pub atlas_count: usize,
    pub sprites: *const sd_diced_sprite,
    pub sprite_count: usize,
}
