using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Isolated preview scene only: never opens, changes or saves a gameplay scene.
public sealed class DetailsPanelPreviewWindow : EditorWindow
{
    [SerializeField] DetailsPanelLayout layout;
    [SerializeField] EquipmentDefinition equipment;
    [SerializeField] int attackIndex;
    [SerializeField] int panelWidth=426,panelHeight=510;
    [SerializeField] bool followSelection=true,live=true;
    [SerializeField] float scroll;
    PreviewRenderUtility preview;
    GameObject canvasRoot;
    RectTransform content;
    DetailsPanelLayout temporaryLayout;
    string fingerprint;
    double nextCheck;
    bool dirty=true;
    string error;
    float contentHeight;
    [MenuItem("Tools/Game/UI/Details panel preview")]
    public static void Open()
    {
        var window=GetWindow<DetailsPanelPreviewWindow>("Details preview");
        window.minSize=new Vector2(380,420);
        if(Selection.activeObject is DetailsPanelLayout asset)window.SetLayout(asset);
    }
    public static void Open(DetailsPanelLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("Details preview");window.SetLayout(asset);}
    void SetLayout(DetailsPanelLayout asset){layout=asset;scroll=0;dirty=true;Repaint();}
    void OnEnable(){Selection.selectionChanged+=SelectionChanged;Undo.undoRedoPerformed+=Changed;EditorApplication.projectChanged+=Changed;}
    void OnDisable(){Selection.selectionChanged-=SelectionChanged;Undo.undoRedoPerformed-=Changed;EditorApplication.projectChanged-=Changed;DisposePreview();}
    void Changed(){dirty=true;Repaint();}
    void SelectionChanged(){if(followSelection&&Selection.activeObject is DetailsPanelLayout asset)SetLayout(asset);}
    void Update()
    {
        if(!live||EditorApplication.timeSinceStartup<nextCheck)return;
        nextCheck=EditorApplication.timeSinceStartup+.3;
        string current=Signature();
        if(current!=fingerprint){fingerprint=current;dirty=true;Repaint();}
    }
    string Signature()
    {
        if(!layout)return "";
        string result=EditorJsonUtility.ToJson(layout)+(equipment?EditorJsonUtility.ToJson(equipment):"");
        foreach(var section in layout.Sections)if(section?.Prefab){
            var prefab=PreviewPrefab(section.Prefab);
            result+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(section.Prefab)).ToString();
            // Include unsaved Prefab Mode changes, including child text styles and decoration.
            foreach(var component in prefab.GetComponentsInChildren<Component>(true))if(component)result+=EditorJsonUtility.ToJson(component);
        }
        return result;
    }
    static DetailsSectionView PreviewPrefab(DetailsSectionView asset)
    {
        var stage=PrefabStageUtility.GetCurrentPrefabStage();
        return stage!=null&&stage.assetPath==AssetDatabase.GetAssetPath(asset)
            ?stage.prefabContentsRoot.GetComponent<DetailsSectionView>()??asset:asset;
    }
    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        layout=(DetailsPanelLayout)EditorGUILayout.ObjectField("Layout",layout,typeof(DetailsPanelLayout),false);
        equipment=(EquipmentDefinition)EditorGUILayout.ObjectField("Example equipment",equipment,typeof(EquipmentDefinition),false);
        using(new EditorGUILayout.HorizontalScope()){
            followSelection=GUILayout.Toggle(followSelection,"Follow layout selection");
            live=GUILayout.Toggle(live,"Live updates");
            if(GUILayout.Button("Refresh",GUILayout.Width(65)))dirty=true;
        }
        panelWidth=EditorGUILayout.IntSlider("Panel width",panelWidth,280,800);
        panelHeight=EditorGUILayout.IntSlider("Panel height",panelHeight,240,900);
        var attacks=AvailableAttacks();
        if(attacks.Count>1)attackIndex=EditorGUILayout.Popup("Example Attack",Mathf.Clamp(attackIndex,0,attacks.Count-1),attacks.Select(a=>a.Name).ToArray());
        if(EditorGUI.EndChangeCheck()){scroll=0;dirty=true;}
        EditorGUILayout.HelpBox("Edit Mode only. Example values are not your live party. Layouts, prefab styling and Attack patterns use the game's renderers. Edits in Prefab Mode are previewed before saving.",MessageType.Info);
        if(!layout){EditorGUILayout.HelpBox("Select a details layout asset to begin.",MessageType.None);return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorGUILayout.HelpBox("Preview pauses during Play Mode.",MessageType.Info);return;}
        if(dirty){
            try{BuildPreview();error=null;}catch(Exception ex){error=ex.Message;DisposePreview();}
            dirty=false;
        }
        if(error!=null){EditorGUILayout.HelpBox(error,MessageType.Error);return;}
        float maxScroll=Mathf.Max(0,contentHeight-panelHeight);
        scroll=EditorGUILayout.Slider("Scroll",scroll,0,maxScroll);
        var area=GUILayoutUtility.GetRect(1,10000,80,10000,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
        float scale=Mathf.Min(area.width/panelWidth,area.height/panelHeight);
        var frame=new Rect(area.x+(area.width-panelWidth*scale)/2,area.y,panelWidth*scale,panelHeight*scale);
        if(Event.current.type==EventType.ScrollWheel&&frame.Contains(Event.current.mousePosition)){scroll=Mathf.Clamp(scroll+Event.current.delta.y*24,0,maxScroll);Event.current.Use();Repaint();}
        if(Event.current.type==EventType.Repaint&&preview!=null){
            content.anchoredPosition=new Vector2(0,scroll);Canvas.ForceUpdateCanvases();
            preview.BeginPreview(new Rect(0,0,panelWidth,panelHeight),GUIStyle.none);
            preview.Render(true);
            var texture=preview.EndPreview();GUI.DrawTexture(frame,texture,ScaleMode.StretchToFill,false);
        }
    }
    List<BattleActionOption> AvailableAttacks()=>equipment
        ?(equipment.Attacks??new()).Concat(WeaponMastery.Milestones(equipment).SelectMany(m=>m.Attacks??new())).Where(a=>a!=null).ToList()
        :new List<BattleActionOption>();
    public void BuildPreview()
    {
        DisposePreview();if(!layout)return;
        preview=new PreviewRenderUtility();
        // Game camera rendering includes world-space UI; this is still an isolated Edit Mode preview.
        preview.camera.cameraType=CameraType.Game;
        preview.camera.orthographic=true;preview.camera.orthographicSize=panelHeight*.005f;
        preview.camera.transform.position=new Vector3(panelWidth*.005f,-panelHeight*.005f,-10);
        preview.camera.transform.rotation=Quaternion.identity;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=30;
        preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.035f,.075f,.12f);
        canvasRoot=new GameObject("Details preview (temporary)",typeof(RectTransform),typeof(Canvas));
        preview.AddSingleGO(canvasRoot);
        var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=preview.camera;
        var root=(RectTransform)canvasRoot.transform;root.pivot=new Vector2(0,1);root.sizeDelta=new Vector2(panelWidth,panelHeight);root.localScale=Vector3.one*.01f;
        var viewport=Rect("Preview viewport",root,panelWidth,panelHeight);viewport.gameObject.AddComponent<RectMask2D>();
        content=Rect("Content",viewport,panelWidth,panelHeight);
        var rows=new List<DetailsPanelComposer.Row>();
        void Text(DetailSection binding,string value){
            if(string.IsNullOrWhiteSpace(value))return;
            var rect=Rect(binding.ToString(),content,panelWidth-24,30);
            var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.fontSize=16;text.text=value;text.raycastTarget=false;
            rect.sizeDelta=new Vector2(panelWidth-24,Mathf.Max(24,text.GetPreferredValues(value,panelWidth-24,0).y));
            rows.Add(new DetailsPanelComposer.Row(binding,rect));
        }
        var attacks=AvailableAttacks();
        var action=attacks.Count>0?attacks[Mathf.Clamp(attackIndex,0,attacks.Count-1)]:new BattleActionOption{Name="Sweeping Blow",Description="Sweep across nearby targets.",Reach=1,Radius=1,Damage=25,Category=BattleActionCategory.Attack};
        Text(DetailSection.Name,equipment?equipment.DisplayName:action.Name);
        var art=equipment?equipment.Image:action.Image;
        if(art){var picture=Rect("Artwork",content,112,112);var image=picture.gameObject.AddComponent<RawImage>();image.texture=art;rows.Add(new DetailsPanelComposer.Row(DetailSection.Artwork,picture));}
        Text(DetailSection.Description,equipment?equipment.Description:"A sample entry with enough text to preview wrapping and spacing.");
        Text(DetailSection.Availability,"Example character · Level 10");
        Text(DetailSection.Price,"50 coins / 300 owned");
        Text(DetailSection.Upgrade,"Upgrade 0 / 3 → 1");
        Text(DetailSection.Materials,"MATERIALS · required / owned\nIron: 2 / 6\nHide: 1 / 3");
        Text(DetailSection.Stats,"Strength: 10 → 12\nMagic: 8 → 8\nPhysical Defense: 6 → 7");
        Text(DetailSection.Mastery,"Mastery level 1 · XP 4 / 10");
        if(equipment)foreach(var milestone in WeaponMastery.Milestones(equipment))Text(DetailSection.Mastery,milestone.Name+" · "+milestone.RequiredExperience+" XP\n"+milestone.Description);
        var source=new GridUnit("Preview",Vector2Int.zero){Strength=10,Magic=10,Soul=10};
        Text(DetailSection.Attacks,action.TooltipFor(source));
        var before=content.Cast<Transform>().ToArray();
        DetailsPatternPreview.Draw(content,action,0,panelWidth-24,24);
        foreach(RectTransform child in content.Cast<Transform>().Except(before).ToArray())rows.Add(new DetailsPanelComposer.Row(DetailSection.Attacks,child));
        Text(DetailSection.Notice,"Example notice");
        temporaryLayout=Instantiate(layout);temporaryLayout.hideFlags=HideFlags.HideAndDontSave;
        foreach(var section in temporaryLayout.Sections)if(section?.Prefab)section.Prefab=PreviewPrefab(section.Prefab);
        Canvas.ForceUpdateCanvases();
        DetailsPanelComposer.Compose(content,temporaryLayout,rows);
        Canvas.ForceUpdateCanvases();foreach(var text in canvasRoot.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate();contentHeight=content.rect.height;
    }
    public Texture2D CapturePreview()
    {
        if(preview==null)BuildPreview();
        if(preview==null)return null;
        content.anchoredPosition=new Vector2(0,scroll);
        preview.BeginStaticPreview(new Rect(0,0,panelWidth,panelHeight));
        Canvas.ForceUpdateCanvases();preview.Render(true);
        return preview.EndStaticPreview();
    }
    static RectTransform Rect(string name,Transform parent,float w,float h)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(w,h);return rect;
    }
    void DisposePreview()
    {
        if(preview!=null){preview.Cleanup();preview=null;}canvasRoot=null;content=null;
        if(temporaryLayout)DestroyImmediate(temporaryLayout);temporaryLayout=null;
    }
}
