using UnityEngine;
using TMPro;
public sealed class ControllerGuideView : MonoBehaviour {
 public TMP_Text Text;
 [TextArea(4,12)] public string CursorText="Cursor mode\nView: free flight\nLT + D-pad: panels\nL3: focus";
 [TextArea(4,12)] public string FlightText="Free flight\nView: cursor\nRT + right stick: height\nL3: focus";
 [TextArea(4,12)] public string NavigationText="{area}\nLT + D-pad: panels\nLeft stick: cursor\nView: free flight";
 public void Present(int mode,string area){Text.text=ControllerGlyphs.Adapt((mode==0?CursorText:mode==1?FlightText:NavigationText).Replace("{area}",area));}
}
