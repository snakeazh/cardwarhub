using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace FusionContinent.SpineBake
{
    public struct SpineMeshFrameEvent
    {
        public int FrameIndex;
        public string EventName;
    }

    public class SpineMeshFrame
    {
        public Vector3[] Vertices;
        public Vector2[] Uvs;
        public Color32[] Colors;
        public Bounds Bounds;
        public short SubMeshCount;
        public int[][] Triangles;
    }

    /// <summary>运行时/烘焙优化后的单帧数据，仅保留每帧变化项。</summary>
    public class SpineMeshCompactFrame
    {
        public Vector3[] Vertices;
        public Color32[] Colors;
        public Bounds Bounds;

        /// <summary>仅当动作级拓扑不共享时填充。</summary>
        public Vector2[] Uvs;
        public short SubMeshCount;
        public int[][] Triangles;
    }

    public class SpineMeshActionFrames
    {
        public string ActionName;
        public SpineMeshFrame[] Frames;
        public SpineMeshFrameEvent[] Events;

        public bool HasSharedTopology;
        public bool HasSharedColors;
        public Vector2[] SharedUvs;
        public int[][] SharedTriangles;
        public short SharedSubMeshCount;
        public Color32[] SharedColors;
        public SpineMeshCompactFrame[] CompactFrames;
    }

    /// <summary>
    /// 编辑器预烘焙的 Spine 网格帧数据，运行时按帧查表播放。
    /// </summary>
    public class SpineMeshFrameData
    {
        public const float DefaultFrameLength = 1f / 30f;
        private const int FormatVersionV2 = 2;
        private static readonly byte[] MagicBytes = { (byte)'S', (byte)'M', (byte)'B', (byte)'K' };

        public float FrameLength = DefaultFrameLength;
        public Dictionary<string, SpineMeshActionFrames> ActionDic = new Dictionary<string, SpineMeshActionFrames>();

        public bool HasAction(string actionName)
        {
            return ActionDic.ContainsKey(actionName);
        }

        public void OptimizeAllActions()
        {
            foreach (KeyValuePair<string, SpineMeshActionFrames> pair in ActionDic)
            {
                BuildCompactFrames(pair.Value);
            }
        }

        public string BuildOptimizationReport()
        {
            OptimizeAllActions();
            var report = new StringBuilder(256);
            foreach (KeyValuePair<string, SpineMeshActionFrames> pair in ActionDic)
            {
                SpineMeshActionFrames action = pair.Value;
                int frameCount = action.CompactFrames != null ? action.CompactFrames.Length : 0;
                int vertexCount = 0;
                if (action.HasSharedTopology && action.SharedUvs != null)
                {
                    vertexCount = action.SharedUvs.Length;
                }
                else if (action.CompactFrames != null && action.CompactFrames.Length > 0)
                {
                    vertexCount = action.CompactFrames[0].Vertices.Length;
                }

                report.Append(pair.Key);
                report.Append(": frames=");
                report.Append(frameCount);
                report.Append(" verts=");
                report.Append(vertexCount);
                report.Append(" sharedTopology=");
                report.Append(action.HasSharedTopology);
                report.Append(" sharedColors=");
                report.Append(action.HasSharedColors);
                report.Append(" subMeshes=");
                report.Append(action.HasSharedTopology ? action.SharedSubMeshCount : (short)0);
                report.AppendLine();
            }

            return report.ToString();
        }

        public static void BuildCompactFrames(SpineMeshActionFrames action)
        {
            if (action == null)
            {
                return;
            }

            if (action.CompactFrames != null && action.Frames == null)
            {
                return;
            }

            if (action.Frames == null || action.Frames.Length == 0)
            {
                return;
            }

            SpineMeshFrame first = action.Frames[0];
            int vertexCount = first.Vertices.Length;
            short subMeshCount = first.SubMeshCount;

            bool sharedTopology = true;
            bool sharedColors = true;
            bool sharedUvs = true;
            bool sharedTriangles = true;

            for (int f = 1; f < action.Frames.Length; f++)
            {
                SpineMeshFrame frame = action.Frames[f];
                if (frame.Vertices.Length != vertexCount || frame.SubMeshCount != subMeshCount)
                {
                    sharedTopology = false;
                    sharedUvs = false;
                    sharedTriangles = false;
                    sharedColors = false;
                    break;
                }

                if (sharedUvs && !CompareVector2Array(first.Uvs, frame.Uvs))
                {
                    sharedUvs = false;
                }

                if (sharedTriangles && !CompareTriangleArrays(first.Triangles, frame.Triangles))
                {
                    sharedTriangles = false;
                }

                if (sharedColors && !CompareColor32Array(first.Colors, frame.Colors))
                {
                    sharedColors = false;
                }
            }

            sharedTopology = sharedUvs && sharedTriangles;

            action.HasSharedTopology = sharedTopology;
            action.HasSharedColors = sharedColors;

            if (sharedTopology)
            {
                action.SharedUvs = first.Uvs;
                action.SharedTriangles = first.Triangles;
                action.SharedSubMeshCount = subMeshCount;
            }
            else
            {
                action.SharedUvs = null;
                action.SharedTriangles = null;
                action.SharedSubMeshCount = 0;
            }

            if (sharedColors)
            {
                action.SharedColors = first.Colors;
            }
            else
            {
                action.SharedColors = null;
            }

            SpineMeshCompactFrame[] compactFrames = new SpineMeshCompactFrame[action.Frames.Length];
            for (int i = 0; i < action.Frames.Length; i++)
            {
                SpineMeshFrame source = action.Frames[i];
                SpineMeshCompactFrame compact = new SpineMeshCompactFrame
                {
                    Vertices = source.Vertices,
                    Bounds = source.Bounds
                };

                if (!sharedColors)
                {
                    compact.Colors = source.Colors;
                }

                if (!sharedTopology)
                {
                    compact.Uvs = source.Uvs;
                    compact.SubMeshCount = source.SubMeshCount;
                    compact.Triangles = source.Triangles;
                }

                compactFrames[i] = compact;
            }

            action.CompactFrames = compactFrames;
            action.Frames = null;
        }

        public static SpineMeshFrameData ReadBinary(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                if (TryReadMagic(reader))
                {
                    int version = reader.ReadInt32();
                    if (version == FormatVersionV2)
                    {
                        return ReadBinaryV2(reader);
                    }

                    LogTool.LogError("[SpineMeshFrameData] 不支持的烘焙格式版本: " + version);
                    return null;
                }

                stream.Position = 0;
                return ReadBinaryV1(reader);
            }
        }

        public byte[] WriteBinary()
        {
            OptimizeAllActions();

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(MagicBytes);
                writer.Write(FormatVersionV2);
                writer.Write(FrameLength);
                writer.Write(ActionDic.Count);
                foreach (KeyValuePair<string, SpineMeshActionFrames> pair in ActionDic)
                {
                    WriteActionV2(writer, pair.Value);
                }

                return stream.ToArray();
            }
        }

        private static bool TryReadMagic(BinaryReader reader)
        {
            if (reader.BaseStream.Length < 4)
            {
                return false;
            }

            byte b0 = reader.ReadByte();
            byte b1 = reader.ReadByte();
            byte b2 = reader.ReadByte();
            byte b3 = reader.ReadByte();
            return b0 == MagicBytes[0] && b1 == MagicBytes[1] && b2 == MagicBytes[2] && b3 == MagicBytes[3];
        }

        private static SpineMeshFrameData ReadBinaryV1(BinaryReader reader)
        {
            var data = new SpineMeshFrameData
            {
                FrameLength = reader.ReadSingle()
            };

            int actionCount = reader.ReadInt32();
            for (int a = 0; a < actionCount; a++)
            {
                var action = new SpineMeshActionFrames
                {
                    ActionName = reader.ReadString(),
                    Events = ReadEvents(reader),
                    Frames = ReadFrames(reader)
                };
                data.ActionDic[action.ActionName] = action;
            }

            data.OptimizeAllActions();
            return data;
        }

        private static SpineMeshFrameData ReadBinaryV2(BinaryReader reader)
        {
            var data = new SpineMeshFrameData
            {
                FrameLength = reader.ReadSingle()
            };

            int actionCount = reader.ReadInt32();
            for (int a = 0; a < actionCount; a++)
            {
                var action = ReadActionV2(reader);
                data.ActionDic[action.ActionName] = action;
            }

            return data;
        }

        private static SpineMeshActionFrames ReadActionV2(BinaryReader reader)
        {
            var action = new SpineMeshActionFrames
            {
                ActionName = reader.ReadString(),
                Events = ReadEvents(reader)
            };

            byte flags = reader.ReadByte();
            action.HasSharedTopology = (flags & 1) != 0;
            action.HasSharedColors = (flags & 2) != 0;

            if (action.HasSharedTopology)
            {
                action.SharedSubMeshCount = reader.ReadInt16();
                int vertexCount = reader.ReadInt32();
                action.SharedUvs = ReadVector2Array(reader, vertexCount);
                action.SharedTriangles = new int[action.SharedSubMeshCount][];
                for (int s = 0; s < action.SharedSubMeshCount; s++)
                {
                    int triCount = reader.ReadInt32();
                    action.SharedTriangles[s] = ReadIntArray(reader, triCount);
                }
            }

            if (action.HasSharedColors)
            {
                int colorCount = reader.ReadInt32();
                action.SharedColors = ReadColor32Array(reader, colorCount);
            }

            int frameCount = reader.ReadInt32();
            action.CompactFrames = new SpineMeshCompactFrame[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                action.CompactFrames[f] = ReadCompactFrameV2(reader, action);
            }

            return action;
        }

        private static SpineMeshCompactFrame ReadCompactFrameV2(BinaryReader reader, SpineMeshActionFrames action)
        {
            int vertexCount = reader.ReadInt32();
            var frame = new SpineMeshCompactFrame
            {
                Vertices = ReadVector3Array(reader, vertexCount),
                Bounds = ReadBounds(reader)
            };

            if (!action.HasSharedColors)
            {
                frame.Colors = ReadColor32Array(reader, vertexCount);
            }

            if (!action.HasSharedTopology)
            {
                frame.Uvs = ReadVector2Array(reader, vertexCount);
                frame.SubMeshCount = reader.ReadInt16();
                frame.Triangles = new int[frame.SubMeshCount][];
                for (int s = 0; s < frame.SubMeshCount; s++)
                {
                    int triCount = reader.ReadInt32();
                    frame.Triangles[s] = ReadIntArray(reader, triCount);
                }
            }

            return frame;
        }

        private static void WriteActionV2(BinaryWriter writer, SpineMeshActionFrames action)
        {
            if (action.CompactFrames == null)
            {
                BuildCompactFrames(action);
            }

            writer.Write(action.ActionName);
            WriteEvents(writer, action.Events);

            byte flags = 0;
            if (action.HasSharedTopology)
            {
                flags |= 1;
            }

            if (action.HasSharedColors)
            {
                flags |= 2;
            }

            writer.Write(flags);

            if (action.HasSharedTopology)
            {
                writer.Write(action.SharedSubMeshCount);
                writer.Write(action.SharedUvs.Length);
                WriteVector2Array(writer, action.SharedUvs);
                for (int s = 0; s < action.SharedSubMeshCount; s++)
                {
                    writer.Write(action.SharedTriangles[s].Length);
                    WriteIntArray(writer, action.SharedTriangles[s]);
                }
            }

            if (action.HasSharedColors)
            {
                writer.Write(action.SharedColors.Length);
                WriteColor32Array(writer, action.SharedColors);
            }

            writer.Write(action.CompactFrames.Length);
            for (int i = 0; i < action.CompactFrames.Length; i++)
            {
                WriteCompactFrameV2(writer, action.CompactFrames[i], action);
            }
        }

        private static void WriteCompactFrameV2(BinaryWriter writer, SpineMeshCompactFrame frame, SpineMeshActionFrames action)
        {
            writer.Write(frame.Vertices.Length);
            WriteVector3Array(writer, frame.Vertices);
            WriteBounds(writer, frame.Bounds);

            if (!action.HasSharedColors)
            {
                WriteColor32Array(writer, frame.Colors);
            }

            if (!action.HasSharedTopology)
            {
                WriteVector2Array(writer, frame.Uvs);
                writer.Write(frame.SubMeshCount);
                for (int s = 0; s < frame.SubMeshCount; s++)
                {
                    writer.Write(frame.Triangles[s].Length);
                    WriteIntArray(writer, frame.Triangles[s]);
                }
            }
        }

        private static SpineMeshFrameEvent[] ReadEvents(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            var events = new SpineMeshFrameEvent[count];
            for (int i = 0; i < count; i++)
            {
                events[i] = new SpineMeshFrameEvent
                {
                    FrameIndex = reader.ReadInt32(),
                    EventName = reader.ReadString()
                };
            }

            return events;
        }

        private static void WriteEvents(BinaryWriter writer, SpineMeshFrameEvent[] events)
        {
            if (events == null)
            {
                writer.Write(0);
                return;
            }

            writer.Write(events.Length);
            for (int i = 0; i < events.Length; i++)
            {
                writer.Write(events[i].FrameIndex);
                writer.Write(events[i].EventName);
            }
        }

        private static SpineMeshFrame[] ReadFrames(BinaryReader reader)
        {
            int frameCount = reader.ReadInt32();
            var frames = new SpineMeshFrame[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                frames[f] = ReadFrame(reader);
            }

            return frames;
        }

        private static SpineMeshFrame ReadFrame(BinaryReader reader)
        {
            int vertexCount = reader.ReadInt32();
            var frame = new SpineMeshFrame
            {
                Vertices = ReadVector3Array(reader, vertexCount),
                Uvs = ReadVector2Array(reader, vertexCount),
                Colors = ReadColor32Array(reader, vertexCount),
                Bounds = ReadBounds(reader)
            };
            frame.SubMeshCount = reader.ReadInt16();
            frame.Triangles = new int[frame.SubMeshCount][];

            for (int s = 0; s < frame.SubMeshCount; s++)
            {
                int triCount = reader.ReadInt32();
                frame.Triangles[s] = ReadIntArray(reader, triCount);
            }

            return frame;
        }

        private static Vector3[] ReadVector3Array(BinaryReader reader, int count)
        {
            var arr = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            return arr;
        }

        private static void WriteVector3Array(BinaryWriter writer, Vector3[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                writer.Write(arr[i].x);
                writer.Write(arr[i].y);
                writer.Write(arr[i].z);
            }
        }

        private static Vector2[] ReadVector2Array(BinaryReader reader, int count)
        {
            var arr = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }

            return arr;
        }

        private static void WriteVector2Array(BinaryWriter writer, Vector2[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                writer.Write(arr[i].x);
                writer.Write(arr[i].y);
            }
        }

        private static Color32[] ReadColor32Array(BinaryReader reader, int count)
        {
            var arr = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
            }

            return arr;
        }

        private static void WriteColor32Array(BinaryWriter writer, Color32[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                writer.Write(arr[i].r);
                writer.Write(arr[i].g);
                writer.Write(arr[i].b);
                writer.Write(arr[i].a);
            }
        }

        private static int[] ReadIntArray(BinaryReader reader, int count)
        {
            var arr = new int[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = reader.ReadInt32();
            }

            return arr;
        }

        private static void WriteIntArray(BinaryWriter writer, int[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                writer.Write(arr[i]);
            }
        }

        private static Bounds ReadBounds(BinaryReader reader)
        {
            var center = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var size = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            return new Bounds(center, size);
        }

        private static void WriteBounds(BinaryWriter writer, Bounds bounds)
        {
            writer.Write(bounds.center.x);
            writer.Write(bounds.center.y);
            writer.Write(bounds.center.z);
            writer.Write(bounds.size.x);
            writer.Write(bounds.size.y);
            writer.Write(bounds.size.z);
        }

        private static bool CompareVector2Array(Vector2[] a, Vector2[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].x != b[i].x || a[i].y != b[i].y)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareColor32Array(Color32[] a, Color32[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].r != b[i].r || a[i].g != b[i].g || a[i].b != b[i].b || a[i].a != b[i].a)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareTriangleArrays(int[][] a, int[][] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int s = 0; s < a.Length; s++)
            {
                if (a[s].Length != b[s].Length)
                {
                    return false;
                }

                for (int i = 0; i < a[s].Length; i++)
                {
                    if (a[s][i] != b[s][i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <summary>MeshFrameData 别名，与文档命名一致。</summary>
    public class MeshFrameData : SpineMeshFrameData
    {
    }
}
