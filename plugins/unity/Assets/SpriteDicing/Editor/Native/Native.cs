using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.TestTools;

namespace SpriteDicing
{
    /// <summary>
    /// Managed wrapper over native dicing library.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static unsafe class Native
    {
        public readonly struct SourceSprite
        {
            public string Id { get; init; }
            public Texture Texture { get; init; }
            public Pivot? Pivot { get; init; }
        }

        public readonly struct Texture
        {
            public uint Width { get; init; }
            public uint Height { get; init; }
            public IReadOnlyList<Pixel> Pixels { get; init; }
        }

        public readonly struct Pixel : IEquatable<Pixel>
        {
            public byte R { get; init; }
            public byte G { get; init; }
            public byte B { get; init; }
            public byte A { get; init; }

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

        public class Artifacts : IDisposable
        {
            public IReadOnlyList<Texture> Atlases { get; }
            public IReadOnlyList<DicedSprite> Sprites { get; }

            private readonly List<IntPtr> pts;

            internal Artifacts (Texture[] atlases, DicedSprite[] sprites, List<IntPtr> pts)
            {
                Atlases = atlases;
                Sprites = sprites;
                this.pts = pts;
            }

            [ExcludeFromCoverage]
            public void Dispose ()
            {
                foreach (var ptr in pts)
                    Marshal.FreeHGlobal(ptr);
            }
        }

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

        public readonly struct Vertex
        {
            public float X { get; init; }
            public float Y { get; init; }
        }

        public readonly struct UV
        {
            public float U { get; init; }
            public float V { get; init; }
        }

        public readonly struct Pivot
        {
            public float X { get; init; }
            public float Y { get; init; }
        }

        public readonly struct Rect
        {
            public float X { get; init; }
            public float Y { get; init; }
            public float Width { get; init; }
            public float Height { get; init; }
        }

        public readonly struct Progress
        {
            public float Ratio { get; init; }
            public string Activity { get; init; }
        }

        public delegate void ProgressCallback (Progress progress);

        [StructLayout(LayoutKind.Sequential)]
        private struct CSourceSprite
        {
            public IntPtr id;
            public CTexture texture;
            [MarshalAs(UnmanagedType.I1)]
            public bool has_pivot;
            public CPivot pivot;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CTexture
        {
            public uint width;
            public uint height;
            public CSlice pixels;
        }

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
            public CPivot pivot;
            [MarshalAs(UnmanagedType.I1)]
            public bool has_progress_callback;
            public CProgressCallback progress_callback;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CArtifacts
        {
            public CSlice atlases;
            public CSlice sprites;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CResult
        {
            public IntPtr error;
            public CArtifacts ok;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CDicedSprite
        {
            public IntPtr id;
            public ulong atlas;
            public CSlice vertices;
            public CSlice uvs;
            public CSlice indices;
            public CRect rect;
            public CPivot pivot;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CVertex
        {
            public float x;
            public float y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CUv
        {
            public float u;
            public float v;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRect
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CPivot
        {
            public float x;
            public float y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CProgress
        {
            public float ratio;
            public IntPtr activity;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CSlice
        {
            public IntPtr ptr;
            public ulong len;
        }

        private delegate void CProgressCallback (CProgress progress);

        public static Artifacts Dice (IEnumerable<SourceSprite> sprites, Prefs prefs)
        {
            var pts = new List<IntPtr>();
            var pins = new List<GCHandle>();
            var result = dice(MarshalSourceSprites(sprites, pins), MarshalPrefs(prefs));
            pins.ForEach(c => c.Free());

            var error = Marshal.PtrToStringUTF8(result.error);
            if (!string.IsNullOrEmpty(error))
                throw new Exception(error);

            return new Artifacts(
                MarshalAtlases(result.ok.atlases, pts),
                MarshalDicedSprites(result.ok.sprites, pts),
                pts
            );
        }

        [DllImport("sprite_dicing")]
        private static extern CResult dice (CSlice sprites, CPrefs prefs);

        private static CPrefs MarshalPrefs (Prefs prefs) => new() {
            unit_size = prefs.UnitSize,
            padding = prefs.Padding,
            uv_inset = prefs.UVInset,
            trim_transparent = prefs.TrimTransparent,
            atlas_size_limit = prefs.AtlasSizeLimit,
            atlas_square = prefs.AtlasSquare,
            atlas_pot = prefs.AtlasPOT,
            pivot = MarshalPivot(prefs.Pivot),
            ppu = prefs.PPU,
            has_progress_callback = prefs.OnProgress != null,
            progress_callback = p => prefs.OnProgress(MarshalProgress(p))
        };

        private static T[] MarshalSlice<T> (CSlice c, List<IntPtr> pts)
        {
            pts.Add(c.ptr);

            var size = Marshal.SizeOf(typeof(T));
            var structs = new T[c.len];

            for (long i = 0; i < (int)c.len; i++)
            {
                var ins = new IntPtr(c.ptr.ToInt64() + (i * size));
                structs[i] = Marshal.PtrToStructure<T>(ins);
            }

            return structs;
        }

        private static CSlice MarshalSlice<T> (IReadOnlyList<T> array, List<GCHandle> pins)
        {
            var pin = GCHandle.Alloc(array, GCHandleType.Pinned);
            pins.Add(pin);
            return new CSlice {
                ptr = pin.AddrOfPinnedObject(),
                len = (ulong)array.Count
            };
        }

        private static Texture[] MarshalAtlases (CSlice c, List<IntPtr> pts)
        {
            var atlasSlices = MarshalSlice<CTexture>(c, pts);
            return atlasSlices.Select(s => MarshalTexture(s, pts)).ToArray();
        }

        private static CSlice MarshalSourceSprites (IEnumerable<SourceSprite> sources, List<GCHandle> pins)
        {
            var sprites = sources.Select(s => MarshalSourceSprite(s, pins)).ToArray();
            return MarshalSlice(sprites, pins);
        }

        private static CSourceSprite MarshalSourceSprite (SourceSprite s, List<GCHandle> pins) => new() {
            id = Marshal.StringToHGlobalAnsi(s.Id),
            texture = MarshalTexture(s.Texture, pins),
            has_pivot = s.Pivot.HasValue,
            pivot = new CPivot {
                x = s.Pivot.GetValueOrDefault().X,
                y = s.Pivot.GetValueOrDefault().Y
            }
        };

        private static DicedSprite MarshalDicedSprite (CDicedSprite c, List<IntPtr> pts) => new() {
            Id = Marshal.PtrToStringUTF8(c.id),
            Atlas = (int)c.atlas,
            Vertices = MarshalSlice<CVertex>(c.vertices, pts).Select(MarshalVertex).ToArray(),
            UVs = MarshalSlice<CUv>(c.uvs, pts).Select(MarshalUV).ToArray(),
            Indices = MarshalIndices(c.indices),
            Rect = MarshalRect(c.rect),
            Pivot = MarshalPivot(c.pivot)
        };

        private static DicedSprite[] MarshalDicedSprites (CSlice c, List<IntPtr> pts)
        {
            var sprites = MarshalSlice<CDicedSprite>(c, pts);
            return sprites.Select(s => MarshalDicedSprite(s, pts)).ToArray();
        }

        private static Vertex MarshalVertex (CVertex c) => new() {
            X = c.x,
            Y = c.y
        };

        private static UV MarshalUV (CUv c) => new() {
            U = c.u,
            V = c.v
        };

        private static int[] MarshalIndices (CSlice c)
        {
            var array = new int[c.len];
            var longPtr = (ulong*)c.ptr;
            for (int i = 0; i < array.Length; ++i)
                array[i] = (int)*longPtr++;
            return array;
        }

        private static Rect MarshalRect (CRect c) => new() {
            X = c.x,
            Y = c.y,
            Width = c.width,
            Height = c.height
        };

        private static Pivot MarshalPivot (CPivot c) => new() {
            X = c.x,
            Y = c.y
        };

        private static CPivot MarshalPivot (Pivot p) => new() {
            x = p.X,
            y = p.Y
        };

        private static Texture MarshalTexture (CTexture c, List<IntPtr> pts) => new() {
            Width = c.width,
            Height = c.height,
            Pixels = MarshalSlice<Pixel>(c.pixels, pts)
        };

        private static CTexture MarshalTexture (Texture p, List<GCHandle> pins) => new() {
            width = p.Width,
            height = p.Height,
            pixels = MarshalSlice(p.Pixels, pins)
        };

        private static Progress MarshalProgress (CProgress p) => new() {
            Ratio = p.ratio,
            Activity = Marshal.PtrToStringUTF8(p.activity)
        };
    }
}
