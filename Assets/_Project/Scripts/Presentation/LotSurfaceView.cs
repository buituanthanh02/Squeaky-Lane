using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
using static SqueakyLane.Presentation.UnityObjects;

namespace SqueakyLane.Presentation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class LotSurfaceView : MonoBehaviour
    {
        public const float Top = -.01f; // Mặt bãi của 010; không đổi gốc tọa độ xe.
        private Mesh ownedMesh;
        public void Configure(LevelDef level, MechanicCatalog.Design design)
        {
            if (ownedMesh != null) { Release(ownedMesh); }
            ownedMesh = BuildMesh(level, design);
            GetComponent<MeshFilter>().sharedMesh = ownedMesh;
        }
        // Một mesh / ba submesh dùng ba material asset: nhựa đường, vạch, bó vỉa.
        public static Mesh BuildMesh(LevelDef level, MechanicCatalog.Design d)
        {
            var vertices = new List<Vector3>();
            var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
            AddBox(vertices, triangles[0], new Vector3(0, Top - d.surfaceThickness * .5f, 0), new Vector3(level.Cols, d.surfaceThickness, level.Rows));
            float y = Top + d.overlayLift;
            for (int c = 1; c < level.Cols; c++)
            {
                AddBox(vertices, triangles[1], new Vector3(c - level.Cols * .5f, y, 0), new Vector3(d.lineWidth, d.overlayLift, level.Rows));
            }
            for (int r = 1; r < level.Rows; r++)
            {
                AddBox(vertices, triangles[1], new Vector3(0, y, r - level.Rows * .5f), new Vector3(level.Cols, d.overlayLift, d.lineWidth));
            }
            for (int i = 0; i < level.Cols; i++)
            {
                Curb(level, d, GateEdge.Top, i, vertices, triangles[2]);
                Curb(level, d, GateEdge.Bottom, i, vertices, triangles[2]);
            }
            for (int i = 0; i < level.Rows; i++)
            {
                Curb(level, d, GateEdge.Left, i, vertices, triangles[2]);
                Curb(level, d, GateEdge.Right, i, vertices, triangles[2]);
            }
            foreach (int x in new[] { -1, 1 })
            {
                foreach (int z in new[] { -1, 1 })
                {
                    AddBox(vertices, triangles[2], new Vector3(x * (level.Cols + d.curbWidth) * .5f,
                        Top + d.curbHeight * .5f, z * (level.Rows + d.curbWidth) * .5f),
                        new Vector3(d.curbWidth, d.curbHeight, d.curbWidth));
                }
            }
            var mesh = new Mesh { name = "Bãi + vạch + bó vỉa", indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 3;
            for (int i = 0; i < 3; i++) { mesh.SetTriangles(triangles[i], i); }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
        private static void Curb(LevelDef level, MechanicCatalog.Design d, GateEdge edge, int i, List<Vector3> vertices, List<int> indices)
        {
            if (level.GateAt(edge, i).HasValue) { return; }
            bool horizontal = edge == GateEdge.Top || edge == GateEdge.Bottom;
            float x = horizontal ? i - (level.Cols - 1) * .5f : (edge == GateEdge.Left ? -1 : 1) * (level.Cols * .5f + d.curbWidth * .5f);
            float z = horizontal ? (edge == GateEdge.Top ? 1 : -1) * (level.Rows * .5f + d.curbWidth * .5f) : (level.Rows - 1) * .5f - i;
            AddBox(vertices, indices, new Vector3(x, Top + d.curbHeight * .5f, z),
                horizontal ? new Vector3(1, d.curbHeight, d.curbWidth) : new Vector3(d.curbWidth, d.curbHeight, 1));
        }
        private static void AddBox(List<Vector3> vertices, List<int> indices, Vector3 center, Vector3 size)
        {
            var a = center - size * .5f;
            var b = center + size * .5f;
            Quad(vertices, indices, new Vector3(a.x, b.y, a.z), new Vector3(a.x, b.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, b.y, a.z));
            Quad(vertices, indices, new Vector3(a.x, a.y, b.z), new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z));
            Quad(vertices, indices, new Vector3(a.x, a.y, a.z), new Vector3(a.x, a.y, b.z), new Vector3(a.x, b.y, b.z), new Vector3(a.x, b.y, a.z));
            Quad(vertices, indices, new Vector3(b.x, a.y, b.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, b.y, b.z));
            Quad(vertices, indices, new Vector3(a.x, a.y, b.z), new Vector3(b.x, a.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z));
            Quad(vertices, indices, new Vector3(b.x, a.y, a.z), new Vector3(a.x, a.y, a.z), new Vector3(a.x, b.y, a.z), new Vector3(b.x, b.y, a.z));
        }
        private static void Quad(List<Vector3> vertices, List<int> indices, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
            indices.Add(start); indices.Add(start + 2); indices.Add(start + 3);
        }
        private void OnDestroy() { if (ownedMesh != null) { Release(ownedMesh); } }
    }
}
