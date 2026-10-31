using UnityEngine;
using UnityEngine.Rendering;

namespace Tenebrasia.GridPrototype
{
    // Camera-facing procedural impact flash; no particle package or texture dependency.
    public sealed class ParryBurst : MonoBehaviour
    {
        private readonly LineRenderer[] sparks = new LineRenderer[16];
        private LineRenderer ring;
        private Material gold, white;
        private Vector3 right, up;
        private float age;
        private const float Lifetime = 0.45f;

        public void Initialize(Vector3 center, Vector3 normal, Shader shader)
        {
            transform.position = center;
            var facing = Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.forward);
            right = facing * Vector3.right; up = facing * Vector3.up;
            gold = new Material(shader); white = new Material(shader);
            gold.renderQueue = white.renderQueue = 3100;
            ring = MakeLine("Parry flash ring", white, 33);
            for (int i = 0; i < sparks.Length; i++) sparks[i] = MakeLine("Parry spark", gold, 2);
            Draw();
        }
        private LineRenderer MakeLine(string name, Material material, int count)
        {
            var obj = new GameObject(name); obj.transform.SetParent(transform, false);
            var line = obj.AddComponent<LineRenderer>(); line.useWorldSpace = false;
            line.sharedMaterial = material; line.positionCount = count;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }
        private void Update()
        {
            if (!Application.isFocused) return;
            age += Time.unscaledDeltaTime;
            if (age >= Lifetime) { Destroy(gameObject); return; }
            Draw();
        }
        private void Draw()
        {
            float t = Mathf.Clamp01(age / Lifetime), fade = 1 - t;
            gold.color = new Color(1, 0.68f, 0.12f, fade); white.color = new Color(1, 0.97f, 0.8f, fade);
            ring.widthMultiplier = Mathf.Lerp(0.10f, 0.01f, t);
            for (int i = 0; i < 33; i++)
            {
                float angle = i * Mathf.PI * 2 / 32;
                ring.SetPosition(i, (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * Mathf.Lerp(0.18f, 0.85f, t));
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / sparks.Length + 0.17f;
                Vector3 direction = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                float distance = Mathf.Lerp(0.15f, 1.0f + (i % 3) * 0.08f, t);
                sparks[i].SetPosition(0, direction * distance);
                sparks[i].SetPosition(1, direction * (distance + 0.36f * (1 - t)));
                sparks[i].widthMultiplier = 0.065f * (1 - t);
            }
        }
        private void OnDestroy()
        {
            foreach (var material in new[] { gold, white })
                if (material) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
