//! End-to-end tests of the command line utility.

// use crate::common::*;
// use rand::{distributions::Alphanumeric, Rng};
// use serde_json::Value;
// use sprite_dicing::{
//     AtlasFormat, DicedSprite, FsPrefs, Pivot, Prefs, RawArtifacts, Rect, Uv, Vertex,
// };
// use std::path::{Path, PathBuf};
// use std::{fs, str::FromStr, vec};
//
// #[test]
// fn mono_reproduces_in_dir() {
//     let out_dir = create_temp_dir();
//
//     let prefs = Prefs {
//         unit_size: 1,
//         padding: 0,
//         ..Prefs::default()
//     };
//     let fs_prefs = FsPrefs {
//         out: Some(out_dir.to_owned()),
//         ..FsPrefs::default()
//     };
//
//     sprite_dicing::dice_dir(&get_fixture_dir("mono"), &fs_prefs, &prefs).unwrap();
//     assert_repro(&MONO, build_arts_from_dir(&out_dir, &fs_prefs), &prefs);
//     fs::remove_dir_all(out_dir).unwrap();
// }
//
// #[test]
// fn exotic_reproduces_in_dir() {
//     let out_dir = create_temp_dir();
//
//     let prefs = Prefs {
//         unit_size: 1,
//         padding: 0,
//         ..Prefs::default()
//     };
//     let fs_prefs = FsPrefs {
//         out: Some(out_dir.to_owned()),
//         ..FsPrefs::default()
//     };
//
//     sprite_dicing::dice_dir(&get_fixture_dir("exotic"), &fs_prefs, &prefs).unwrap();
//     assert_repro(&EXOTIC, build_arts_from_dir(&out_dir, &fs_prefs), &prefs);
//     fs::remove_dir_all(out_dir).unwrap();
// }
//
// #[test]
// fn can_write_webp() {
//     let out_dir = create_temp_dir();
//
//     let prefs = Prefs {
//         unit_size: 1,
//         padding: 0,
//         ..Prefs::default()
//     };
//     let fs_prefs = FsPrefs {
//         out: Some(out_dir.to_owned()),
//         atlas_format: AtlasFormat::Webp,
//         ..FsPrefs::default()
//     };
//
//     sprite_dicing::dice_dir(&get_fixture_dir("mono"), &fs_prefs, &prefs).unwrap();
//     assert_repro(&MONO, build_arts_from_dir(&out_dir, &fs_prefs), &prefs);
//     fs::remove_dir_all(out_dir).unwrap();
// }
//
// #[test]
// fn can_find_sources_in_nested_directories() {
//     let out_dir = create_temp_dir();
//
//     let prefs = Prefs {
//         unit_size: 1,
//         padding: 0,
//         ..Prefs::default()
//     };
//     let fs_prefs = FsPrefs {
//         out: Some(out_dir.to_owned()),
//         recursive: true,
//         ..FsPrefs::default()
//     };
//
//     sprite_dicing::dice_dir(&get_fixture_dir("nested"), &fs_prefs, &prefs).unwrap();
//     let arts = build_arts_from_dir(&out_dir, &fs_prefs);
//     assert_eq!(arts.sprites.len(), 6);
//     fs::remove_dir_all(out_dir).unwrap();
// }
//
// #[test]
// fn errs_on_invalid_dir() {
//     let prefs = Prefs::default();
//     let fs_prefs = FsPrefs::default();
//     assert!(
//         sprite_dicing::dice_dir(&get_fixture_dir("foo"), &fs_prefs, &prefs)
//             .is_err_and(|e| e.to_string().contains("cannot find the path"))
//     );
// }
//
// #[test]
// fn errs_on_invalid_sources() {
//     let out_dir = create_temp_dir();
//     let prefs = Prefs::default();
//     let fs_prefs = FsPrefs::default();
//     assert!(
//         sprite_dicing::dice_dir(&get_fixture_dir("invalid"), &fs_prefs, &prefs)
//             .is_err_and(|e| e.to_string().contains("error decoding"))
//     );
//     fs::remove_dir_all(out_dir).unwrap();
// }
//
// fn build_arts_from_dir(dir: &Path, prefs: &FsPrefs) -> RawArtifacts {
//     let ext = prefs.atlas_format.extension();
//     let atlases = vec![fs::read(format!("{}/atlas_0.{ext}", dir.to_str().unwrap())).unwrap()];
//     let json = fs::read_to_string(format!("{}/sprites.json", dir.to_str().unwrap())).unwrap();
//     let json = serde_json::from_str::<Value>(&json).unwrap();
//     let sprites = json
//         .as_array()
//         .unwrap()
//         .iter()
//         .map(parse_diced_sprite)
//         .collect();
//     RawArtifacts { atlases, sprites }
// }
//
// fn parse_diced_sprite(json: &Value) -> DicedSprite {
//     DicedSprite {
//         id: json["id"].as_str().unwrap().to_owned(),
//         atlas_index: json["atlas"].as_u64().unwrap() as usize,
//         vertices: json["vertices"]
//             .as_array()
//             .unwrap()
//             .iter()
//             .map(|v| Vertex {
//                 x: v["x"].as_f64().unwrap() as f32,
//                 y: v["y"].as_f64().unwrap() as f32,
//             })
//             .collect::<Vec<_>>(),
//         uvs: json["uvs"]
//             .as_array()
//             .unwrap()
//             .iter()
//             .map(|v| Uv {
//                 u: v["u"].as_f64().unwrap() as f32,
//                 v: v["v"].as_f64().unwrap() as f32,
//             })
//             .collect::<Vec<_>>(),
//         indices: json["indices"]
//             .as_array()
//             .unwrap()
//             .iter()
//             .map(|v| v.as_u64().unwrap() as usize)
//             .collect::<Vec<_>>(),
//         rect: Rect {
//             x: json["rect"]["x"].as_f64().unwrap() as f32,
//             y: json["rect"]["y"].as_f64().unwrap() as f32,
//             width: json["rect"]["width"].as_f64().unwrap() as f32,
//             height: json["rect"]["height"].as_f64().unwrap() as f32,
//         },
//         pivot: Pivot::new(0.5, 0.5),
//     }
// }
//
// fn create_temp_dir() -> PathBuf {
//     let rand: String = rand::thread_rng()
//         .sample_iter(&Alphanumeric)
//         .take(8)
//         .map(char::from)
//         .collect();
//     let crate_dir = env!("CARGO_MANIFEST_DIR");
//     let tmp_dir = PathBuf::from_str(&format!("{crate_dir}/tmp-{rand}")).unwrap();
//     _ = fs::remove_dir_all(&tmp_dir);
//     fs::create_dir(&tmp_dir).unwrap();
//     tmp_dir
// }
//
// fn get_fixture_dir(fixture: &'static str) -> PathBuf {
//     let crate_dir = env!("CARGO_MANIFEST_DIR");
//     PathBuf::from_str(&format!("{crate_dir}/fixtures/{fixture}")).unwrap()
// }
