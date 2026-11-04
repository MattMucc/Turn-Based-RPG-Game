using System.Collections.Generic;
using Game.World;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(-80)]
[RequireComponent(typeof(WorldPlaceService))]
public sealed class WorldManipulationController : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    public bool BlocksWorldInput => IsOpen || closedFrame == Time.frameCount;
    WorldPlaceService service;
    WorldPlaceObject selected, hovered, grabbed;
    WorldPlaceSocket socket;
    bool magical, savedVisible, savedNavigation, padCursor, validDestination;
    CursorLockMode savedLock;
    int closedFrame=-1;
    Vector2 pointer;
    Vector3 destination;
    string previewReason="";
    GameObject ui, ownedEvents;
    RectTransform cursorRect;
    TMP_Text title, help, status;
    Button restoreButton, modeButton, dismissButton;
    readonly List<Image> buttonIcons=new();
    readonly int[] controllerButtons={2,3,5,1}; // X/Square, Y/Triangle, RT/R2, B/Circle.
    EventSystem events;
    readonly List<LineRenderer> lines=new();
    Material lineMaterial;
    GameManager Manager => GameManager.Instance;
    Transform Actor => Manager && Manager.PossessedCharacter ? Manager.PossessedCharacter.transform : null;
    PlaceWorldAbility Ability => Actor ? Actor.GetComponent<PlaceWorldAbility>() : null;
    int Strength => Actor && Actor.GetComponent<BattleCharacterInfo>() ? Actor.GetComponent<BattleCharacterInfo>().Stat(CharacterStat.Strength) : 0;
    bool InBattle => BattleManager.Instance && BattleManager.Instance.InBattle;
    void Awake() { service=GetComponent<WorldPlaceService>(); }
    void Update()
    {
        if (!service) service=GetComponent<WorldPlaceService>();
        if (InBattle) { Close(); return; }
        if (!Application.isFocused) { grabbed=null; return; }
        service.TickPendingReturn();
        if (!Manager || !Actor) return;
        var k=Keyboard.current; var pad=Gamepad.current; var mouse=Mouse.current;
        bool toggle=(k!=null && k.gKey.wasPressedThisFrame)||(pad!=null && pad.leftTrigger.wasPressedThisFrame);
        if(toggle) { if(IsOpen)Close(); else if(!Manager.WorldInputBlocked)Open(); return; }
        if(!IsOpen)return;
        if((k!=null && k.escapeKey.wasPressedThisFrame)||(pad!=null && pad.buttonEast.wasPressedThisFrame)){Close();return;}
        if(pad!=null && pad.leftStick.ReadValue().sqrMagnitude>.02f) {padCursor=true;pointer+=pad.leftStick.ReadValue()*800*Time.unscaledDeltaTime;}
        if(mouse!=null && mouse.delta.ReadValue().sqrMagnitude>0){padCursor=false;pointer=mouse.position.ReadValue();}
        pointer=new Vector2(Mathf.Clamp(pointer.x,0,Screen.width),Mathf.Clamp(pointer.y,0,Screen.height));
        Cursor.visible=!padCursor;
        cursorRect.gameObject.SetActive(padCursor); cursorRect.position=pointer;
        bool overUi=OverUi(out var button);
        bool down=padCursor ? pad!=null && pad.buttonSouth.wasPressedThisFrame : mouse!=null && mouse.leftButton.wasPressedThisFrame;
        bool up=padCursor ? pad!=null && pad.buttonSouth.wasReleasedThisFrame : mouse!=null && mouse.leftButton.wasReleasedThisFrame;
        if(down && overUi && padCursor && button && button.interactable)button.onClick.Invoke();
        if(!IsOpen)return;
        if(Manager.PlayerRotation)Manager.PlayerRotation.PreviewWorldCamera((padCursor && pad!=null && pad.rightStick.ReadValue().sqrMagnitude>.02f)||(!padCursor && mouse!=null && mouse.rightButton.isPressed));
        Pick(overUi);
        if(down && !overUi && hovered) {selected=hovered; grabbed=hovered;}
        if((k!=null && k.rKey.wasPressedThisFrame)||(pad!=null && pad.buttonWest.wasPressedThisFrame))Restore();
        if((k!=null && k.eKey.wasPressedThisFrame)||(pad!=null && pad.buttonNorth.wasPressedThisFrame))ToggleMode();
        if((k!=null && k.deleteKey.wasPressedThisFrame)||(pad!=null && pad.rightTrigger.wasPressedThisFrame))Dismiss();
        if(grabbed && up)
        {
            if(!overUi && validDestination)service.TryMove(grabbed,destination,Actor,Strength,Ability?Ability.Range:0,magical,socket);
            grabbed=null;
        }
        DrawHighlights(); Render();
    }
    public void Open()
    {
        if(IsOpen || InBattle || !Actor || Manager.WorldInputBlocked)return;
        if(!ui)Build();
        events=EventSystem.current;
        if(!events){ownedEvents=new GameObject("World interaction events",typeof(EventSystem),typeof(InputSystemUIInputModule));events=ownedEvents.GetComponent<EventSystem>();}
        savedNavigation=events.sendNavigationEvents;events.sendNavigationEvents=false;
        savedVisible=Cursor.visible;savedLock=Cursor.lockState;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);padCursor=Manager.Input && Manager.Input.CurrentScheme==PlayerInputReader.ControlScheme.Gamepad;
        IsOpen=true;ui.SetActive(true);Manager.PossessedCharacter.ResetMotion();Render();
    }
    public void Close()
    {
        if(!IsOpen)return;
        IsOpen=false;closedFrame=Time.frameCount;grabbed=hovered=selected=null;magical=false;
        if(ui)ui.SetActive(false);foreach(var line in lines)if(line)line.gameObject.SetActive(false);
        if(events)events.sendNavigationEvents=savedNavigation;
        Cursor.lockState=savedLock;Cursor.visible=savedVisible;
    }
    void Restore() { grabbed=null; var obj=hovered?hovered:selected; if(Ability && obj)service.TryRestore(obj,Actor,Ability.Range); }
    void ToggleMode() { grabbed=null; if(Ability)magical=!magical; }
    void Dismiss() { grabbed=null; if(Ability)service.TryDismiss(); }
    void Pick(bool overUi)
    {
        hovered=null;validDestination=false;socket=null;previewReason="Point at a supported destination.";
        if(grabbed)destination=grabbed.transform.position;
        if(overUi || !Camera.main)return;
        var ray=Camera.main.ScreenPointToRay(pointer);
        var hits=Physics.RaycastAll(ray,100,~0,QueryTriggerInteraction.Collide);
        System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits)
        {
            if(hit.collider.GetComponentInParent<BattleCharacterInfo>())continue;
            var obj=hit.collider.GetComponentInParent<WorldPlaceObject>();
            if(!grabbed)
            {
                if(obj)hovered=obj;
                if(obj || !hit.collider.isTrigger)break;
                continue;
            }
            if(obj==grabbed || hit.collider.isTrigger)continue;
            if(Vector3.Angle(hit.normal,Vector3.up)>35){previewReason="Choose a horizontal surface.";break;}
            var volume=grabbed.Volume(Vector3.zero);
            destination=hit.point-Vector3.up*volume.min.y;
            foreach(var candidate in FindObjectsByType<WorldPlaceSocket>(FindObjectsSortMode.None))
                if(candidate.Accepts(grabbed) && Vector3.Distance(destination,candidate.Position)<=candidate.SnapRadius){socket=candidate;destination=candidate.Position;break;}
            validDestination=service.PreviewMove(grabbed,destination,Actor,Strength,Ability?Ability.Range:0,magical,out previewReason);
            break;
        }
    }
    bool OverUi(out Button button)
    {
        button=null;if(!events)return false;
        var results=new List<RaycastResult>();events.RaycastAll(new PointerEventData(events){position=pointer},results);
        if(results.Count==0)return false;
        button=results[0].gameObject.GetComponentInParent<Button>();return true;
    }
    void Render()
    {
        var obj=hovered?hovered:selected;
        title.text=(Ability?"Place · ":"Objects · ")+(magical?"Relocate":"Drag")+(obj?" — "+obj.DisplayName:"");
        bool controller=Manager.Input && Manager.Input.CurrentScheme==PlayerInputReader.ControlScheme.Gamepad;
        help.text=controller
            ? "Hold "+(ControllerGlyphs.PlayStation?"Cross":"A")+", move cursor, release to place\nLeft stick: cursor   Right stick: camera\n"+(ControllerGlyphs.PlayStation?"L2 / Circle":"LT / B")+": close tools\nUse the button shortcuts below."
            : "Hold left mouse, move cursor, release to place\nRight-drag: camera\nG / Escape: close tools\nUse the button shortcuts below.";
        status.text=grabbed ? (validDestination?"Release to place.":previewReason) : service.Message+(service.ActiveObject?"\nActive: "+service.ActiveObject.DisplayName:"");
        restoreButton.interactable=Ability && obj && obj.PastForm;
        modeButton.interactable=Ability; dismissButton.interactable=Ability && service.ActiveObject;
        RefreshButtonIcons(controller);
    }
    // Poll alongside the panel so keyboard/gamepad changes and controller-family
    // changes also refresh while it is already open. No separate device detector.
    void RefreshButtonIcons(bool controller)
    {
        var catalog=ControllerGlyphs.Catalog;
        if(!catalog)return;
        for(int i=0;i<buttonIcons.Count;i++)
        {
            var icon=buttonIcons[i];Color tint=Color.white;
            icon.sprite=controller ? ControllerGlyphs.Resolve(catalog.Buttons[controllerButtons[i]].Xbox,out tint)
                : catalog.OverworldKeys!=null && i<catalog.OverworldKeys.Length ? catalog.OverworldKeys[i] : null;
            // Trigger/key artwork needs contrast against the pale button face.
            if(tint==Color.white)tint=new Color(.08f,.09f,.11f);
            icon.color=tint;ControllerGlyphs.StyleFace(icon);
            var color=icon.color;color.a=icon.GetComponentInParent<Button>().interactable?1:.4f;icon.color=color;
            icon.enabled=icon.sprite;
        }
    }
    void DrawHighlights()
    {
        int used=0;
        foreach(var obj in FindObjectsByType<WorldPlaceObject>(FindObjectsSortMode.None))
        {
            float range=Ability?Ability.Range:service.DragReach;
            if(Vector3.Distance(Actor.position,obj.Volume(obj.transform.position).ClosestPoint(Actor.position))>range)continue;
            DrawBox(obj.Volume(obj.transform.position),obj==hovered?Color.yellow:new Color(1,.8f,.25f,.65f),ref used);
        }
        foreach(var point in FindObjectsByType<WorldPlaceSocket>(FindObjectsSortMode.None))
            if(Vector3.Distance(point.Position,Actor.position)<(Ability?Ability.Range:service.DragReach))DrawBox(new Bounds(point.Position+Vector3.up*.025f,new Vector3(1,.05f,1)),point.Activated?Color.green:Color.yellow,ref used);
        if(grabbed)DrawBox(grabbed.Volume(destination),validDestination?Color.green:Color.red,ref used);
        for(int i=used;i<lines.Count;i++)lines[i].gameObject.SetActive(false);
    }
    void DrawBox(Bounds b,Color color,ref int used)
    {
        // A single continuous wire traces all twelve edges (some edges are retraced).
        int[] order={0,1,3,2,0,4,5,1,5,7,3,7,6,2,6,4};
        if(used==lines.Count)
        {
            if(!lineMaterial)lineMaterial=new Material(Shader.Find("Game/GridOverlay"));
            var go=new GameObject("World preview outline");go.transform.SetParent(transform);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=lineMaterial;line.widthMultiplier=.035f;line.useWorldSpace=true;line.positionCount=order.Length;lines.Add(line);
        }
        var current=lines[used++];current.gameObject.SetActive(true);current.startColor=current.endColor=color;
        var properties=new MaterialPropertyBlock();properties.SetColor("_Color",color);current.SetPropertyBlock(properties);
        for(int i=0;i<order.Length;i++){int v=order[i];current.SetPosition(i,b.center+Vector3.Scale(b.extents,new Vector3((v&1)==0?-1:1,(v&2)==0?-1:1,(v&4)==0?-1:1)));}
    }
    RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    TMP_Text Label(string name,Transform parent,Vector2 pos,Vector2 size,int font)
    {
        var t=Rect(name,parent,pos,size).gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=font;t.color=Color.white;t.raycastTarget=false;return t;
    }
    Button MakeButton(Transform parent,string text,int x,UnityEngine.Events.UnityAction action)
    {
        var r=Rect(text,parent,new Vector2(x,-166),new Vector2(88,26));var image=r.gameObject.AddComponent<Image>();image.color=new Color(.8f,.85f,.9f);
        var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);
        var label=Label(text,r,new Vector2(6,0),new Vector2(56,26),12);label.color=Color.black;label.alignment=TextAlignmentOptions.MidlineLeft;label.text=text;
        label.enableAutoSizing=true;label.fontSizeMin=10;label.fontSizeMax=12;label.textWrappingMode=TextWrappingModes.NoWrap;
        var icon=Rect("Shortcut icon",r,new Vector2(64,-4.75f),new Vector2(16.5f,16.5f)).gameObject.AddComponent<Image>();
        icon.preserveAspect=true;icon.raycastTarget=false;buttonIcons.Add(icon);return button;
    }
    void Build()
    {
        ui=new GameObject("World manipulation",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));ui.transform.SetParent(transform,false);
        ui.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;ui.GetComponent<Canvas>().sortingOrder=190;
        var scale=ui.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
        var panel=Rect("Tools",ui.transform,new Vector2(16,-16),new Vector2(392,204));panel.gameObject.AddComponent<Image>().color=new Color(.025f,.035f,.05f,.9f);
        title=Label("Title",panel,new Vector2(12,-8),new Vector2(368,24),17);
        help=Label("Controls",panel,new Vector2(12,-36),new Vector2(368,76),12);
        status=Label("Feedback",panel,new Vector2(12,-116),new Vector2(368,46),13);
        restoreButton=MakeButton(panel,"Restore",12,Restore);modeButton=MakeButton(panel,"Mode",106,ToggleMode);dismissButton=MakeButton(panel,"Dismiss",200,Dismiss);MakeButton(panel,"Close",294,Close);
        RefreshButtonIcons(Manager && Manager.Input && Manager.Input.CurrentScheme==PlayerInputReader.ControlScheme.Gamepad);
        cursorRect=Rect("Controller cursor",ui.transform,Vector2.zero,new Vector2(10,10));cursorRect.pivot=new Vector2(.5f,.5f);var image=cursorRect.gameObject.AddComponent<Image>();image.color=Color.white;image.raycastTarget=false;
    }
    void OnDisable()=>Close();
    void OnDestroy(){Close();if(lineMaterial)Destroy(lineMaterial);if(ownedEvents)Destroy(ownedEvents);}
}
