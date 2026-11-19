using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class InventoryWorkspaceView : MonoBehaviour
{
    public TMP_Text Heading,Count;
    public GridLayoutGroup Filters,Grid;
    public ScrollRect Scroll;
    public bool FitFilterColumns=true;
}
