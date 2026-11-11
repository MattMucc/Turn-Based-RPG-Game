using System;
using System.Collections.Generic;
using Game.GridPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class SaveSceneSetup
{
    [MenuItem("Tools/Game/Prepare scene save identities")]
    public static void Prepare()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before preparing save identities.");
        var scene=SceneManager.GetActiveScene();var used=new HashSet<string>();
        foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true)){
            if(component is BattleCharacterInfo||component is BattleGroup||component is BattleDestructible||component is BattleUsable||component is SaveCheckpoint)Ensure(component.gameObject);
            var usable=component as BattleUsable;if(usable)for(int i=0;i<usable.OnUse.GetPersistentEventCount();i++){
                var target=usable.OnUse.GetPersistentTarget(i);var go=target as GameObject;var behaviour=target as Component;if(behaviour)go=behaviour.gameObject;
                if(go&&go.scene==scene)Ensure(go);
            }
        }
        foreach(var root in scene.GetRootGameObjects())foreach(var identity in root.GetComponentsInChildren<SaveIdentity>(true)){
            if(string.IsNullOrEmpty(identity.Id)||!used.Add(identity.Id)){identity.AssignEditorId(Guid.NewGuid().ToString("N"));used.Add(identity.Id);}
            EditorUtility.SetDirty(identity);PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }
    static void Ensure(GameObject go){if(!go.GetComponent<SaveIdentity>())Undo.AddComponent<SaveIdentity>(go);}
    [MenuItem("Tools/Game/Add prototype checkpoint")]
    public static void AddCheckpoint()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        if(UnityEngine.Object.FindFirstObjectByType<SaveCheckpoint>())return;
        var manager=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(!manager||manager.BattleParty.Count==0)throw new InvalidOperationException("No party found.");
        var leader=manager.BattleParty[0].transform;var location=leader.position+leader.forward*5;
        RaycastHit hit;if(Physics.Raycast(location+Vector3.up*4,Vector3.down,out hit,20,~0,QueryTriggerInteraction.Ignore))location=hit.point;
        var root=new GameObject("Prototype Checkpoint");Undo.RegisterCreatedObjectUndo(root,"Add checkpoint");root.transform.position=location;
        var collider=root.AddComponent<BoxCollider>();collider.isTrigger=true;collider.center=Vector3.up;collider.size=new Vector3(2.5f,2.5f,2.5f);
        root.AddComponent<SaveCheckpoint>().DisplayName="Starting area checkpoint";
        var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Checkpoint marker";marker.transform.SetParent(root.transform,false);marker.transform.localPosition=new Vector3(0,.08f,0);marker.transform.localScale=new Vector3(1.8f,.08f,1.8f);
        UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
        const string path="Assets/Resources/CheckpointMarker.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");material=new Material(shader);material.color=new Color(.15f,.75f,1);AssetDatabase.CreateAsset(material,path);}
        marker.GetComponent<Renderer>().sharedMaterial=material;
        Prepare();
    }
}
