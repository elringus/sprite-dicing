use crate::common::img;
use image::RgbaImage;
use sprite_dicing::SourceSprite;
use std::collections::HashMap;
use std::fs;
use std::path::{Path, PathBuf};
use std::sync::LazyLock;

pub const MONO: &str = "mono";
pub const ICONS: &str = "icons";
pub const SIZED: &str = "sized";
pub const TRIM: &str = "trim";
pub const NESTED: &str = "nested";
pub const EXOTIC: &str = "exotic";
pub const INVALID: &str = "invalid";

pub static SRC: LazyLock<SourcesByFixture> = LazyLock::new(cache_sources);
pub static DIR: LazyLock<DirByFixture> = LazyLock::new(cache_dirs);
pub static RAW: LazyLock<RawsByFixture> = LazyLock::new(cache_raws);

pub type SourcesByFixture = HashMap<String, Vec<SourceSprite>>;
pub type DirByFixture = HashMap<String, PathBuf>;
pub type RawsByFixture = HashMap<String, RawBySpriteId>;
pub type RawBySpriteId = HashMap<String, RgbaImage>;

fn cache_sources() -> SourcesByFixture {
    let mut src = HashMap::new();
    for entry in fs::read_dir(get_fixtures_root()).unwrap() {
        let root = entry.unwrap().path();
        let fixture = root.file_name().unwrap().to_str().unwrap().to_owned();
        let sources = img::load_all(&root)
            .into_iter()
            .map(|(path, image)| create_sprite(&path, image, &root))
            .collect();
        src.insert(fixture, sources);
    }
    src
}

fn cache_dirs() -> DirByFixture {
    let mut dirs = HashMap::new();
    for entry in fs::read_dir(get_fixtures_root()).unwrap() {
        let root = entry.unwrap().path();
        let fixture = root.file_name().unwrap().to_str().unwrap().to_owned();
        dirs.insert(fixture, root);
    }
    dirs
}

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
        .to_str()
        .unwrap()
        .replace('\\', "/")
        .to_owned()
}

fn get_fixtures_root() -> PathBuf {
    Path::new(&format!("{}/fixtures", env!("CARGO_MANIFEST_DIR"))).to_owned()
}
