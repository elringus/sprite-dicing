use once_cell::sync::Lazy;
use sprite_dicing::RawSprite;
use std::collections::HashMap;
use std::fs;
use std::path::{Path, PathBuf};

pub static MONO: Lazy<Vec<RawSprite>> = Lazy::new(|| cache_src("mono"));

static BYTES: Lazy<HashMap<String, HashMap<PathBuf, Vec<u8>>>> = Lazy::new(cache_bytes);

fn cache_bytes() -> HashMap<String, HashMap<PathBuf, Vec<u8>>> {
    let mut map = HashMap::new();
    let crate_root = env!("CARGO_MANIFEST_DIR");
    let fixtures_root = format!("{crate_root}/fixtures");
    for entry in fs::read_dir(fixtures_root).unwrap() {
        let dir = entry.unwrap().path();
        let bytes = load_bytes(&dir);
        let key = dir.file_name().unwrap().to_str().unwrap().to_owned();
        map.insert(key, bytes);
    }
    map
}

fn load_bytes(dir: &Path) -> HashMap<PathBuf, Vec<u8>> {
    let mut map = HashMap::new();
    for entry in fs::read_dir(dir).unwrap() {
        let path = entry.unwrap().path();
        let bytes = fs::read(&path).unwrap();
        map.insert(path, bytes);
    }
    map
}

fn cache_src(fixture: &'static str) -> Vec<RawSprite> {
    let src = &BYTES[fixture];
    src.iter().map(|s| create_src(s.0, s.1)).collect()
}

fn create_src<'a>(path: &Path, bytes: &'a [u8]) -> RawSprite<'a> {
    RawSprite {
        id: path.to_str().unwrap().to_owned(),
        bytes,
        format: path.extension().unwrap().to_str().unwrap().to_owned(),
        pivot: None,
    }
}
