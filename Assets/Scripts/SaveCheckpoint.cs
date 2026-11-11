using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(BoxCollider),typeof(SaveIdentity))]
public sealed class SaveCheckpoint : MonoBehaviour
{
    public string DisplayName="Checkpoint";
    [Min(1)] public float InteractionRange=3;
    [Min(1)] public float SafeRadius=5;
    Canvas promptCanvas;RectTransform prompt;Image glyph;TMP_Text caption;
    void Reset(){GetComponent<BoxCollider>().isTrigger=true;}
    public bool InRange(Transform actor)=>actor&&Vector3.Distance(actor.position,transform.position)<=Mathf.Max(1,InteractionRange);
    public static bool InSafeArea(Vector3 position)=>FindObjectsByType<SaveCheckpoint>(FindObjectsSortMode.None).Any(c=>c.isActiveAndEnabled&&new Vector2(position.x-c.transform.position.x,position.z-c.transform.position.z).sqrMagnitude<Mathf.Pow(Mathf.Max(1,c.SafeRadius),2));
    public static SaveCheckpoint Nearest(Transform actor)=>!actor?null:FindObjectsByType<SaveCheckpoint>(FindObjectsSortMode.None).Where(c=>c.isActiveAndEnabled&&c.InRange(actor)).OrderBy(c=>(c.transform.position-actor.position).sqrMagnitude).FirstOrDefault();
    public bool CanInteract=>GameManager.Instance&&GameManager.Instance.PossessedCharacter&&InRange(GameManager.Instance.PossessedCharacter.transform)&&!GameManager.Instance.WorldInputBlocked&&!(BattleManager.Instance&&BattleManager.Instance.InBattle);
    void Update()
    {
        bool visible=CanInteract&&Nearest(GameManager.Instance.PossessedCharacter.transform)==this;
        if(visible&&GameManager.Instance.Input&&GameManager.Instance.Input.InteractPressed)GameManager.Instance.ProgressionMenu.OpenCheckpoint(this);
        if(!Application.isPlaying)return;
        if(!visible){if(promptCanvas)promptCanvas.gameObject.SetActive(false);return;}
        if(!promptCanvas)BuildPrompt();promptCanvas.gameObject.SetActive(true);
        var camera=Camera.main;if(!camera){promptCanvas.gameObject.SetActive(false);return;}
        var screen=camera.WorldToScreenPoint(transform.position+Vector3.up*2);if(screen.z<=0){promptCanvas.gameObject.SetActive(false);return;}
        prompt.position=screen;
        bool controller=GameManager.Instance.Input&&GameManager.Instance.Input.CurrentScheme==PlayerInputReader.ControlScheme.Gamepad;var catalog=ControllerGlyphs.Catalog;
        glyph.sprite=controller&&catalog&&catalog.Buttons!=null&&catalog.Buttons.Length>3?ControllerGlyphs.Resolve(catalog.Buttons[3].Xbox,out _):!controller&&catalog&&catalog.OverworldKeys!=null&&catalog.OverworldKeys.Length>1?catalog.OverworldKeys[1]:null;
        glyph.material=null;glyph.color=Color.white;glyph.enabled=glyph.sprite;if(controller&&glyph.sprite)ControllerGlyphs.StyleFace(glyph);
        caption.text=(glyph.sprite?"":controller?(ControllerGlyphs.PlayStation?"Triangle · ":"Y · "):"E · ")+DisplayName;
    }
    void BuildPrompt()
    {
        var go=new GameObject("Checkpoint interaction prompt",typeof(Canvas));go.transform.SetParent(transform,false);promptCanvas=go.GetComponent<Canvas>();promptCanvas.renderMode=RenderMode.ScreenSpaceOverlay;promptCanvas.sortingOrder=120;
        var panel=new GameObject("Prompt",typeof(RectTransform),typeof(Image));panel.transform.SetParent(go.transform,false);prompt=panel.GetComponent<RectTransform>();prompt.sizeDelta=new Vector2(220,38);var background=panel.GetComponent<Image>();background.color=new Color(.025f,.05f,.08f,.85f);background.raycastTarget=false;
        var icon=new GameObject("Interact icon",typeof(RectTransform),typeof(Image));icon.transform.SetParent(panel.transform,false);var ir=icon.GetComponent<RectTransform>();ir.sizeDelta=new Vector2(28,28);ir.anchoredPosition=new Vector2(-88,0);glyph=icon.GetComponent<Image>();glyph.preserveAspect=true;glyph.raycastTarget=false;
        var text=new GameObject("Checkpoint name",typeof(RectTransform));text.transform.SetParent(panel.transform,false);var tr=text.GetComponent<RectTransform>();tr.sizeDelta=new Vector2(170,32);tr.anchoredPosition=new Vector2(20,0);caption=text.AddComponent<TextMeshProUGUI>();caption.font=TMP_Settings.defaultFontAsset;caption.fontSize=15;caption.alignment=TextAlignmentOptions.MidlineLeft;caption.raycastTarget=false;
    }
    void OnDisable(){if(promptCanvas)promptCanvas.gameObject.SetActive(false);}
    void OnDrawGizmos(){Gizmos.color=new Color(.3f,.8f,1);Gizmos.DrawWireSphere(transform.position,Mathf.Max(1,SafeRadius));}
}
