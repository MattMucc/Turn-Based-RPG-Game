using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerShortcutGlyph : MonoBehaviour
{
    Image key, trigger;
    TMP_Text plus, fallback;
    int slot;
    bool initialized, previousController, previousSony;
    public void Bind(int index, Image keyImage, Image triggerImage, TMP_Text separator, TMP_Text fallbackText)
    { slot=index;key=keyImage;trigger=triggerImage;plus=separator;fallback=fallbackText;Refresh(false); }
    void Update()
    {
        bool controller=GameManager.Instance && GameManager.Instance.Input && GameManager.Instance.Input.CurrentScheme==PlayerInputReader.ControlScheme.Gamepad;
        if(!initialized||controller!=previousController||ControllerGlyphs.PlayStation!=previousSony)Refresh(controller);
    }
    public void Refresh(bool controller)
    {
        initialized=true;previousController=controller;previousSony=ControllerGlyphs.PlayStation;
        var catalog=ControllerGlyphs.Catalog;key.material=null;key.color=Color.white;trigger.material=null;trigger.color=Color.white;
        key.sprite=controller && catalog && catalog.Buttons.Length>3 ? ControllerGlyphs.Resolve(catalog.Buttons[new[]{2,3,0,1}[slot]].Xbox,out _) :
            !controller && catalog && catalog.ShortcutKeys!=null && catalog.ShortcutKeys.Length>slot ? catalog.ShortcutKeys[slot] : null;
        trigger.sprite=catalog && catalog.Buttons.Length>4 ? ControllerGlyphs.Resolve(catalog.Buttons[4].Xbox,out _) : null;
        key.gameObject.SetActive(key.sprite);trigger.gameObject.SetActive(controller && trigger.sprite);plus.gameObject.SetActive(controller);
        fallback.gameObject.SetActive(!key.sprite);
        fallback.text=controller?(previousSony?new[]{"Square","Triangle","Cross","Circle"}[slot]:new[]{"X","Y","A","B"}[slot]):(slot+1).ToString();
        if(controller && key.sprite)ControllerGlyphs.StyleFace(key);
    }
}
