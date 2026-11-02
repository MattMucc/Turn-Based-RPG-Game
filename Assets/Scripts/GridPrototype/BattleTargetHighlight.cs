using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.GridPrototype
{
    public sealed class BattleTargetHighlight : MonoBehaviour
    {
        private BattleTarget current;
        private TerrainBattleGrid grid;
        private Material outlineMaterial, circleMaterial;
        private Mesh disc;
        private GameObject circle;
        private readonly List<(Renderer source, Renderer copy)> copies = new();
        public void Initialize(Shader overlayShader)
        {
            var outlineShader = Resources.Load<Shader>("BattleTargetOutline");
            outlineMaterial = new Material(outlineShader ? outlineShader : overlayShader); outlineMaterial.color = Color.red;
            circleMaterial = new Material(overlayShader); circleMaterial.color = new Color(1, 0.03f, 0.02f, 0.32f);
            const int segments = 48; var vertices = new Vector3[segments + 1]; var triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments; vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            disc = new Mesh(); disc.vertices = vertices; disc.triangles = triangles; disc.RecalculateBounds();
            circle = new GameObject("Red target circle", typeof(MeshFilter), typeof(MeshRenderer)); circle.transform.SetParent(transform);
            circle.GetComponent<MeshFilter>().sharedMesh = disc; circle.GetComponent<MeshRenderer>().sharedMaterial = circleMaterial;
            circle.SetActive(false);
        }
        public void Show(BattleTarget target, TerrainBattleGrid terrain)
        {
            grid = terrain;
            if (current != target)
            {
                foreach (var pair in copies) if (pair.copy) Release(pair.copy.gameObject);
                copies.Clear(); current = target;
                if (target?.Alive == true) foreach (var source in target.Renderers)
                {
                    if (!source || !source.enabled) continue;
                    var obj = new GameObject("Red silhouette"); obj.transform.SetParent(transform); Renderer copy;
                    if (source is SkinnedMeshRenderer skin)
                    {
                        var skinned = obj.AddComponent<SkinnedMeshRenderer>(); skinned.sharedMesh = skin.sharedMesh;
                        skinned.bones = skin.bones; skinned.rootBone = skin.rootBone; skinned.localBounds = skin.localBounds;
                        skinned.updateWhenOffscreen = true; copy = skinned;
                    }
                    else if (source is MeshRenderer && source.TryGetComponent<MeshFilter>(out var filter))
                    { obj.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh; copy = obj.AddComponent<MeshRenderer>(); }
                    else { Release(obj); continue; }
                    var materials = new Material[Mathf.Max(1, source.sharedMaterials.Length)];
                    for (int i = 0; i < materials.Length; i++) materials[i] = outlineMaterial;
                    copy.sharedMaterials = materials; copy.shadowCastingMode = ShadowCastingMode.Off; copy.receiveShadows = false;
                    copies.Add((source, copy));
                }
            }
            UpdateGeometry();
        }
        private void LateUpdate() => UpdateGeometry();
        private void UpdateGeometry()
        {
            if (!circle) return;
            bool visible = current?.Alive == true; circle.SetActive(visible);
            foreach (var pair in copies)
            {
                if (!pair.copy) continue;
                pair.copy.enabled = visible && pair.source && pair.source.enabled;
                if (!pair.copy.enabled) continue;
                pair.copy.transform.SetPositionAndRotation(pair.source.transform.position, pair.source.transform.rotation);
                pair.copy.transform.localScale = pair.source.transform.lossyScale;
                if (pair.source is SkinnedMeshRenderer source && pair.copy is SkinnedMeshRenderer copy && source.sharedMesh)
                    for (int i = 0; i < source.sharedMesh.blendShapeCount; i++) copy.SetBlendShapeWeight(i, source.GetBlendShapeWeight(i));
            }
            if (!visible) return;
            var bounds = current.Bounds; float ground = bounds.min.y;
            if (grid.TryCell(bounds.center, out var cell)) ground = Mathf.Min(ground, grid.Point(cell).y);
            circle.transform.position = new Vector3(bounds.center.x, ground + 0.06f, bounds.center.z);
            float radius = Mathf.Max(0.35f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.1f);
            circle.transform.localScale = new Vector3(radius, 1, radius);
        }
        private static void Release(Object obj)
        { if (obj is GameObject go) go.SetActive(false); if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        private void OnDestroy()
        { if (outlineMaterial) Release(outlineMaterial); if (circleMaterial) Release(circleMaterial); if (disc) Release(disc); }
    }
}
