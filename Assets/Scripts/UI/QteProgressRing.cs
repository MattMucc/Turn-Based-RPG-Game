using UnityEngine;
using UnityEngine.UI;

// Standard UI Images make the arc visible through the same renderer as its button icon.
public sealed class QteProgressRing : MonoBehaviour
{
    private const int Segments = 96;
    private Image[] track, fill;
    private float progress;
    private float innerRadius = -1;
    public float OuterRadius => innerRadius + 7;
    public void FitToIcon(float radius)
    {
        Ensure();
        if (Mathf.Abs(innerRadius - radius) < 0.01f) return;
        innerRadius = radius;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * 360f / Segments;
            var position = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * (radius + 3.5f);
            var size = new Vector2(2 * Mathf.PI * (radius + 3.5f) / Segments + 0.4f, 7);
            track[i].rectTransform.anchoredPosition = fill[i].rectTransform.anchoredPosition = position;
            track[i].rectTransform.sizeDelta = fill[i].rectTransform.sizeDelta = size;
        }
    }
    public Color color = new Color(0.3f, 1, 0.55f);
    public bool raycastTarget { get => false; set { } }
    public float Progress { get => progress; set { progress = Mathf.Clamp01(value); Ensure(); Draw(); } }
    private void OnEnable() { Ensure(); Draw(); }
    private void Ensure()
    {
        if (track != null) return;
        track = new Image[Segments]; fill = new Image[Segments];
        var rect = (RectTransform)transform;
        float radius = Mathf.Max(10, Mathf.Min(rect.rect.width, rect.rect.height) * 0.5f - 4);
        for (int i = 0; i < Segments; i++)
        {
            track[i] = Segment("Track " + i, i, radius);
            track[i].color = new Color(0.18f, 0.20f, 0.24f, 1);
            fill[i] = Segment("Fill " + i, i, radius);
        }
    }
    private Image Segment(string name, int index, float radius)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(transform, false);
        var rect = (RectTransform)obj.transform;
        float angle = index * 360f / Segments;
        rect.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * radius;
        rect.localRotation = Quaternion.Euler(0, 0, -angle);
        rect.sizeDelta = new Vector2(2 * Mathf.PI * radius / Segments + 0.4f, 7);
        var image = obj.GetComponent<Image>(); image.raycastTarget = false; return image;
    }
    private void Draw()
    {
        if (fill == null) return;
        for (int i = 0; i < Segments; i++)
        {
            fill[i].color = color;
            fill[i].enabled = i < Mathf.CeilToInt(progress * Segments);
        }
    }
}
