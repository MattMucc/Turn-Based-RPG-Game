using UnityEngine;
using UnityEngine.UI;

public sealed class QteTriangleCue : MonoBehaviour
{
    private GameObject outline, fill;
    private void Ensure()
    {
        if (outline) return;
        outline = new GameObject("Outlined triangle", typeof(RectTransform)); outline.transform.SetParent(transform, false);
        fill = new GameObject("Filled triangle", typeof(RectTransform)); fill.transform.SetParent(transform, false);
        Line(outline.transform, new Vector2(0, 0), new Vector2(-8, 12));
        Line(outline.transform, new Vector2(-8, 12), new Vector2(8, 12));
        Line(outline.transform, new Vector2(8, 12), new Vector2(0, 0));
        for (int row = 0; row < 12; row++)
        {
            var rect = Piece(fill.transform); rect.anchoredPosition = new Vector2(0, row + 0.5f);
            rect.sizeDelta = new Vector2(16 * (row + 1) / 12f, 1.1f);
        }
    }
    private RectTransform Piece(Transform parent)
    {
        var obj = new GameObject("Triangle stroke", typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<Image>(); image.color = Color.white; image.raycastTarget = false;
        return (RectTransform)obj.transform;
    }
    private void Line(Transform parent, Vector2 a, Vector2 b)
    {
        var rect = Piece(parent); rect.anchoredPosition = (a + b) * 0.5f;
        rect.sizeDelta = new Vector2(Vector2.Distance(a, b), 1.5f);
        rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
    }
    public void Present(bool holding, float outerRadius, float seconds, bool animate)
    {
        Ensure(); outline.SetActive(!holding); fill.SetActive(holding);
        float bob = holding || !animate ? 0 : (1 - Mathf.Cos(seconds * Mathf.PI * 3)) * 2.5f;
        ((RectTransform)transform).anchoredPosition = new Vector2(0, outerRadius + (holding ? 0 : 0.75f) + bob);
    }
}
