# SpriteDicing for Godot

A tool for lossless compression of sprites with identical regions.

## Structure

```
addons/sprite_dicing/
├── runtime/           # Pure GDScript, no native dependencies
│   ├── diced_sprite.gd
│   ├── diced_sprite_atlas.gd
│   └── diced_sprite_2d_type.gd
├── editor/            # Editor-only code
│   ├── sprite_dicing.gd    # Plugin
│   ├── native.gd           # GDExtension wrapper
│   ├── atlas_builder.gd    # Build orchestration
│   └── native/             # C++ GDExtension
│       ├── src/
│       │   ├── sprite_dicing.h
│       │   └── sprite_dicing.cpp
│       ├── bin/            # Native Rust libraries
│       ├── sprite_dicing.gdextension
│       └── SConstruct
├── plugin.cfg
└── README.md
```

## Building the GDExtension

### Prerequisites

1. **Python 3.6+** with SCons installed:
   ```bash
   pip install scons
   ```

2. **C++ Compiler**:
   - Windows: Visual Studio 2019+ with C++ workload
   - macOS: Xcode Command Line Tools
   - Linux: GCC or Clang

### Build Steps

1. Clone godot-cpp into the native directory:
   ```bash
   cd godot/addons/sprite_dicing/editor/native
   git clone https://github.com/godotengine/godot-cpp
   cd godot-cpp
   git checkout 4.6  # Match your Godot version
   git submodule update --init --recursive
   ```

2. Build the GDExtension:
   ```bash
   cd ..  # Back to native directory
   scons
   ```

3. The compiled library will be in `bin/`.

## Usage

1. Create a new **DicedSpriteAtlas** resource (right-click in FileSystem → New Resource → DicedSpriteAtlas)
2. Configure the atlas:
   - **Input Folder**: Select a folder containing source sprite textures
   - **Include Subfolders**: Whether to search recursively
   - **Separator**: Character to join folder names in sprite IDs
   - **Dice Unit Size**: Size of diced chunks (8, 16, 32, 64, 128, 256)
   - **Padding**: Pixel border between diced units
   - **Atlas Size Limit**: Maximum atlas texture size
   - **Trim Transparent**: Remove transparent mesh areas
   - **Pixels Per Unit**: World space scale
   - **Default Pivot**: Origin point (0-1 normalized)
3. Click **Build Atlas** to generate diced sprites

## Using Diced Sprites

Add a **DicedSprite2D** node to your scene:
- Set the **Atlas** property to your DicedSpriteAtlas resource
- Set the **Sprite ID** to the identifier of the sprite you want to display
- Optionally set **Modulate Color** to tint the sprite

## API

### DicedSpriteAtlas

```gdscript
# Get a sprite by ID
var sprite = atlas.get_sprite("sprite_id")

# Collect all sprite identifiers
var ids := PackedStringArray()
atlas.collect_sprite_ids(ids)
```

### DicedSprite2D

```gdscript
# Change the displayed sprite
sprite_node.sprite_id = "new_sprite"

# Change the atlas
sprite_node.atlas = new_atlas
```
