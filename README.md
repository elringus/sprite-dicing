This project is a part of **Naninovel** — visual novel engine extension for Unity. [Check it out on the Asset Store!](https://u3d.as/1pg9)

## Installation

Use [UPM](https://docs.unity3d.com/Manual/upm-ui.html) to install the package via the following git URL: `https://github.com/Elringus/SpriteDicing.git#package` or download and import [SpriteDicing.unitypackage](https://github.com/Elringus/SpriteDicing/raw/master/SpriteDicing.unitypackage) manually.

![](https://i.gyazo.com/b54e9daa9a483d9bf7f74f0e94b2d38a.gif)

## Description

Sprite Dicing is an editor extension for [Unity game engine](https://unity3d.com/) which allows to split up a set of large sprite textures into small chunks, discard identical ones, bake them into atlas textures and then seamlessly reconstruct the original sprites at runtime for render. 

This technique allows to significantly reduce build size, in cases when multiple textures with identical areas are used. Consider a [visual novel](https://en.wikipedia.org/wiki/Visual_novel) type of game, where you have multiple textures per character, each portraying a different emotion; most of the textures space will be occupied with the identical data, and only a small area will vary:

![](https://i.gyazo.com/af08d141e7a08b6a8e2ef60c07332bbf.png)

These original five textures have total size of **17.5MB**. After dicing, the resulting atlas texture will contain only the unique chunks, having the size of just **2.4MB**. We can now discard the original five textures and use the atlas to render the original sprites, effectively compressing source textures data by **86.3%**:

![](https://i.gyazo.com/7f79936fc714abcc342ae348478b9c8e.gif)

## How to use

1. Create a `DicedSpriteAtlas` asset using `Assets -> Create -> Diced Sprite Atals` menu command, select it;
2. Specify `Input Folder` — project directory, containing the source textures you wish to process. You can simply drag-drop a folder from the project hierarchy window into the field;
3. Press `Build Atlas` button and wait for the generation procedure to finish;
4. Sub-assets will appear inside the atlas asset, representing the original sprites; select any of them and drop to the scene.

You can optionally configure the atlas generation settings via the inspector editor window. Consult the tooltips for information on avilable options.

![](https://i.gyazo.com/1453dba6e6923db7e314fad16198dd3c.png)
