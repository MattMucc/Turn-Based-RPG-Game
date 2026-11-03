using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BattlePortrait : MonoBehaviour, IPointerClickHandler
{
    public System.Action LeftClick;
    public System.Action<Vector2> RightClick;
    public void OnPointerClick(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left) LeftClick?.Invoke();
        if (data.button == PointerEventData.InputButton.Right) RightClick?.Invoke(data.position);
    }
}

// Lightweight model-only copies: never clone scripts, colliders, or gameplay objects.
public sealed class BattlePortraitCapture : MonoBehaviour
{
    private readonly List<Object> owned = new();
    public Texture Capture(Transform actor, int slot)
    {
        if (!Application.isPlaying) return null;
        var stage = new GameObject("Portrait model"); owned.Add(stage);
        stage.transform.position = new Vector3(50000 + slot * 100, 10000, 50000);
        bool found = false; Bounds bounds = default;
        foreach (var source in actor.GetComponentsInChildren<Renderer>(true))
        {
            if (!source.enabled) continue;
            Mesh mesh = null;
            if (source is SkinnedMeshRenderer skin)
            { mesh = new Mesh(); skin.BakeMesh(mesh, false); owned.Add(mesh); }
            else if (source is MeshRenderer && source.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
            if (!mesh) continue;
            var model = new GameObject("Portrait mesh", typeof(MeshFilter), typeof(MeshRenderer)); model.layer = 31;
            model.transform.SetParent(stage.transform, false);
            model.transform.localPosition = Quaternion.Inverse(actor.rotation) * (source.transform.position - actor.position);
            model.transform.localRotation = Quaternion.Inverse(actor.rotation) * source.transform.rotation;
            model.transform.localScale = source.transform.lossyScale;
            model.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = model.GetComponent<MeshRenderer>(); renderer.sharedMaterials = source.sharedMaterials;
            if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
        }
        if (!found) return null;
        var texture = new RenderTexture(128, 128, 24); texture.Create(); owned.Add(texture);
        var obj = new GameObject("Portrait camera", typeof(Camera)); obj.transform.SetParent(stage.transform);
        var camera = obj.GetComponent<Camera>(); camera.targetTexture = texture; camera.cullingMask = 1 << 31;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.14f, 0.17f, 0.22f);
        camera.orthographic = true; camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x) * 1.1f;
        float distance = Mathf.Max(3, bounds.size.magnitude * 2);
        camera.nearClipPlane = 0.01f; camera.farClipPlane = distance * 3;
        camera.transform.position = bounds.center + Vector3.forward * distance; camera.transform.LookAt(bounds.center);
        var lamp = new GameObject("Portrait light", typeof(Light)); lamp.transform.SetParent(stage.transform);
        lamp.transform.position = camera.transform.position; var light = lamp.GetComponent<Light>();
        light.type = LightType.Point; light.range = distance * 3; light.intensity = 3; light.cullingMask = 1 << 31;
        StartCoroutine(Freeze(camera, stage)); return texture;
    }
    private IEnumerator Freeze(Camera camera, GameObject stage)
    { yield return new WaitForEndOfFrame(); if (camera) camera.enabled = false; if (stage) stage.SetActive(false); }
    private void OnDestroy()
    {
        foreach (var obj in owned) if (obj)
        { if (obj is RenderTexture texture) texture.Release(); if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
    }
}
