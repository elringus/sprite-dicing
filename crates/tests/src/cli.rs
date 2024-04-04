//! End-to-end tests of the command line utility.

use crate::common::*;
use cli::models::*;
use rand::{distributions::Alphanumeric, Rng};
use serde_json::Value;
use sprite_dicing::{Artifacts, DicedSprite, Pivot, Prefs, Rect, Uv, Vertex};
use std::path::{Path, PathBuf};
use std::{fs, str::FromStr, vec};

#[test]
fn mono_reproduced() {
    let out_dir = create_temp_dir();

    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let fs_prefs = FsPrefs {
        out: Some(out_dir.to_owned()),
        ..FsPrefs::default()
    };

    cli::dice_dir(&DIR[MONO], &fs_prefs, &prefs).unwrap();
    assert_repro(MONO, build_arts(&out_dir, &fs_prefs), &prefs);
    fs::remove_dir_all(out_dir).unwrap();
}

#[test]
fn nested_reproduced() {
    let out_dir = create_temp_dir();

    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let fs_prefs = FsPrefs {
        out: Some(out_dir.to_owned()),
        recursive: true,
        ..FsPrefs::default()
    };

    cli::dice_dir(&DIR[NESTED], &fs_prefs, &prefs).unwrap();
    assert_repro(NESTED, build_arts(&out_dir, &fs_prefs), &prefs);
    fs::remove_dir_all(out_dir).unwrap();
}

#[test]
fn exotic_reproduced() {
    let out_dir = create_temp_dir();

    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        trim_transparent: false,
        ..Prefs::default()
    };
    let fs_prefs = FsPrefs {
        out: Some(out_dir.to_owned()),
        ..FsPrefs::default()
    };

    cli::dice_dir(&DIR[EXOTIC], &fs_prefs, &prefs).unwrap();
    assert_repro(EXOTIC, build_arts(&out_dir, &fs_prefs), &prefs);
    fs::remove_dir_all(out_dir).unwrap();
}

#[test]
fn errs_on_invalid_source() {
    let out_dir = create_temp_dir();
    let prefs = Prefs::default();
    let fs_prefs = FsPrefs::default();
    assert!(cli::dice_dir(&DIR[INVALID], &fs_prefs, &prefs)
        .is_err_and(|e| e.to_string().contains("error decoding")));
    fs::remove_dir_all(out_dir).unwrap();
}

#[test]
fn can_write_webp() {
    let out_dir = create_temp_dir();

    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let fs_prefs = FsPrefs {
        out: Some(out_dir.to_owned()),
        atlas_format: AtlasFormat::Webp,
        ..FsPrefs::default()
    };

    cli::dice_dir(&DIR[MONO], &fs_prefs, &prefs).unwrap();
    assert!(Path::new(&format!("{}/atlas_0.webp", out_dir.to_str().unwrap())).exists());
    assert_repro(MONO, build_arts(&out_dir, &fs_prefs), &prefs);
    fs::remove_dir_all(out_dir).unwrap();
}

#[test]
fn can_write_tga() {
    let out_dir = create_temp_dir();

    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let fs_prefs = FsPrefs {
        out: Some(out_dir.to_owned()),
        atlas_format: AtlasFormat::Tga,
        ..FsPrefs::default()
    };

    cli::dice_dir(&DIR[MONO], &fs_prefs, &prefs).unwrap();
    assert!(Path::new(&format!("{}/atlas_0.tga", out_dir.to_str().unwrap())).exists());
    assert_repro(MONO, build_arts(&out_dir, &fs_prefs), &prefs);
    fs::remove_dir_all(out_dir).unwrap();
}

fn build_arts(dir: &Path, prefs: &FsPrefs) -> Artifacts {
    let ext = prefs.atlas_format.extension();
    let atlas_img = image::open(format!("{}/atlas_0.{ext}", dir.to_str().unwrap())).unwrap();
    let atlases = vec![to_texture(&atlas_img.into_rgba8())];

    let json = fs::read_to_string(format!("{}/sprites.json", dir.to_str().unwrap())).unwrap();
    let json = serde_json::from_str::<Value>(&json).unwrap();
    let sprites = json
        .as_array()
        .unwrap()
        .iter()
        .map(parse_diced_sprite)
        .collect();

    Artifacts { atlases, sprites }
}

fn parse_diced_sprite(json: &Value) -> DicedSprite {
    DicedSprite {
        id: json["id"].as_str().unwrap().to_owned(),
        atlas_index: json["atlas"].as_u64().unwrap() as usize,
        vertices: json["vertices"]
            .as_array()
            .unwrap()
            .iter()
            .map(|v| Vertex {
                x: v["x"].as_f64().unwrap() as f32,
                y: v["y"].as_f64().unwrap() as f32,
            })
            .collect::<Vec<_>>(),
        uvs: json["uvs"]
            .as_array()
            .unwrap()
            .iter()
            .map(|v| Uv {
                u: v["u"].as_f64().unwrap() as f32,
                v: v["v"].as_f64().unwrap() as f32,
            })
            .collect::<Vec<_>>(),
        indices: json["indices"]
            .as_array()
            .unwrap()
            .iter()
            .map(|v| v.as_u64().unwrap() as usize)
            .collect::<Vec<_>>(),
        rect: Rect {
            x: json["rect"]["x"].as_f64().unwrap() as f32,
            y: json["rect"]["y"].as_f64().unwrap() as f32,
            width: json["rect"]["width"].as_f64().unwrap() as f32,
            height: json["rect"]["height"].as_f64().unwrap() as f32,
        },
        pivot: Pivot::new(0.5, 0.5),
    }
}

fn create_temp_dir() -> PathBuf {
    let rand: String = rand::thread_rng()
        .sample_iter(&Alphanumeric)
        .take(8)
        .map(char::from)
        .collect();
    let crate_dir = env!("CARGO_MANIFEST_DIR");
    let tmp_dir = PathBuf::from_str(&format!("{crate_dir}/tmp-{rand}")).unwrap();
    _ = fs::remove_dir_all(&tmp_dir);
    fs::create_dir(&tmp_dir).unwrap();
    tmp_dir
}
