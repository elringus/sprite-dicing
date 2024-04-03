use crate::img;
use crate::models::*;
use sprite_dicing::{DicedSprite, Prefs, Progress, SourceSprite, Texture};
use std::{fs, path::Path, path::PathBuf};

/// Packs all the textures of supported formats inside directory with specified path and
/// writes generated atlas textures and diced sprite meshes serialized in JSON.
///
/// # Arguments
///
/// * `dir`: Directory to look for textures to pack.
/// * `fs_prefs`: FS-related preferences: out directory, atlas format, etc.
/// * `prefs`: Dicing-related preferences: unit size, padding, PPU, etc.
///
/// returns: [Ok] when operation successful, [Error] otherwise.
pub fn dice_dir(dir: &Path, fs_prefs: &FsPrefs, prefs: &Prefs) -> Result<()> {
    let paths = collect_sources(dir, fs_prefs)?;
    let sources = load_sources(dir, &paths, prefs, fs_prefs)?;
    let diced = sprite_dicing::dice(&sources, prefs).map_err(Error::Dicing)?;
    let out_dir = fs_prefs.out.as_deref().unwrap_or(dir);
    write_atlases(diced.atlases, out_dir, &fs_prefs.atlas_format, prefs)?;
    write_sprites(diced.sprites, out_dir)
}

fn collect_sources(dir: &Path, prefs: &FsPrefs) -> Result<Vec<PathBuf>> {
    let mut sprites = vec![];
    for entry in fs::read_dir(dir)? {
        let path = entry?.path();
        if path.is_dir() && prefs.recursive {
            sprites.extend(collect_sources(&path, prefs)?);
        } else if path.is_file() && img::supported(&path) {
            sprites.push(path);
        }
    }
    Ok(sprites)
}

fn load_sources(
    root: &Path,
    paths: &[PathBuf],
    prefs: &Prefs,
    fs_prefs: &FsPrefs,
) -> Result<Vec<SourceSprite>> {
    let mut sprites = Vec::with_capacity(paths.len());
    for (idx, path) in paths.iter().enumerate() {
        Progress::report(prefs, 0, idx, paths.len(), "Loading source textures");
        sprites.push(create_sprite(root, path, fs_prefs)?);
    }
    Ok(sprites)
}

fn create_sprite(root: &Path, path: &Path, prefs: &FsPrefs) -> Result<SourceSprite> {
    let id = eval_sprite_id(root, path, &prefs.separator);
    let texture = img::to_texture(&img::load(path)?);
    let pivot = None;
    Ok(SourceSprite { id, texture, pivot })
}

fn eval_sprite_id(root: &Path, path: &Path, separator: &str) -> String {
    path.with_extension("")
        .iter()
        .skip(root.iter().count())
        .map(|o| o.to_str().unwrap_or(""))
        .collect::<Vec<_>>()
        .join(separator)
}

fn write_atlases(tex: Vec<Texture>, dir: &Path, fmt: &AtlasFormat, prefs: &Prefs) -> Result<()> {
    let total = tex.len();
    tex.into_iter().enumerate().try_for_each(|(idx, tex)| {
        Progress::report(prefs, 4, idx, total, "Writing atlas textures");
        let name = format!("atlas_{idx}.{}", fmt.extension());
        write_atlas(&dir.join(name), tex)
    })
}

fn write_atlas(path: &Path, tex: Texture) -> Result<()> {
    let img = img::from_texture(tex)?;
    img::save(path, img)
}

fn write_sprites(sprites: Vec<DicedSprite>, dir: &Path) -> Result<()> {
    let json = crate::json::sprites_to_json(&sprites);
    let path = dir.join("sprites.json");
    fs::write(path, json).map_err(Error::Io)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn evaluates_sprite_id_from_path() {
        assert_eq!(
            eval_sprite_id(Path::new("/foo/bar"), Path::new("/foo/bar/img.png"), "/"),
            "img"
        );
        assert_eq!(
            eval_sprite_id(Path::new("/foo"), Path::new("/foo/bar/img.png"), "/"),
            "bar/img"
        );
        assert_eq!(
            eval_sprite_id(Path::new("/"), Path::new("/foo/bar/img.png"), "/"),
            "foo/bar/img"
        );
    }
}
