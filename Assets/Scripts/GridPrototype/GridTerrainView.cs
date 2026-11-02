using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    // Terrain geometry and tactical overlays are separate renderers. A future authored
    // terrain can replace the generated mesh without becoming a collection of tiles.
    public sealed class GridTerrainView : MonoBehaviour
    {
        public MeshCollider TerrainCollider { get; private set; }
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly Dictionary<Vector2Int, Renderer> fills = new Dictionary<Vector2Int, Renderer>();
        private LineRenderer selection;
        private GridBoard board;
        private float size, elevation;

        private sealed class Geometry
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int n = Vertices.Count; Vertices.AddRange(new[] { a, b, c, d });
                Triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            }
        }

        public void Build(GridBoard model, float cellSize, float heightStep, Material surface, Shader overlayShader)
        {
            board = model; size = cellSize; elevation = heightStep;
            var terrain = new Geometry();
            var tops = new[] { new List<int>(), new List<int>(), new List<int>() };
            var grid = new Geometry();
            Material green = Overlay(overlayShader, new Color(0.15f, 1, 0.3f, 0.32f));
            Material white = Overlay(overlayShader, Color.white); white.renderQueue = 3010;
            for (int x = 0; x < board.Width; x++) for (int z = 0; z < board.Depth; z++)
            {
                var p = new Vector2Int(x, z);
                float y = SurfaceHeight(p), half = size * 0.5f;
                Vector3 a = new Vector3(x * size - half, y, z * size - half);
                Vector3 b = a + Vector3.forward * size, c = b + Vector3.right * size, d = a + Vector3.right * size;
                int start = terrain.Triangles.Count;
                terrain.Quad(a, b, c, d);
                int type = board.At(p).Ground == Ground.Water ? 1 : board.At(p).Ground == Ground.Hazard ? 2 : 0;
                tops[type].AddRange(terrain.Triangles.GetRange(start, 6));
                AddWall(terrain, tops[0], a, b, x == 0 ? -1 : SurfaceHeight(new Vector2Int(x - 1, z)));
                AddWall(terrain, tops[0], b, c, z == board.Depth - 1 ? -1 : SurfaceHeight(new Vector2Int(x, z + 1)));
                AddWall(terrain, tops[0], c, d, x == board.Width - 1 ? -1 : SurfaceHeight(new Vector2Int(x + 1, z)));
                AddWall(terrain, tops[0], d, a, z == 0 ? -1 : SurfaceHeight(new Vector2Int(x, z - 1)));
                var raised = Vector3.up * 0.035f;
                AddStrip(grid, a + raised, b + raised);
                AddStrip(grid, b + raised, c + raised);
                AddStrip(grid, c + raised, d + raised);
                AddStrip(grid, d + raised, a + raised);
                var fill = new Geometry();
                fill.Quad(a + Vector3.up * 0.02f, b + Vector3.up * 0.02f, c + Vector3.up * 0.02f, d + Vector3.up * 0.02f);
                fills[p] = MeshObject($"Reachable {x},{z}", ToMesh(fill), new[] { green }).GetComponent<Renderer>();
                fills[p].enabled = false;
            }
            Mesh terrainMesh = ToMesh(terrain);
            terrainMesh.subMeshCount = 3;
            for (int i = 0; i < 3; i++) terrainMesh.SetTriangles(tops[i], i);
            var terrainObject = MeshObject("Continuous terrain", terrainMesh, new[] {
                Surface(surface, new Color(0.3f, 0.37f, 0.3f)), Surface(surface, new Color(0.08f, 0.3f, 0.55f)),
                Surface(surface, new Color(0.85f, 0.28f, 0.06f)) });
            TerrainCollider = terrainObject.AddComponent<MeshCollider>(); TerrainCollider.sharedMesh = terrainMesh;
            MeshObject("White grid overlay", ToMesh(grid), new[] { white });
            selection = new GameObject("Selected cell outline").AddComponent<LineRenderer>();
            selection.transform.SetParent(transform); selection.loop = true; selection.positionCount = 4;
            selection.startWidth = selection.endWidth = 0.055f;
            selection.sharedMaterial = Overlay(overlayShader, new Color(1, 0.8f, 0.1f));
            selection.sharedMaterial.renderQueue = 3020;
        }

        public float SurfaceHeight(Vector2Int p) => board.At(p).Height * elevation
            + (board.At(p).Ground == Ground.Water ? -0.15f : 0);

        public bool TryCell(Vector3 world, out Vector2Int cell)
        {
            Vector3 p = transform.InverseTransformPoint(world);
            cell = new Vector2Int(Mathf.FloorToInt(p.x / size + 0.5f), Mathf.FloorToInt(p.z / size + 0.5f));
            return board.Contains(cell);
        }

        public void Highlight(Dictionary<Vector2Int, GridRoute> reachable, Vector2Int selected)
        {
            foreach (var fill in fills) fill.Value.enabled = reachable.ContainsKey(fill.Key);
            Vector3 center = new Vector3(selected.x * size, SurfaceHeight(selected) + 0.065f, selected.y * size);
            float half = size * 0.48f;
            selection.SetPositions(new[] { center + new Vector3(-half, 0, -half), center + new Vector3(-half, 0, half),
                center + new Vector3(half, 0, half), center + new Vector3(half, 0, -half) });
        }

        private void AddWall(Geometry g, List<int> triangles, Vector3 a, Vector3 b, float bottom)
        {
            if (bottom >= a.y) return;
            int start = g.Triangles.Count;
            g.Quad(a, new Vector3(a.x, bottom, a.z), new Vector3(b.x, bottom, b.z), b);
            triangles.AddRange(g.Triangles.GetRange(start, 6));
        }

        private void AddStrip(Geometry g, Vector3 a, Vector3 b)
        {
            Vector3 side = Vector3.Cross((b - a).normalized, Vector3.up) * 0.04f;
            g.Quad(a - side, b - side, b + side, a + side);
        }

        private Mesh ToMesh(Geometry geometry)
        {
            var mesh = new Mesh(); mesh.SetVertices(geometry.Vertices); mesh.SetTriangles(geometry.Triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh); return mesh;
        }

        private GameObject MeshObject(string label, Mesh mesh, Material[] surfaces)
        {
            var obj = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(transform, false); obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterials = surfaces; return obj;
        }

        private Material Surface(Material source, Color color)
        { var m = new Material(source); m.color = color; materials.Add(m); return m; }
        private Material Overlay(Shader shader, Color color)
        { var m = new Material(shader); m.color = color; materials.Add(m); return m; }
        private void OnDestroy()
        {
            foreach (var mesh in meshes) if (mesh) Destroy(mesh);
            foreach (var material in materials) if (material) Destroy(material);
        }
    }
}
