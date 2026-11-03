using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed class SampledGridOverlay : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<float> originalAlphas = new();
        public void SetOpacity(float alpha)
        {
            for (int i = 0; i < materials.Count; i++)
            { var color = materials[i].color; color.a = originalAlphas[i] * Mathf.Clamp01(alpha); materials[i].color = color; }
        }
        private readonly Dictionary<Vector2Int, Renderer> fills = new Dictionary<Vector2Int, Renderer>();
        private TerrainBattleGrid grid;
        private Material movement, reach, affected;
        private LineRenderer outline, route;

        public void Build(TerrainBattleGrid source, Shader shader)
        {
            grid = source;
            Material white = MakeMaterial(shader, Color.white), green = MakeMaterial(shader, new Color(0.1f, 1, 0.2f, 0.3f));
            movement = green;
            reach = MakeMaterial(shader, new Color(0.15f, 0.55f, 1, 0.25f));
            affected = MakeMaterial(shader, new Color(1, 0.08f, 0.06f, 0.5f));
            white.renderQueue = 3010;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int x = 0; x < grid.Board.Width; x++) for (int z = 0; z < grid.Board.Depth; z++)
            {
                if (!grid.HasSurface[x, z] || grid.Board.Cells[x, z].Ground == Ground.Obstacle) continue;
                var corners = grid.Corners[x, z];
                for (int i = 0; i < 4; i++)
                {
                    Vector3 a = corners[i] + Vector3.up * 0.05f, b = corners[(i + 1) % 4] + Vector3.up * 0.05f;
                    Vector3 side = Vector3.Cross((b - a).normalized, Vector3.up) * 0.035f;
                    AddQuad(vertices, triangles, a - side, b - side, b + side, a + side);
                }
                var face = new List<Vector3>(); var indices = new List<int>();
                AddQuad(face, indices, corners[0] + Vector3.up * 0.03f, corners[1] + Vector3.up * 0.03f,
                    corners[2] + Vector3.up * 0.03f, corners[3] + Vector3.up * 0.03f);
                var renderer = MakeMesh("Reachable cell", face, indices, green);
                renderer.enabled = false; fills[new Vector2Int(x, z)] = renderer;
            }
            MakeMesh("Terrain grid outlines", vertices, triangles, white);
            outline = MakeLine("Selected cell", MakeMaterial(shader, Color.yellow), true);
            route = MakeLine("Movement route", MakeMaterial(shader, Color.yellow), false);
        }

        public void Show(Dictionary<Vector2Int, GridRoute> reachable, Vector2Int selected)
        {
            foreach (var fill in fills) { fill.Value.sharedMaterial = movement; fill.Value.enabled = reachable.ContainsKey(fill.Key); }
            outline.positionCount = 0; route.positionCount = 0;
            if (grid.Board.Contains(selected) && grid.HasSurface[selected.x, selected.y])
            {
                outline.positionCount = 4;
                var corners = grid.Corners[selected.x, selected.y];
                for (int i = 0; i < 4; i++) outline.SetPosition(i, corners[i] + Vector3.up * 0.08f);
            }
            if (!reachable.TryGetValue(selected, out var path)) return;
            var points = new List<Vector3>();
            for (int i = 1; i < path.Stops.Count; i++)
                for (int step = 0; step <= 8; step++)
                    points.Add(grid.RoutePoint(path.Stops[i - 1], path.Stops[i], step / 8f) + Vector3.up * 0.15f);
            route.positionCount = points.Count; route.SetPositions(points.ToArray());
        }

        public void ShowTargeting(HashSet<Vector2Int> range, HashSet<Vector2Int> impact, Vector2Int cursor)
        {
            outline.positionCount = route.positionCount = 0;
            if (grid.Board.Contains(cursor) && grid.HasSurface[cursor.x, cursor.y])
            {
                outline.positionCount = 4;
                var corners = grid.Corners[cursor.x, cursor.y];
                for (int i = 0; i < 4; i++) outline.SetPosition(i, corners[i] + Vector3.up * 0.08f);
            }
            foreach (var fill in fills)
            {
                fill.Value.enabled = range.Contains(fill.Key) || impact.Contains(fill.Key);
                fill.Value.sharedMaterial = impact.Contains(fill.Key) ? affected : reach;
            }
        }

        public void ShowApproach(GridRoute path)
        {
            var points = new List<Vector3>();
            for (int i = 1; i < path.Stops.Count; i++) for (int step = 0; step <= 8; step++)
                points.Add(grid.RoutePoint(path.Stops[i - 1], path.Stops[i], step / 8f) + Vector3.up * .15f);
            route.positionCount = points.Count; route.SetPositions(points.ToArray());
        }

        private void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int n = vertices.Count; vertices.AddRange(new[] { a, b, c, d });
            triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
        }
        private Renderer MakeMesh(string label, List<Vector3> vertices, List<int> triangles, Material material)
        {
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); meshes.Add(mesh);
            var obj = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(transform, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material; return renderer;
        }
        private Material MakeMaterial(Shader shader, Color color)
        { var m = new Material(shader); m.color = color; materials.Add(m); originalAlphas.Add(color.a); return m; }
        private LineRenderer MakeLine(string label, Material material, bool loop)
        {
            var line = new GameObject(label).AddComponent<LineRenderer>(); line.transform.SetParent(transform);
            line.sharedMaterial = material; line.loop = loop; line.positionCount = 0; line.startWidth = line.endWidth = 0.07f;
            return line;
        }
        private void OnDestroy()
        {
            foreach (var mesh in meshes) if (mesh) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            foreach (var material in materials) if (material) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
