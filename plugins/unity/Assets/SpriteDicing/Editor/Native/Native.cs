using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpriteDicing
{
    /// <summary>
    /// Managed wrapper over native dicing library.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static unsafe class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct SourceSprite
        {
            public string Id { get; init; }
            public Texture Texture { get; init; }
            public Pivot? Pivot { get; init; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Texture
        {
            public uint Width { get; init; }
            public uint Height { get; init; }
            public IReadOnlyList<Pixel> Pixels { get; init; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Pixel : IEquatable<Pixel>
        {
            public readonly byte R;
            public readonly byte G;
            public readonly byte B;
            public readonly byte A;

            public Pixel (byte r, byte g, byte b, byte a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }

            public bool Equals (Pixel other) => R == other.R && G == other.G && B == other.B && A == other.A;
            public override bool Equals (object obj) => obj is Pixel other && Equals(other);
            public override int GetHashCode () => HashCode.Combine(R, G, B, A);
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Prefs
        {
            public uint UnitSize { get; init; }
            public uint Padding { get; init; }
            public float UVInset { get; init; }
            public bool TrimTransparent { get; init; }
            public uint AtlasSizeLimit { get; init; }
            public bool AtlasSquare { get; init; }
            public bool AtlasPOT { get; init; }
            public float PPU { get; init; }
            public Pivot Pivot { get; init; }
            public ProgressCallback OnProgress { get; init; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Artifacts
        {
            public IReadOnlyList<Texture> Atlases { get; init; }
            public IReadOnlyList<DicedSprite> Sprites { get; init; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct DicedSprite
        {
            public string Id { get; init; }
            public int Atlas { get; init; }
            public IReadOnlyList<Vertex> Vertices { get; init; }
            public IReadOnlyList<UV> UVs { get; init; }
            public IReadOnlyList<int> Indices { get; init; }
            public Rect Rect { get; init; }
            public Pivot Pivot { get; init; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Vertex
        {
            public readonly float X;
            public readonly float Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct UV
        {
            public readonly float U;
            public readonly float V;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Pivot
        {
            public readonly float X;
            public readonly float Y;

            public Pivot (float x, float y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Rect
        {
            public readonly float X;
            public readonly float Y;
            public readonly float Width;
            public readonly float Height;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Progress
        {
            public float Ratio { get; init; }
            public string Activity { get; init; }
        }

        public delegate void ProgressCallback (Progress progress);

        [StructLayout(LayoutKind.Sequential)]
        private struct CTexture
        {
            public uint width;
            public uint height;
            public Pixel* pixels;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CSourceSprite
        {
            public IntPtr id;
            public CTexture texture;
            [MarshalAs(UnmanagedType.I1)]
            public bool has_pivot;
            public Pivot pivot;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CProgress
        {
            public float ratio;
            public IntPtr activity;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CProgressCallback (CProgress progress, IntPtr userData);

        [StructLayout(LayoutKind.Sequential)]
        private struct CPrefs
        {
            public uint unit_size;
            public uint padding;
            public float uv_inset;
            [MarshalAs(UnmanagedType.I1)]
            public bool trim_transparent;
            public uint atlas_size_limit;
            [MarshalAs(UnmanagedType.I1)]
            public bool atlas_square;
            [MarshalAs(UnmanagedType.I1)]
            public bool atlas_pot;
            public float ppu;
            public Pivot pivot;
            public CProgressCallback on_progress;
            public IntPtr user_data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CDicedSprite
        {
            public IntPtr id;
            public nuint atlas_index;
            public Vertex* vertices;
            public UV* uvs;
            public nuint vertex_count;
            public uint* indices;
            public nuint index_count;
            public Rect rect;
            public Pivot pivot;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CResult
        {
            public IntPtr error;
            public CTexture* atlases;
            public nuint atlas_count;
            public CDicedSprite* sprites;
            public nuint sprite_count;
        }

        public static Artifacts Dice (IEnumerable<SourceSprite> sprites, Prefs prefs)
        {
            var result = DiceNative(sprites, prefs);
            try
            {
                var error = Marshal.PtrToStringUTF8(result.error);
                if (error != null) throw new(error);
                return new() {
                    Atlases = MarshalArray(result.atlases, result.atlas_count, MarshalTexture),
                    Sprites = MarshalArray(result.sprites, result.sprite_count, MarshalDicedSprite)
                };
            }
            finally { sd_free(result); }
        }

        [DllImport("sprite_dicing", CallingConvention = CallingConvention.Cdecl)]
        private static extern CResult sd_dice (CSourceSprite* sprites, nuint count, in CPrefs prefs);

        [DllImport("sprite_dicing", CallingConvention = CallingConvention.Cdecl)]
        private static extern void sd_free (CResult result);

        private static CResult DiceNative (IEnumerable<SourceSprite> sprites, Prefs prefs)
        {
            var pins = new List<GCHandle>();
            var strings = new List<IntPtr>();
            CProgressCallback onProgress = prefs.OnProgress is { } callback
                ? (progress, _) => callback(MarshalProgress(progress))
                : null;
            try
            {
                var cSprites = sprites.Select(s => MarshalSourceSprite(s, pins, strings)).ToArray();
                var cPrefs = MarshalPrefs(prefs, onProgress);
                fixed (CSourceSprite* ptr = cSprites)
                    return sd_dice(ptr, (nuint)cSprites.Length, in cPrefs);
            }
            finally
            {
                GC.KeepAlive(onProgress);
                pins.ForEach(pin => pin.Free());
                strings.ForEach(Marshal.FreeCoTaskMem);
            }
        }

        private static CPrefs MarshalPrefs (Prefs prefs, CProgressCallback onProgress) => new() {
            unit_size = prefs.UnitSize,
            padding = prefs.Padding,
            uv_inset = prefs.UVInset,
            trim_transparent = prefs.TrimTransparent,
            atlas_size_limit = prefs.AtlasSizeLimit,
            atlas_square = prefs.AtlasSquare,
            atlas_pot = prefs.AtlasPOT,
            ppu = prefs.PPU,
            pivot = prefs.Pivot,
            on_progress = onProgress
        };

        private static CSourceSprite MarshalSourceSprite (SourceSprite src, List<GCHandle> pins, List<IntPtr> strings)
        {
            var id = Marshal.StringToCoTaskMemUTF8(src.Id);
            strings.Add(id);
            return new() {
                id = id,
                texture = MarshalTexture(src.Texture, pins),
                has_pivot = src.Pivot.HasValue,
                pivot = src.Pivot.GetValueOrDefault()
            };
        }

        private static CTexture MarshalTexture (Texture texture, List<GCHandle> pins)
        {
            var pixels = texture.Pixels as Pixel[] ?? texture.Pixels.ToArray();
            var pin = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            pins.Add(pin);
            return new() {
                width = texture.Width,
                height = texture.Height,
                pixels = (Pixel*)pin.AddrOfPinnedObject()
            };
        }

        private static Texture MarshalTexture (CTexture texture) => new() {
            Width = texture.width,
            Height = texture.height,
            Pixels = MarshalArray(texture.pixels, texture.width * texture.height)
        };

        private static DicedSprite MarshalDicedSprite (CDicedSprite sprite) => new() {
            Id = Marshal.PtrToStringUTF8(sprite.id),
            Atlas = (int)sprite.atlas_index,
            Vertices = MarshalArray(sprite.vertices, sprite.vertex_count),
            UVs = MarshalArray(sprite.uvs, sprite.vertex_count),
            Indices = MarshalArray((int*)sprite.indices, sprite.index_count),
            Rect = sprite.rect,
            Pivot = sprite.pivot
        };

        private static Progress MarshalProgress (CProgress progress) => new() {
            Ratio = progress.ratio,
            Activity = Marshal.PtrToStringUTF8(progress.activity)
        };

        private static T[] MarshalArray<T> (T* array, nuint count) where T : unmanaged
        {
            return new ReadOnlySpan<T>(array, (int)count).ToArray();
        }

        private static T[] MarshalArray<TNative, T> (TNative* array, nuint count, Func<TNative, T> marshal)
            where TNative : unmanaged
        {
            var result = new T[(int)count];
            for (var i = 0; i < result.Length; i++)
                result[i] = marshal(array[i]);
            return result;
        }
    }
}
