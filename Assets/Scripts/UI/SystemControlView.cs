using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class SystemControlView:MonoBehaviour {
    [Tooltip("Stable binding key. Keep this when renaming or rearranging the object.")] public string Key;
    public TMP_Text Text;
    public Button Button;
    public TMP_Dropdown Dropdown;
    public Slider Slider;
}
