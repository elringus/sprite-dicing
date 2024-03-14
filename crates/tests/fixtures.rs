// pub static B: Lazy<Texture> = Lazy::new(|| load("1x1/B"));
// pub static R: Lazy<Texture> = Lazy::new(|| load("1x1/R"));
// pub static BGRC: Lazy<Texture> = Lazy::new(|| load("2x2/BGRC"));
// pub static BCGR: Lazy<Texture> = Lazy::new(|| load("2x2/BCGR"));
// pub static BCGC: Lazy<Texture> = Lazy::new(|| load("2x2/BCGC"));
// pub static CCCC: Lazy<Texture> = Lazy::new(|| load("2x2/CCCC"));
// pub static RGB1X3: Lazy<Texture> = Lazy::new(|| load("RGB1x3"));
// pub static RGB3X1: Lazy<Texture> = Lazy::new(|| load("RGB3x1"));
// pub static RGB4X4: Lazy<Texture> = Lazy::new(|| load("RGB4x4"));
// pub static UIC4X4: Lazy<Texture> = Lazy::new(|| load("UIC4x4"));
//
// fn load(name: &'static str) -> Texture {
//     let lib_dir = env!("CARGO_MANIFEST_DIR");
//     let path = format!("{lib_dir}/../tests/fixtures/{name}.png");
//     let img = image::open(path).unwrap();
//     Texture {
//         width: img.width() as u16,
//         height: img.height() as u16,
//         pixels: img
//             .as_rgba8()
//             .unwrap()
//             .pixels()
//             .map(|p| Pixel::new(p[0], p[1], p[2], p[3]))
//             .collect(),
//     }
// }
