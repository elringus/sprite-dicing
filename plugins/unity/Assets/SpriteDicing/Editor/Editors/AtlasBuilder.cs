using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using static SpriteDicing.Editors.EditorProperties;

namespace SpriteDicing.Editors
{
    public class AtlasBuilder
    {
        private readonly SerializedObject serializedObject;
        private readonly DicedSpriteAtlas target;
        private readonly string atlasPath;

        private double? buildStartTime;

        public AtlasBuilder (SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            target = serializedObject.targetObject as DicedSpriteAtlas;
            atlasPath = AssetDatabase.GetAssetPath(target);
        }

        public void Build ()
        {
            try
            {
                // BuildSharp();
                BuildRust();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to build diced sprite atlas. {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                GUIUtility.ExitGUI();
            }
        }

        private void BuildSharp ()
        {
            var sourceTextures = CollectSourceTextures();
            var dicedTextures = DiceTextures(sourceTextures);
            var atlasTextures = PackTextures(dicedTextures);
            BuildDicedSprites(atlasTextures);
            UpdateCompressionRatio(sourceTextures.Select(t => t.Texture), atlasTextures.Select(t => t.Texture));
        }

        private static readonly MethodInfo createSpriteMethod =
            typeof(Sprite).GetMethod("CreateSprite", BindingFlags.NonPublic | BindingFlags.Static);

        private void BuildRust ()
        {
            var sourceTextures = CollectSourceTextures();
            var atlasName = Path.GetFileNameWithoutExtension(atlasPath);
            var inDir = Path.GetFullPath(AssetDatabase.GetAssetPath(InputFolder));
            var outDir = Path.GetFullPath(Path.GetDirectoryName(atlasPath));

            DisplayProgressBar("Dicing sources...", 1);

            var cBytes = new List<GCHandle>();
            var cSprites = Pin(sourceTextures.Select(s => {
                var path = AssetDatabase.GetAssetPath(s.Texture);
                var bytes = Pin(File.ReadAllBytes(path));
                cBytes.Add(bytes);
                return new CSprite {
                    id = Marshal.StringToHGlobalAnsi(s.Name),
                    bytes = new CSlice {
                        ptr = bytes.AddrOfPinnedObject(),
                        len = (ulong)((byte[])bytes.Target).Length
                    },
                    extension = Marshal.StringToHGlobalAnsi(Path.GetExtension(path).Substring(1)),
                    has_pivot = s.Pivot.HasValue,
                    pivot_x = s.Pivot.GetValueOrDefault().x,
                    pivot_y = s.Pivot.GetValueOrDefault().y
                };
            }).ToArray());
            var cSliceOfSprites = new CSlice {
                ptr = cSprites.AddrOfPinnedObject(),
                len = (ulong)((CSprite[])cSprites.Target).Length
            };
            var cPrefs = new CPrefs {
                unit_size = (uint)UnitSize,
                padding = (uint)Padding,
                uv_inset = UVInset,
                trim_transparent = TrimTransparent,
                atlas_size_limit = (uint)AtlasSizeLimit,
                atlas_square = ForceSquare,
                atlas_pot = ForcePot,
                pivot_x = DefaultPivot.x,
                pivot_y = DefaultPivot.y,
                ppu = PPU,
                atlas_format = 0
            };

            var artifacts = dice(cSliceOfSprites, cPrefs);
            // cBytes.ForEach(c => c.Free());
            // cSprites.Free();

            DisplayProgressBar("Writing atlases...", 1);

            Debug.Log($"artifacts.atlases {{ Ptr: {artifacts.atlases.ptr} Len: {artifacts.atlases.len} }}");
            var atlasSlices = ToManagedStructs<CSlice>(artifacts.atlases.ptr, artifacts.atlases.len);
            foreach (var slice in atlasSlices)
                Debug.Log($"AtlasSlice (bytes) {{ Ptr: {slice.ptr} Len: {slice.len / 1000000f:F1} }}");
            var atlasBytes = Array.Empty<byte[]>(); //atlasSlices.Select(s => ToManagedBytes(s.ptr, s.len)).ToArray();
            var atlasPaths = new string[atlasBytes.Length];
            for (var i = 0; i < atlasBytes.Length; i++)
            {
                var path = Path.Combine(outDir, $"{atlasName} 00{i + 1}.png");
                File.WriteAllBytes(path, atlasBytes[i]);
                atlasPaths[i] = Path.Combine(Path.GetDirectoryName(atlasPath), Path.GetFileName(path));
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            DisplayProgressBar("Building sprites...", 1);

            // TODO: Marshal.PtrToStringUTF8
            var json = Marshal.PtrToStringAnsi(artifacts.sprites);
            var diced = JsonUtility.FromJson<DicedSprites>($"{{ \"sprites\": {json} }}");
            var sprites = diced.sprites.Select(BuildSprite);
            new DicedSpriteSerializer(serializedObject).Serialize(sprites);
            File.Delete(Path.Combine(outDir, "sprites.json"));
            File.Delete(Path.Combine(outDir, "sprites.json.meta"));
            AssetDatabase.Refresh();

            var atlasTextures = atlasPaths.Select(AssetDatabase.LoadAssetAtPath<Texture2D>);
            UpdateCompressionRatio(sourceTextures.Select(t => t.Texture), atlasTextures);

            Sprite BuildSprite (DicedSprite data)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPaths[data.atlas]);
                var rect = new UnityEngine.Rect(data.rect.x * PPU, data.rect.y * PPU, data.rect.width * PPU, data.rect.height * PPU);
                var pivot = DefaultPivot /* or per-sprite */;
                var args = new object[] { texture, rect, pivot, PPU, (uint)0, SpriteMeshType.Tight, Vector4.zero, false };
                var sprite = (Sprite)createSpriteMethod.Invoke(null, args);
                var vertices = data.vertices.Select(v => new Vector3(v.x, data.rect.height * (1 - pivot.y * 2) - v.y)).ToArray();
                var uvs = data.uvs.Select(v => new Vector2(v.u, 1 - v.v)).ToArray();
                var triangles = data.indices.Select(i => (ushort)i).ToArray();
                sprite.name = data.id;
                sprite.SetVertexCount(vertices.Length);
                sprite.SetIndices(new NativeArray<ushort>(triangles, Allocator.Temp));
                sprite.SetVertexAttribute(VertexAttribute.Position, new NativeArray<Vector3>(vertices, Allocator.Temp));
                sprite.SetVertexAttribute(VertexAttribute.TexCoord0, new NativeArray<Vector2>(uvs, Allocator.Temp));
                return sprite;
            }
        }

