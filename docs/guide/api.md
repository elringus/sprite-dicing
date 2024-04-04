# API

Rust API is fairly simple: collect the source sprites to dice and pass them to `sprite_dicing::dice()` free function to perform the operation.

```rust
use sprite_dicing::{Prefs, SourceSprite, Texture, Pixel};

// Fake function to load textures (images).
fn load (path: &str) -> Texture {
    let red = Pixel::new(255, 0, 0, 255);
    let blue = Pixel::new(0, 0, 255, 255);
    Texture { width: 2, height: 2, pixels: vec![red, blue, blue, red] }
}

// Fake function to save textures.
fn save (path: &str, tex: &Texture) { }

// Collect source sprites to dice.
let sprites = vec![
    SourceSprite { id: "1", texture: load("1.png"), pivot: None },
    SourceSprite { id: "2", texture: load("2.png"), pivot: None },
    // ...
];

// Dice source sprites with default preferences.
let diced = sprite_dicing::dice(&sprites, &Prefs::default()).unwrap();

// Write generated atlas textures to file system.
for (index, atlas) in diced.atlases.iter().enumerate() {
    save(&format!("atlas_{index}.png"), atlas);
}

// Build diced sprites using generated data.
for sprite in diced.sprites {
    // Unique ID as set in the associated source sprite.
    _ = sprite.id;
    // Atlas texture containing all the unique pixels for the sprite.
    _ = &diced.atlases[sprite.atlas_index];
    // Mesh vertex positions in local space units.
    _ = sprite.vertices;
    // Atlas texture coordinates mapped to the mesh vertices.
    _ = sprite.uvs;
    // Mesh quad faces as indices to the vertices array.
    _ = sprite.indices;
    // Sprite rect in local space units.
    _ = sprite.rect;
    // ... (actual sprite asset building process is engine-specific)
}
```
