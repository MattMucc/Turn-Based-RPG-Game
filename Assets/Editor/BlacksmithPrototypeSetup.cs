using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BlacksmithPrototypeSetup
{
    [MenuItem("Tools/Game/Prepare prototype blacksmith")]
    public static void Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        var manager=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(!manager)throw new InvalidOperationException("No GameManager in this scene.");
        foreach(var item in manager.Equipment.Catalog){
            bool oldPrototype=item.Upgrades.Count==3&&item.Upgrades.Select((step,i)=>step.Materials.Count==1&&step.Materials[0].Quantity==(i+1)*2&&(step.Materials[0].MaterialId=="iron"||step.Materials[0].MaterialId=="leather")).All(v=>v);
            if(item.Upgrades.Count!=0&&!oldPrototype)continue;
            var old=item.Upgrades.ToArray();item.Upgrades=new List<EquipmentUpgradeStep>();
            string[][] materials=item.Kind==EquipmentKind.Weapon?new[]{new[]{"iron"},new[]{"iron","hardwood"},new[]{"steel","crystal"}}:
                item.Kind==EquipmentKind.Armor?new[]{new[]{"leather"},new[]{"iron","hide"},new[]{"steel","hide","crystal"}}:
                new[]{new[]{"copper"},new[]{"silver","crystal"},new[]{"gold-ore","crystal"}};
            for(int i=0;i<3;i++){
                var stats=i<old.Length?old[i].AddedStats:new StatValues{Vitality=item.Bonuses.Vitality>0?1:0,Magic=item.Bonuses.Magic>0?1:0};
                var recipe=new EquipmentUpgradeStep{Fee=new[]{25,75,150}[i],AddedStats=stats};
                for(int j=0;j<materials[i].Length;j++)recipe.Materials.Add(new UpgradeMaterialCost{MaterialId=materials[i][j],Quantity=j==0?(i+1)*2:j==1?i*2:2});
                item.Upgrades.Add(recipe);
            }EditorUtility.SetDirty(item);
        }
        var ids=new[]{"iron","leather","hardwood","hide","steel","crystal","copper","silver","gold-ore"};
        var names=new[]{"Iron","Leather","Hardwood","Hide","Steel","Crystal","Copper","Silver","Gold Ore"};
        for(int i=0;i<ids.Length;i++)if(!manager.Equipment.Materials.Any(m=>m.Id==ids[i]))manager.Equipment.Materials.Add(new MaterialStack{Id=ids[i],Name=names[i],Quantity=12});
        if(!UnityEngine.Object.FindFirstObjectByType<Blacksmith>()){
            var root=new GameObject("Prototype Blacksmith");Undo.RegisterCreatedObjectUndo(root,"Add blacksmith");root.transform.position=new Vector3(8,0,-14);
            RaycastHit hit;if(Physics.Raycast(root.transform.position+Vector3.up*8,Vector3.down,out hit,20,~0,QueryTriggerInteraction.Ignore))root.transform.position=hit.point;
            var smith=root.AddComponent<Blacksmith>();smith.DisplayName="Blacksmith";
            var trigger=root.GetComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up;trigger.size=new Vector3(2,2,2);
            var model=GameObject.CreatePrimitive(PrimitiveType.Capsule);model.name="Blacksmith placeholder";model.transform.SetParent(root.transform,false);model.transform.localPosition=Vector3.up;
            var sign=new GameObject("Blacksmith sign");sign.transform.SetParent(root.transform,false);sign.transform.localPosition=new Vector3(0,2.5f,0);var text=sign.AddComponent<TextMesh>();text.text="BLACKSMITH";text.characterSize=.12f;text.fontSize=36;text.anchor=TextAnchor.MiddleCenter;
            manager.Equipment.Coins=Math.Max(manager.Equipment.Coins,300);
        }
        EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
