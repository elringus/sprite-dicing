using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpriteDicing
{
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

            private readonly IntPtr[] pointers;

            internal Artifacts (byte[][] atlases, DicedSprite[] sprites, IntPtr[] pointers)
            {
                Atlases = atlases;
                Sprites = sprites;
                this.pointers = pointers;
            }

            public void Dispose ()
            {
                foreach (var ptr in pointers)
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

        [StructLayout(LayoutKind.Sequential)]
        private struct CSprite
        {
            public IntPtr id;
            public CSlice bytes;
            public IntPtr format;
            [MarshalAs(UnmanagedType.I1)]
            public bool has_pivot;
            public float pivot_x;
            public float pivot_y;
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
            public float pivot_x;
            public float pivot_y;
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

        public static Artifacts Dice (IEnumerable<SourceSprite> sprites, Prefs prefs)
        {
            var cBytes = new List<GCHandle>();
            var cSprites = Pin(sprites.Select(s => {
                var bytes = Pin(s.Bytes);
                cBytes.Add(bytes);
                return new CSprite {
                    id = Marshal.StringToHGlobalAnsi(s.Id),
                    bytes = new CSlice {
                        ptr = bytes.AddrOfPinnedObject(),
                        len = (ulong)((byte[])bytes.Target).Length
                    },
                    format = Marshal.StringToHGlobalAnsi(s.Format),
                    has_pivot = s.Pivot.HasValue,
                    pivot_x = s.Pivot.GetValueOrDefault().X,
                    pivot_y = s.Pivot.GetValueOrDefault().Y
                };
            }).ToArray());
            var cSliceOfSprites = new CSlice {
                ptr = cSprites.AddrOfPinnedObject(),
                len = (ulong)((CSprite[])cSprites.Target).Length
            };
            var diced = dice(cSliceOfSprites, MarshalPrefs(prefs));

            cBytes.ForEach(c => c.Free());
            cSprites.Free();

            var atlasSlices = MarshalSlice<CSlice>(diced.atlases);
            var atlases = atlasSlices.Select(s => MarshalBytes(s.ptr, (int)s.len)).ToArray();
            var cDicedSprites = MarshalSlice<CDicedSprite>(diced.sprites);
            var dicedSprites = cDicedSprites.Select(MarshalDicedSprite).ToArray();
            var pointers = atlasSlices.Select(s => s.ptr)
                .Append(diced.atlases.ptr)
                .Append(diced.sprites.ptr)
                .Concat(cDicedSprites.SelectMany(c => new[] {
                    c.vertices.ptr,
                    c.uvs.ptr,
                    c.indices.ptr
                }))
                .ToArray();
            return new Artifacts(atlases, dicedSprites, pointers);
        }

        [DllImport("sprite_dicing")]
        private static extern CArtifacts dice (CSlice sprites, CPrefs prefs);

        private static CPrefs MarshalPrefs (Prefs prefs)
        {
            return new CPrefs {
                unit_size = prefs.UnitSize,
                padding = prefs.Padding,
                uv_inset = prefs.UVInset,
                trim_transparent = prefs.TrimTransparent,
                atlas_size_limit = prefs.AtlasSizeLimit,
                atlas_square = prefs.AtlasSquare,
                atlas_pot = prefs.AtlasPOT,
                pivot_x = prefs.Pivot.X,
                pivot_y = prefs.Pivot.Y,
                ppu = prefs.PPU,
                atlas_format = (byte)prefs.AtlasFormat
            };
        }

        private static T[] MarshalSlice<T> (CSlice c)
        {
            var size = Marshal.SizeOf(typeof(T));
            var structs = new T[c.len];

            for (long i = 0; i < (int)c.len; i++)
            {
                var ins = new IntPtr(c.ptr.ToInt64() + i * size);
                structs[i] = Marshal.PtrToStructure<T>(ins);
            }

            return structs;
        }

        private static byte[] MarshalBytes (IntPtr ptr, int length)
        {
            var array = new byte[length];
            Marshal.Copy(ptr, array, 0, length);
            return array;
        }

        private static DicedSprite MarshalDicedSprite (CDicedSprite c)
        {
            return new DicedSprite {
                // TODO: Marshal.PtrToStringUTF8
                Id = Marshal.PtrToStringAnsi(c.id),
                Atlas = (int)c.atlas,
                Vertices = MarshalSlice<CVertex>(c.vertices).Select(MarshalVertex).ToArray(),
                UVs = MarshalSlice<CUV>(c.uvs).Select(MarshalUV).ToArray(),
                Indices = MarshalIndices(c.indices),
                Rect = MarshalRect(c.rect)
            };
        }

        private static Vertex MarshalVertex (CVertex c)
        {
            return new Vertex { X = c.x, Y = c.y };
        }

        private static UV MarshalUV (CUV c)
        {
            return new UV { U = c.u, V = c.v };
        }

        private static int[] MarshalIndices (CSlice c)
        {
            var array = new int[c.len];
            var longPtr = (ulong*)c.ptr;
            for (int i = 0; i < array.Length; ++i)
                array[i] = (int)*longPtr++;
            return array;
        }

        private static Rect MarshalRect (CRect c)
        {
            return new Rect {
                X = c.x,
                Y = c.y,
                Width = c.width,
                Height = c.height
            };
        }

        private static GCHandle Pin (object obj)
        {
            return GCHandle.Alloc(obj, GCHandleType.Pinned);
        }
    }
}
