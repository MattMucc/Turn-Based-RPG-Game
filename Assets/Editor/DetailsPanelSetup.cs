using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DetailsPanelSetup
{
    const string Folder="Assets/Resources/UI/Details";
    [MenuItem("Tools/Game/UI/Create missing details templates")]
    public static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder);
        var title=Template("Title",23,new Color(.3f,.8f,1));
        var text=Template("Text",16,new Color(.88f,.94f,1));
        var compact=Template("Compact",14,new Color(.88f,.94f,1));
        var attack=Template("Attack",16,new Color(.88f,.94f,1));
        Make("Abilities",new[]{DetailSection.Name,DetailSection.Artwork,DetailSection.Availability,DetailSection.Mastery,DetailSection.Attacks},title,text,compact,attack);
        Make("Shop",new[]{DetailSection.Name,DetailSection.Artwork,DetailSection.Description,DetailSection.Price,DetailSection.Upgrade,DetailSection.Stats,DetailSection.Availability,DetailSection.Mastery,DetailSection.Attacks},title,text,compact,attack);
        Make("Blacksmith",new[]{DetailSection.Name,DetailSection.Artwork,DetailSection.Description,DetailSection.Upgrade,DetailSection.Availability,DetailSection.Price,DetailSection.Materials,DetailSection.Stats,DetailSection.Notice},title,text,compact,attack);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    static DetailsSectionView Template(string name,int size,Color color)
    {
        string path=Folder+"/"+name+".prefab";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing)return existing.GetComponent<DetailsSectionView>();
        var root=new GameObject(name,typeof(RectTransform),typeof(DetailsSectionView));
        try{
            var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(380,110);
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(root.transform,false);
            var host=(RectTransform)content.transform;host.anchorMin=Vector2.zero;host.anchorMax=Vector2.one;host.offsetMin=host.offsetMax=Vector2.zero;
            var label=new GameObject("Text template",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(content.transform,false);
            var lr=(RectTransform)label.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
            var tmp=label.GetComponent<TextMeshProUGUI>();tmp.font=TMP_Settings.defaultFontAsset;tmp.fontSize=size;tmp.color=color;tmp.raycastTarget=false;
            tmp.text=name=="Title"?"Selected ability / equipment":"Sample bound text\nContent comes from the selected ability or equipment.";tmp.textWrappingMode=TextWrappingModes.Normal;
            var view=root.GetComponent<DetailsSectionView>();view.Content=host;view.TextTemplate=tmp;
            return PrefabUtility.SaveAsPrefabAsset(root,path).GetComponent<DetailsSectionView>();
        }finally{Object.DestroyImmediate(root);}
    }
    static void Make(string name,DetailSection[] bindings,DetailsSectionView title,DetailsSectionView text,DetailsSectionView compact,DetailsSectionView attack)
    {
        string path=Folder+"/"+name+".asset";if(AssetDatabase.LoadAssetAtPath<DetailsPanelLayout>(path))return;
        var layout=ScriptableObject.CreateInstance<DetailsPanelLayout>();layout.Sections=new System.Collections.Generic.List<DetailsPanelLayout.Section>();layout.Padding=new RectOffset(12,12,12,12);layout.Spacing=10;
        foreach(var binding in bindings)layout.Sections.Add(new DetailsPanelLayout.Section{Binding=binding,Prefab=binding==DetailSection.Name?title:binding==DetailSection.Attacks?attack:binding==DetailSection.Availability||binding==DetailSection.Mastery?compact:text});
        AssetDatabase.CreateAsset(layout,path);
    }
}
[CustomEditor(typeof(DetailsPanelLayout))]
public sealed class DetailsPanelLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Reorder Sections by dragging their handles. Toggle Visible to hide a section. Assign a section prefab to change its appearance. Empty sections collapse automatically. Attack patterns use the action's existing combat data.",MessageType.Info);
        if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((DetailsPanelLayout)target);
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Double-click a section prefab to edit its text template, artwork size, padding, row spacing or decorative children. Duplicate a prefab or layout for page-specific styling. Custom sections display Custom Text.",MessageType.None);
    }
}
