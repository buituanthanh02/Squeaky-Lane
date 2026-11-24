using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SqueakyLane.EditorTools
{
    // Chỉ chạy khi tạo prefab. Chèn một đoạn thùng tại đường cắt; hai đầu giữ nguyên hình.
    internal static class KenneyTruckMesh
    {
        private struct Vertex
        {
            public Vector3 position, normal;
            public Vector4 tangent;
            public Vector2 uv;
            public Color color;

            public static Vertex Lerp(Vertex a, Vertex b, float t)
            {
                return new Vertex
                {
                    position = Vector3.LerpUnclamped(a.position, b.position, t),
                    normal = Vector3.LerpUnclamped(a.normal, b.normal, t).normalized,
                    tangent = Vector4.LerpUnclamped(a.tangent, b.tangent, t),
                    uv = Vector2.LerpUnclamped(a.uv, b.uv, t),
                    color = Color.LerpUnclamped(a.color, b.color, t)
                };
            }
        }

        internal static Mesh ExtendBed(Mesh source, Matrix4x4 meshToFootprint, float cut, float extra)
        {
            if (source == null || source.subMeshCount != 1 || extra <= 0)
            {
                throw new ArgumentException("Cần mesh thân tải một submesh và độ nối dài dương.");
            }
            // Car Kit 3.1 chỉ có UV0. Không âm thầm làm mất dữ liệu nếu đổi sang kit khác.
            for (int channel = 1; channel < 8; channel++)
            {
                if (source.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0 + channel)))
                {
                    throw new InvalidOperationException("Mesh tải có thêm UV ngoài UV0; cần kiểm lại trước khi nối thùng.");
                }
            }
            var positions = source.vertices;
            var normals = source.normals;
            var tangents = source.tangents;
            var uvs = source.uv;
            var colors = source.colors;
            var input = new Vertex[positions.Length];
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = new Vertex
                {
                    position = meshToFootprint.MultiplyPoint3x4(positions[i]),
                    normal = normals.Length == positions.Length ? normals[i] : Vector3.up,
                    tangent = tangents.Length == positions.Length ? tangents[i] : Vector4.zero,
                    uv = uvs.Length == positions.Length ? uvs[i] : Vector2.zero,
                    color = colors.Length == positions.Length ? colors[i] : Color.white
                };
            }
            var output = new List<Vertex>();
            var triangles = new List<int>();
            var indices = source.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var triangle = new[] { input[indices[i]], input[indices[i + 1]], input[indices[i + 2]] };
                AddPolygon(Clip(triangle, cut, true), -extra * .5f, output, triangles);
                AddPolygon(Clip(triangle, cut, false), extra * .5f, output, triangles);
                Vertex rearToFront = default, frontToRear = default;
                bool enters = false, leaves = false;
                for (int edge = 0; edge < 3; edge++)
                {
                    Vertex a = triangle[edge], b = triangle[(edge + 1) % 3];
                    if (a.position.x <= cut && b.position.x > cut)
                    {
                        rearToFront = Intersect(a, b, cut);
                        enters = true;
                    }
                    if (a.position.x > cut && b.position.x <= cut)
                    {
                        frontToRear = Intersect(a, b, cut);
                        leaves = true;
                    }
                }
                if (enters && leaves)
                {
                    // Nối hai mép vừa cắt, giữ winding của tam giác nguồn và UV bảng màu.
                    AddPolygon(new List<Vertex>
                    {
                        Shift(rearToFront, -extra * .5f), Shift(rearToFront, extra * .5f),
                        Shift(frontToRear, extra * .5f), Shift(frontToRear, -extra * .5f)
                    }, 0, output, triangles);
                }
            }
            var verticesOut = new List<Vector3>();
            var normalsOut = new List<Vector3>();
            var tangentsOut = new List<Vector4>();
            var uvOut = new List<Vector2>();
            var colorsOut = new List<Color>();
            Matrix4x4 footprintToMesh = meshToFootprint.inverse;
            foreach (var vertex in output)
            {
                verticesOut.Add(footprintToMesh.MultiplyPoint3x4(vertex.position));
                normalsOut.Add(vertex.normal);
                tangentsOut.Add(vertex.tangent);
                uvOut.Add(vertex.uv);
                colorsOut.Add(vertex.color);
            }
            var result = new Mesh
            {
                name = "Truck body · thùng dài",
                indexFormat = output.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            result.SetVertices(verticesOut);
            result.SetTriangles(triangles, 0);
            result.SetNormals(normalsOut);
            result.SetUVs(0, uvOut);
            if (tangents.Length == positions.Length)
            {
                result.SetTangents(tangentsOut);
            }
            if (colors.Length == positions.Length)
            {
                result.SetColors(colorsOut);
            }
            result.RecalculateBounds();
            return result;
        }

        private static List<Vertex> Clip(Vertex[] triangle, float cut, bool rear)
        {
            var polygon = new List<Vertex>();
            for (int i = 0; i < 3; i++)
            {
                Vertex a = triangle[i], b = triangle[(i + 1) % 3];
                bool insideA = rear ? a.position.x <= cut : a.position.x > cut;
                bool insideB = rear ? b.position.x <= cut : b.position.x > cut;
                if (insideA)
                {
                    polygon.Add(a);
                }
                if (insideA != insideB)
                {
                    polygon.Add(Intersect(a, b, cut));
                }
            }
            return polygon;
        }

        private static Vertex Intersect(Vertex a, Vertex b, float cut)
        {
            return Vertex.Lerp(a, b, (cut - a.position.x) / (b.position.x - a.position.x));
        }

        private static Vertex Shift(Vertex vertex, float offset)
        {
            vertex.position.x += offset;
            return vertex;
        }

        private static void AddPolygon(List<Vertex> polygon, float offset, List<Vertex> output, List<int> triangles)
        {
            for (int i = 1; i + 1 < polygon.Count; i++)
            {
                if (Vector3.Cross(polygon[i].position - polygon[0].position,
                    polygon[i + 1].position - polygon[0].position).sqrMagnitude < 1e-12f)
                {
                    continue;
                }
                int start = output.Count;
                output.Add(Shift(polygon[0], offset));
                output.Add(Shift(polygon[i], offset));
                output.Add(Shift(polygon[i + 1], offset));
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }
        }
    }
}
