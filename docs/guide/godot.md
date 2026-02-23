# Godot

SpriteDicing has a [Godot](https://godotengine.org/) integration with a full-fledged atlas editor. It communicates with the Rust libraries via C ABI and outputs sprite resources which can be rendered with a dedicated `DicedSprite2D` node.

![](https://i.gyazo.com/93aaa8ed9a9aab15841c6faaf4516fa1.png)

The plugin bundles pre-built binaries for Windows x64, Mac ARM and Linux x64. All the code is editor-only, so this covers all the Godot editor-supported platforms.

Minimum supported Godot version: 4.6.

## Installation

...

Alternatively, clone the repository and install the [plugin](https://github.com/elringus/sprite-dicing/tree/main/plugins/godot/addons/sprite_dicing) by moving it under the project's `addons` folder.

## Usage

1. Create a new **DicedSpriteAtlas** resource ![](https://i.gyazo.com/257dfb9734d26ff55b882eb3a6fd3e3d.png)
2. Select a folder containing source sprite textures and click **Build Atlas** ![](https://i.gyazo.com/f1ab20f8caff059ec475f73707d350e5.png)
3. Add a **DicedSprite2D** node to your scene ![](https://i.gyazo.com/82325449a7e9021577f7def108028e29.png)
4. Set the **Atlas** and **Sprite ID** properties ![](https://i.gyazo.com/fe39ca501b88f4b7e769e6be93b73419.png)

## Atlas Generation Options

You can optionally configure atlas generation settings in the inspector.

![](https://i.gyazo.com/7fc948fe098f8cd0b6909200162404d1.png)

| Option | Description
| --- | --- |
| Trim Transparent | Whether to discard fully-transparent diced units on the generated mesh. Disable to preserve original texture dimensions (usable for animations). |
| Default Pivot | Relative pivot point position in 0 to 1 range, counting from the bottom-left corner. Can be changed after build for each sprite individually. |
| Keep Original | Whether to use pivot set on source textures (if any) instead of default. |
| Atlas Size Limit | Maximum size of a single generated atlas texture; will generate multiple textures when the limit is reached. |
| Square | The generated atlas textures will always be square. Less efficient, but required for PVRTC compression. |
| POT | The generated atlas textures will always have width and height of power of two. Extremely inefficient, but may be required by some older GPUs. |
| Pixels Per Unit | How many pixels in the sprite correspond to the unit in the world. |
| Dice Unit Size | The size of a single diced unit. |
| Padding | The size of a pixel border to add between adjacent diced units inside atlas. Increase to prevent texture bleeding artifacts (usually appear as thin gaps between diced units). Larger values will consume more texture space, but yield better anti-bleeding results. Minimum value of 2 is recommended in most cases. When 2 is not enough to prevent bleeding, consider adding a bit of `UV Inset` before increasing the padding. |
| UV Inset | Relative inset of the diced units UV coordinates. Can be used in addition to (or instead of) `Padding` to prevent texture bleeding artifacts. Won't consume any texture space, but higher values could visually distort the final result. |
| Input Folder | Asset folder with source sprite textures. |
| Include Subfolders | Whether to recursively search for textures inside the input folder. |
| Separator | When sprite is from a sub-folder(s), the separator will be used to join the folder name(s) and the sprite name. |

All the above descriptions are available as tooltips when hovering corresponding configuration options in the editor.

## Compression Ratio

When inspecting atlas asset, notice `Compression Ratio` line; it shows the ratio between source textures size and generated data (atlas textures + sprite meshes).

![](https://i.gyazo.com/80575364742301fbf5902d91972f2d4f.png)

When around to 1.0 or lower, the generated data size is close to or even larger than the source. You should either change atlas generation configuration (eg, increase dice unit size) or not use the solution at all.

## Export Mode

When exporting the project, make sure to **not include the source textures**. Otherwise, your build will contain both the original textures and their diced counterparts baked into the atlas textures.

Use the **Export Mode** to control which resources are exported. The simplest way to exclude the source textures is by changing the mode to "Export selected scenes" — this will make Godot only include the resources actually used in the selected scenes.

![](https://i.gyazo.com/488b2fe92ecd692708f76b66663e45f8.png)

## API

### DicedSpriteAtlas

```gdscript
# Get a sprite resource by identifer
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

## Sample Assets

- [Godette by tqqq](https://tqqq.itch.io/godette-3-animations-with-psd-files) (CC-BY 3.0)
