use crate::common::img;
use image::RgbaImage;
use once_cell::sync::Lazy;
use sprite_dicing::SourceSprite;
use std::collections::HashMap;
use std::fs;
use std::path::{Path, PathBuf};

pub const MONO: &str = "mono";
// pub const NESTED: &str = "nested";
// pub const EXOTIC: &str = "exotic";
// pub const INVALID: &str = "invalid";

pub static RAW: Lazy<RawsByFixture> = Lazy::new(cache_raws);
pub static SRC: Lazy<SourcesByFixture> = Lazy::new(cache_sources);

pub type SourcesByFixture = HashMap<String, Vec<SourceSprite>>;
pub type RawsByFixture = HashMap<String, RawBySpriteId>;
pub type RawBySpriteId = HashMap<String, RgbaImage>;

fn cache_raws() -> RawsByFixture {
    let mut raws = HashMap::new();
    for entry in fs::read_dir(get_fixtures_root()).unwrap() {
        let root = entry.unwrap().path();
        let fixture = root.file_name().unwrap().to_str().unwrap().to_owned();
        let images = img::load_all(&root)
            .into_iter()
            .map(|(path, image)| (build_id(&path, &root), image))
            .collect();
        raws.insert(fixture, images);
    }
    raws
}

fn cache_sources() -> SourcesByFixture {
    let mut src = HashMap::new();
    for entry in fs::read_dir(get_fixtures_root()).unwrap() {
        let root = entry.unwrap().path();
        let fixture = root.file_name().unwrap().to_str().unwrap().to_owned();
        let images = img::load_all(&root)
            .into_iter()
            .map(|(path, image)| create_sprite(&path, image, &root))
            .collect();
        src.insert(fixture, images);
    }
    src
}

fn create_sprite(path: &Path, image: RgbaImage, root: &Path) -> SourceSprite {
    SourceSprite {
        id: build_id(path, root),
        texture: img::to_texture(&image),
        pivot: None,
    }
}

fn build_id(path: &Path, root: &Path) -> String {
    path.strip_prefix(root)
        .unwrap()
        .with_extension("")
        .file_name()
        .unwrap()
        .to_str()
        .unwrap()
        .to_owned()
}

fn get_fixtures_root() -> PathBuf {
    Path::new(&format!("{}/fixtures", env!("CARGO_MANIFEST_DIR"))).to_owned()
}
