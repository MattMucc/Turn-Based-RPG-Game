using Game.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorldPlacePrototypeBuilder
{
    static Material Material(string name,Color color)
    {
        string path="Assets/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;AssetDatabase.CreateAsset(m,path);}
        return m;
    }
    static GameObject Box(string name,Transform parent,Vector3 local,Vector3 size,Material material)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=local;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;return o;
    }
    static void Sign(Transform parent,Vector3 position,string words)
    {
        var obj=new GameObject("Prototype sign");obj.transform.SetParent(parent,false);obj.transform.position=position;
        var t=obj.AddComponent<TextMesh>();t.text=words;t.characterSize=.13f;t.fontSize=48;t.anchor=TextAnchor.MiddleCenter;t.color=Color.white;
        obj.transform.rotation=Quaternion.Euler(0,180,0);
    }
    [MenuItem("Tools/Game/Create Place Overworld Prototype")]
    public static void CreateMenu() => Debug.Log(Create());
    public static string Create()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.path!="Assets/Scenes/SampleScene.unity")return "Open SampleScene in Edit Mode first.";
        if(GameObject.Find("Place Overworld Prototype"))return "Prototype already exists; existing objects preserved.";
        var root=new GameObject("Place Overworld Prototype");
        var stone=Material("Place Prototype Stone",new Color(.25f,.3f,.35f));
        var past=Material("Place Prototype Past",new Color(.3f,.65f,.8f));
        var gold=Material("Place Prototype Socket",new Color(.8f,.65f,.2f));
        var wood=Material("Place Prototype Weight",new Color(.4f,.23f,.12f));
        Box("Near bank",root.transform,new Vector3(31,0,-22),new Vector3(6,1,10),stone);
        Box("Far bank",root.transform,new Vector3(43,0,-22),new Vector3(6,1,10),stone);
        var bridge=new GameObject("Remembered crossing");bridge.transform.SetParent(root.transform);bridge.transform.position=new Vector3(37,.1f,-20);
        var target=bridge.AddComponent<WorldPlaceObject>();target.DisplayName="Ruined crossing";target.CanDrag=false;target.CanRelocate=false;target.BoundsCenter=new Vector3(0,.2f,0);target.BoundsSize=new Vector3(6,.4f,3);
        target.PresentForm=new GameObject("Present - broken ends");target.PresentForm.transform.SetParent(bridge.transform,false);
        Box("Broken left",target.PresentForm.transform,new Vector3(-2.8f,.15f,0),new Vector3(.4f,.3f,3),stone);
        Box("Broken right",target.PresentForm.transform,new Vector3(2.8f,.15f,0),new Vector3(.4f,.3f,3),stone);
        target.PastForm=Box("Past - usable crossing",bridge.transform,new Vector3(0,.2f,0),new Vector3(6,.4f,3),past);target.PastForm.SetActive(false);
        var pick=bridge.AddComponent<BoxCollider>();pick.isTrigger=true;pick.center=target.BoundsCenter;pick.size=target.BoundsSize;
        var shortcut=Box("Permanent shortcut",root.transform,new Vector3(37,.3f,-25),new Vector3(6,.4f,2),gold);shortcut.SetActive(false);
        var socketObj=new GameObject("Weight socket - opens permanent shortcut");socketObj.transform.SetParent(root.transform);socketObj.transform.position=new Vector3(31,.5f,-24);
        var socket=socketObj.AddComponent<WorldPlaceSocket>();socket.SocketKey="Weight";socket.PermanentShortcut=shortcut;socket.SnapRadius=.8f;
        var marker=Box("Socket marking",socketObj.transform,new Vector3(0,.006f,0),new Vector3(1.3f,.01f,1.3f),gold);Object.DestroyImmediate(marker.GetComponent<Collider>());
        for(int i=0;i<2;i++)
        {
            var weight=new GameObject(i==0?"Movable weight":"Heavy weight");weight.transform.SetParent(root.transform);weight.transform.position=new Vector3(i==0?29:28,.5f,i==0?-23:-18);
            var obj=weight.AddComponent<WorldPlaceObject>();obj.DisplayName=weight.name;obj.RequiredStrength=i==0?0:12;obj.SocketKey="Weight";
            obj.PresentForm=Box("Present",weight.transform,new Vector3(0,.5f,0),Vector3.one,wood);
            obj.PastForm=Box("Past",weight.transform,new Vector3(0,.5f,0),Vector3.one,past);obj.PastForm.SetActive(false);
        }
        Sign(root.transform,new Vector3(27,2.5f,-25),"PLACE PROTOTYPE\nG / LT: object tools\nVanguard: restore + relocate\nWeight socket: permanent bridge");
        var manager=Object.FindFirstObjectByType<GameManager>();
        if(manager && !manager.GetComponent<WorldManipulationController>())manager.gameObject.AddComponent<WorldManipulationController>();
        string path="Assets/Prefabs/Player/Prototype Vanguard.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(path);if(!prefab.GetComponent<PlaceWorldAbility>())prefab.AddComponent<PlaceWorldAbility>();PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Created beside the east edge of the ground (x28-46, z-17 to -27). Vanguard is the temporary Place tester.";
    }
}