        private static T[] ToManagedStructs<T> (IntPtr ptr, ulong length)
        {
            return new[] { Marshal.PtrToStructure<T>(ptr) };

            // var size = Marshal.SizeOf(typeof(T));
            // Debug.Log(size);
            // var structs = new T[length];
            //
            // for (long i = 0; i < (long)length; i++)
            // {
            //     var ins = new IntPtr(ptr.ToInt64() + i * size);
            //     structs[i] = Marshal.PtrToStructure<T>(ins);
            // }
            //
            // return structs;
        }

        private static byte[] ToManagedBytes (IntPtr ptr, int length)
        {
            var array = new byte[length];
            Marshal.Copy(ptr, array, 0, length);
            return array;
        }

        private static GCHandle Pin (object obj)
        {
            return GCHandle.Alloc(obj, GCHandleType.Pinned);
        }

        [DllImport("sprite_dicing")]
        public static extern CArtifacts dice (CSlice sprites, CPrefs prefs);

        [StructLayout(LayoutKind.Sequential)]
        public struct CSprite
        {
            public IntPtr id;
            public CSlice bytes;
            public IntPtr extension;
            public bool has_pivot;
            public float pivot_x;
            public float pivot_y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CPrefs
        {
            public uint unit_size;
            public uint padding;
            public float uv_inset;
            public bool trim_transparent;
            public uint atlas_size_limit;
            public float pivot_x;
            public float pivot_y;
            public float ppu;
            public byte atlas_format;
            public bool atlas_square;
            public bool atlas_pot;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CArtifacts
        {
            public CSlice atlases;
            public IntPtr sprites;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CSlice
        {
            public IntPtr ptr;
            public ulong len;
        }

        [Serializable]
        public struct DicedSprites
        {
            public DicedSprite[] sprites;
        }

        [Serializable]
        public struct DicedSprite
        {
            public string id;
            public int atlas;
            public Vertex[] vertices;
            public UV[] uvs;
            public int[] indices;
            public Rect rect;
        }

        [Serializable]
        public struct Vertex
        {
            public float x;
            public float y;
        }

        [Serializable]
        public struct UV
        {
            public float u;
            public float v;
        }

        [Serializable]
        public struct Rect
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }

        private SourceTexture[] CollectSourceTextures ()
        {
            DisplayProgressBar("Collecting source textures...", .0f);
            var inputFolderPath = AssetDatabase.GetAssetPath(InputFolder);
            var texturePaths = TextureFinder.FindAt(inputFolderPath, IncludeSubfolders);
            var loader = new TextureLoader(inputFolderPath);
            return texturePaths.Select(loader.Load).ToArray();
        }

        private List<DicedTexture> DiceTextures (IReadOnlyList<SourceTexture> sourceTextures)
        {
            var dicer = new TextureDicer(UnitSize, Padding, TrimTransparent);
            var dicedTextures = new List<DicedTexture>();
            for (int i = 0; i < sourceTextures.Count; i++)
            {
                DisplayProgressBar("Dicing textures...", .5f * i / sourceTextures.Count);
                dicedTextures.Add(dicer.Dice(sourceTextures[i]));
            }
            return dicedTextures;
        }

        private List<AtlasTexture> PackTextures (IReadOnlyList<DicedTexture> dicedTextures)
        {
            DisplayProgressBar("Packing dices...", .5f);
            var textureSettings = GetExistingAtlasTextureSettings();
            DeleteExistingAtlasTextures();
            var basePath = atlasPath.Substring(0, atlasPath.LastIndexOf(".", StringComparison.Ordinal));
            var serializer = new TextureSerializer(basePath, textureSettings);
            var packer = new TexturePacker(serializer, UVInset, ForceSquare, ForcePot, AtlasSizeLimit, UnitSize, Padding);
            var atlasTextures = packer.Pack(dicedTextures);
            SaveAtlasTextures(atlasTextures);
            return atlasTextures;

            TextureSettings GetExistingAtlasTextureSettings ()
            {
                var settings = new TextureSettings();
                if (TexturesProperty.arraySize <= 0) return settings;
                var texture = TexturesProperty.GetArrayElementAtIndex(0).objectReferenceValue as Texture;
                if (texture) settings.TryImportExisting(texture);
                return settings;
            }

            void DeleteExistingAtlasTextures ()
            {
                for (int i = TexturesProperty.arraySize - 1; i >= 0; i--)
                {
                    var texture = TexturesProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (!texture) continue;
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(texture));
                    UnityEngine.Object.DestroyImmediate(texture, true);
                }
                TexturesProperty.arraySize = 0;
            }

            void SaveAtlasTextures (IReadOnlyList<AtlasTexture> textures)
            {
                TexturesProperty.arraySize = textures.Count;
                for (int i = 0; i < textures.Count; i++)
                    TexturesProperty.GetArrayElementAtIndex(i).objectReferenceValue = textures[i].Texture;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void BuildDicedSprites (IReadOnlyCollection<AtlasTexture> atlasTextures)
        {
            var sprites = new List<Sprite>();
            var builder = new SpriteBuilder(PPU, DefaultPivot, KeepOriginalPivot);
            float total = atlasTextures.Sum(a => a.DicedTextures.Count), built = 0;
            foreach (var atlasTexture in atlasTextures)
            foreach (var dicedTexture in atlasTexture.DicedTextures)
            {
                DisplayProgressBar("Building diced sprites...", .5f + .5f * ++built / total);
                sprites.Add(builder.Build(atlasTexture, dicedTexture));
            }
            new DicedSpriteSerializer(serializedObject).Serialize(sprites);
        }

        private void UpdateCompressionRatio (IEnumerable<Texture2D> sourceTextures, IEnumerable<Texture2D> atlasTextures)
        {
            var sourceSize = sourceTextures.Sum(GetAssetSize);
            var atlasSize = atlasTextures.Sum(GetAssetSize);
            var dataSize = GetDataSize();
            var ratio = sourceSize / (float)(atlasSize + dataSize);
            var color = ratio > 2 ? EditorGUIUtility.isProSkin ? "lime" : "green" : ratio > 1 ? "yellow" : "red";
            LastRatioValueProperty.stringValue = $"{sourceSize} KB / ({atlasSize} KB + {dataSize} KB) = <color={color}>{ratio:F2}</color>";
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            long GetDataSize ()
            {
                var size = GetAssetSize(target);
                if (DecoupleSpriteData)
                    for (int i = SpritesProperty.arraySize - 1; i >= 0; i--)
                        size += GetAssetSize(SpritesProperty.GetArrayElementAtIndex(i).objectReferenceValue);
                return size / (EditorSettings.serializationMode == SerializationMode.ForceText ? 2 : 1);
            }

            long GetAssetSize (UnityEngine.Object asset)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);
                if (!File.Exists(assetPath)) return 0;
                return new FileInfo(assetPath).Length / 1024;
            }
        }

        private void DisplayProgressBar (string activity, float progress)
        {
            if (!buildStartTime.HasValue) buildStartTime = EditorApplication.timeSinceStartup;
            var elapsed = TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - buildStartTime.Value);
            var title = $"Building Diced Atlas ({elapsed:mm\\:ss})";
            if (EditorUtility.DisplayCancelableProgressBar(title, activity, progress))
                throw new OperationCanceledException("Diced sprite atlas building was canceled by the user.");
        }
    }
}
