using TMPro;
using UnityEngine;
using UnityEngine.UI;

// References here are authored prefab slots, not runtime-only references.
public sealed class DetailsSectionView : MonoBehaviour
{
    public RectTransform Content;
    [Tooltip("Editable text template. Hidden at runtime; cloned for each bound text row.")]
    public TMP_Text TextTemplate;
    [Min(0)] public float RowSpacing=6;
    public RectOffset Padding;
    void OnEnable(){Padding??=new RectOffset(0,0,0,0);}
    [Min(16)] public float ArtworkSize=112;
}
