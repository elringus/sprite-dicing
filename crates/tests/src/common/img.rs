use image::RgbaImage;

pub fn bytes_to_img(bytes: &[u8]) -> RgbaImage {
    image::load_from_memory(bytes)
        .unwrap()
        .as_rgba8()
        .unwrap()
        .to_owned()
}

pub fn is_clear(img: &RgbaImage) -> bool {
    img.pixels().all(|p| p.0[3] == 0)
}
