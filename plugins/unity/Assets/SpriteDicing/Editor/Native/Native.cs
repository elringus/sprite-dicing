using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpriteDicing
{
    /// <summary>
    /// Managed wrapper over native dicing library.
    /// </summary>
    public static unsafe class Native
    {
        // TODO: Readonly structs.

        public struct SourceSprite
        {
            public string Id { get; set; }
            public byte[] Bytes { get; set; }
            public string Format { get; set; }
            public Pivot? Pivot { get; set; }
        }

        public struct Prefs
        {
            public uint UnitSize { get; set; }
            public uint Padding { get; set; }
            public float UVInset { get; set; }
            public bool TrimTransparent { get; set; }
            public uint AtlasSizeLimit { get; set; }
            public bool AtlasSquare { get; set; }
            public bool AtlasPOT { get; set; }
            public AtlasFormat AtlasFormat { get; set; }
            public float PPU { get; set; }
            public Pivot Pivot { get; set; }
            public ProgressCallback OnProgress { get; set; }
        }

        public enum AtlasFormat : byte
        {
            PNG,
            JPEG,
            WEBP,
            TGA,
            TIFF
        }

        public class Artifacts : IDisposable
        {
            public IReadOnlyList<byte[]> Atlases { get; }
            public IReadOnlyList<DicedSprite> Sprites { get; }

            private readonly List<IntPtr> pts;

            internal Artifacts (byte[][] atlases, DicedSprite[] sprites, List<IntPtr> pts)
            {
                Atlases = atlases;
                Sprites = sprites;
                this.pts = pts;
            }

            public void Dispose ()
            {
                foreach (var ptr in pts)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        public struct DicedSprite
        {
            public string Id { get; set; }
            public int Atlas { get; set; }
            public IReadOnlyList<Vertex> Vertices { get; set; }
            public IReadOnlyList<UV> UVs { get; set; }
            public IReadOnlyList<int> Indices { get; set; }
            public Rect Rect { get; set; }
            public Pivot Pivot { get; set; }
        }

        public struct Vertex
        {
            public float X { get; set; }
            public float Y { get; set; }
        }

        public struct UV
        {
            public float U { get; set; }
            public float V { get; set; }
        }

        public struct Pivot
        {
            public float X { get; set; }
            public float Y { get; set; }
        }

        public struct Rect
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
        }

        public struct Progress
        {
            public float Ratio { get; set; }
            public string Activity { get; set; }
        }

        public delegate void ProgressCallback (Progress progress);

        [StructLayout(LayoutKind.Sequential)]
        private struct CSourceSprite
        {
            public IntPtr id;
            public CSlice bytes;
            public IntPtr format;
            [MarshalAs(UnmanagedType.I1)]
            public bool has_pivot;
            public CPivot pivot;
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
            public byte atlas_format;
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
        private struct CSlice
        {
            public IntPtr ptr;
            public ulong len;
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
            public CPivot Pivot;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CVertex
        {
            public float x;
            public float y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CUV
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

        private delegate void CProgressCallback (CProgress progress);

        public static Artifacts Dice (IEnumerable<SourceSprite> sprites, Prefs prefs)
        {
            var pts = new List<IntPtr>();
            var pins = new List<GCHandle>();
            var diced = dice(MarshalSourceSprites(sprites, pins), MarshalPrefs(prefs));
            pins.ForEach(c => c.Free());
            return new Artifacts(
                MarshalAtlases(diced.atlases, pts),
                MarshalDicedSprites(diced.sprites, pts), pts);
        }

        [DllImport("sprite_dicing")]
        private static extern CArtifacts dice (CSlice sprites, CPrefs prefs);

        private static CPrefs MarshalPrefs (Prefs prefs) => new CPrefs {
            unit_size = prefs.UnitSize,
            padding = prefs.Padding,
            uv_inset = prefs.UVInset,
            trim_transparent = prefs.TrimTransparent,
            atlas_size_limit = prefs.AtlasSizeLimit,
            atlas_square = prefs.AtlasSquare,
            atlas_pot = prefs.AtlasPOT,
            pivot = MarshalPivot(prefs.Pivot),
            ppu = prefs.PPU,
            atlas_format = (byte)prefs.AtlasFormat,
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
                var ins = new IntPtr(c.ptr.ToInt64() + i * size);
                structs[i] = Marshal.PtrToStructure<T>(ins);
            }

            return structs;
        }

        private static CSlice MarshalSlice<T> (T[] array, List<GCHandle> pins)
        {
            var pin = GCHandle.Alloc(array, GCHandleType.Pinned);
            pins.Add(pin);
            return new CSlice {
                ptr = pin.AddrOfPinnedObject(),
                len = (ulong)array.Length
            };
        }

        private static byte[] MarshalBytes (CSlice c, List<IntPtr> pts)
        {
            pts.Add(c.ptr);
            var array = new byte[c.len];
            Marshal.Copy(c.ptr, array, 0, (int)c.len);
            return array;
        }

        private static byte[][] MarshalAtlases (CSlice c, List<IntPtr> pts)
        {
            var atlasSlices = MarshalSlice<CSlice>(c, pts);
            return atlasSlices.Select(s => MarshalBytes(s, pts)).ToArray();
        }

        private static CSlice MarshalSourceSprites (IEnumerable<SourceSprite> sources, List<GCHandle> pins)
        {
            var sprites = sources.Select(s => MarshalSourceSprite(s, pins)).ToArray();
            return MarshalSlice(sprites, pins);
        }

        private static CSourceSprite MarshalSourceSprite (SourceSprite s, List<GCHandle> pins) => new CSourceSprite {
            id = Marshal.StringToHGlobalAnsi(s.Id),
            bytes = MarshalSlice(s.Bytes, pins),
            format = Marshal.StringToHGlobalAnsi(s.Format),
            has_pivot = s.Pivot.HasValue,
            pivot = new CPivot { x = s.Pivot.GetValueOrDefault().X, y = s.Pivot.GetValueOrDefault().Y }
        };

        private static DicedSprite MarshalDicedSprite (CDicedSprite c, List<IntPtr> pts) => new DicedSprite {
            // TODO: Marshal.PtrToStringUTF8
            Id = Marshal.PtrToStringAnsi(c.id),
            Atlas = (int)c.atlas,
            Vertices = MarshalSlice<CVertex>(c.vertices, pts).Select(MarshalVertex).ToArray(),
            UVs = MarshalSlice<CUV>(c.uvs, pts).Select(MarshalUV).ToArray(),
            Indices = MarshalIndices(c.indices),
            Rect = MarshalRect(c.rect),
            Pivot = MarshalPivot(c.Pivot)
        };

        private static DicedSprite[] MarshalDicedSprites (CSlice c, List<IntPtr> pts)
        {
            var sprites = MarshalSlice<CDicedSprite>(c, pts);
            return sprites.Select(s => MarshalDicedSprite(s, pts)).ToArray();
        }

        private static Vertex MarshalVertex (CVertex c) => new Vertex {
            X = c.x,
            Y = c.y
        };

        private static UV MarshalUV (CUV c) => new UV {
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

        private static Rect MarshalRect (CRect c) => new Rect {
            X = c.x,
            Y = c.y,
            Width = c.width,
            Height = c.height
        };

        private static Pivot MarshalPivot (CPivot c) => new Pivot {
            X = c.x,
            Y = c.y
        };

        private static CPivot MarshalPivot (Pivot p) => new CPivot {
            x = p.X,
            y = p.Y
        };

        private static Progress MarshalProgress (CProgress p) => new Progress {
            Ratio = p.ratio,
            // TODO: Marshal.PtrToStringUTF8
            Activity = Marshal.PtrToStringAnsi(p.activity)
        };
    }
}
