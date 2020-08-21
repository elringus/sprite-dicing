This project is a part of **Naninovel** — visual novel engine extension for Unity. [Check it out on the Asset Store!](https://u3d.as/1pg9)

## Installation

Use [UPM](https://docs.unity3d.com/Manual/upm-ui.html) to install the package via the following git URL: `https://github.com/Elringus/SpriteDicing.git#package` or download and import [SpriteDicing.unitypackage](https://github.com/Elringus/SpriteDicing/raw/master/SpriteDicing.unitypackage) manually.

![](https://i.gyazo.com/b54e9daa9a483d9bf7f74f0e94b2d38a.gif)

Minimum supported Unity version: 2019.3

## Description

Sprite Dicing is an editor extension for [Unity game engine](https://unity3d.com/) which allows to split up a set of large sprite textures into small chunks, discard identical ones, bake them into atlas textures and then seamlessly reconstruct the original sprites at runtime for render. 

This technique allows to significantly reduce build size, in cases when multiple textures with identical areas are used. Consider a [visual novel](https://en.wikipedia.org/wiki/Visual_novel) type of game, where you have multiple textures per character, each portraying a different emotion; most of the textures space will be occupied with the identical data, and only a small area will vary:

![](https://i.gyazo.com/af08d141e7a08b6a8e2ef60c07332bbf.png)

These original five textures have total size of **17.5MB**. After dicing, the resulting atlas texture will contain only the unique chunks, having the size of just **2.4MB**. We can now discard the original five textures and use the atlas to render the original sprites, effectively compressing source textures data by **86.3%**:

## How to use

1. Create a `DicedSpriteAtlas` asset using `Assets -> Create -> Diced Sprite Atals` menu command, select it;
2. Specify `Input Folder` — project directory, containing the source textures to process. You can simply drag-drop a folder from the project hierarchy window into the field;
3. Press `Build Atlas` button and wait for the generation procedure to finish;
4. Generated sprites will appear inside the atlas asset; select any of them and drop to the scene.

![](https://i.gyazo.com/faddf19580d8e6c9e0660d61976b2bef.gif)

## Atlas Generation Options

You can optionally configure atlas generation settings via the editor inspector window.

![](https://i.gyazo.com/d47adf4dab2cb358f226d1af9d3c5b4b.png)

| Option | Description
| --- | --- |
| Generated Data Size | Total amount of the generated sprites data (vertices, UVs and triangles). Reduce by increasing Dice Unit Size. |
| Default Pivot | Relative pivot point position in 0 to 1 range, counting from the bottom-left corner. Can be changed after build for each sprite individually. |
| Keep Original | Whether to preserve original sprites pivot (usable for animations). |
| Decouple Sprite Data | Whether to save sprite assets in a separate folder instead of adding them as childs of the atlas asset. |
| Atlas Size Limit | Maximum size of a single generated atlas texture; will generate multiple textures when the limit is reached. |
| Force Square | The generated atlas textures will always be square. Less efficient, but required for PVRTC compression. |
| Pixels Per Unit | How many pixels in the sprite correspond to the unit in the world. |
| Dice Unit Size | The size of a single diced unit. |
| Padding | The size of a pixel border to add between adjacent diced units inside atlas. Increase to prevent texture bleeding artifacts (usually appear as thin gaps between diced units). Larger values will consume more texture space, but yield better anti-bleeding results. Minimum value of 2 is recommended in most cases. When 2 is not enough to prevent bleeding, consider adding a bit of `UV Inset` before increasing the padding. |
| UV Inset | Relative inset of the diced units UV coordinates. Can be used in addition to (or instead of) `Padding` to prevent texture bleeding artifacts. Won't consume any texture space, but higher values could visually distort the final result. |
| Input Folder | Asset folder with source sprite textures. |
| Include Subfolders | Whether to recursively search for textures inside the input folder. |
| Prepend Names | Whether to prepend sprite names with the subfolder name; eg: `SubfolderName.SpriteName`. |

All the above descriptions are available as tooltips when hovering corresponding configuration options in the editor.

## Animation

It's possible to use diced sprites for animation. Make sure to enable `Keep Original Pivot` when generating the atlas to preserve the relative positions of the generated sprites. An example on animating diced sprites is available in the project in "Animation" scene.

![](https://i.gyazo.com/9df7af39368a7b17f067a03a50c41509.gif)
