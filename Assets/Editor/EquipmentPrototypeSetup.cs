using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EquipmentPrototypeSetup
{
    [MenuItem("Tools/Game/Prepare prototype equipment")]
    public static void Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        const string folder="Assets/Data/Equipment";
        if(!AssetDatabase.IsValidFolder("Assets/Data"))AssetDatabase.CreateFolder("Assets","Data");
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Data","Equipment");
        var sword=Make("shortsword","Training Shortsword",EquipmentKind.Weapon,WeaponType.Shortsword,new StatValues{Strength=2},new[]{Attack("Quick Attack",8,1,.75f),Attack("Slash",12,1,1)});
        var sweep=Attack("Sweeping Blow",15,2,1);sweep.Pattern=BattleTargetPattern.Sweep;sweep.Radius=1;
        var slam=Attack("Slam",12,2,1);slam.Pattern=BattleTargetPattern.Area;slam.Radius=1;
        var great=Make("greatsword","Training Greatsword",EquipmentKind.Weapon,WeaponType.Greatsword,new StatValues{Strength=4},new[]{sweep,slam});
        var thrust=Attack("Staff Thrust",8,2,.75f);thrust.Pattern=BattleTargetPattern.Piercing;
        var staff=Make("staff","Training Staff",EquipmentKind.Weapon,WeaponType.Staff,new StatValues{Magic=3},new[]{Attack("Staff Strike",10,1,.75f),thrust});
        var triple=Attack("Triple Shot",5,5,.4f);triple.Pattern=BattleTargetPattern.Multiple;triple.MaxTargets=3;
        var bow=Make("bow","Training Bow",EquipmentKind.Weapon,WeaponType.Bow,new StatValues{Strength=1},new[]{Attack("Shot",10,5,.8f),triple});
        var coat=Make("travel-outfit","Travel Outfit",EquipmentKind.Armor,0,new StatValues{PhysicalDefense=2,MagicalDefense=2},Array.Empty<BattleActionOption>());
        var heavy=Make("reinforced-outfit","Reinforced Outfit",EquipmentKind.Armor,0,new StatValues{PhysicalDefense=5},Array.Empty<BattleActionOption>());
        var charm=Make("vitality-charm","Vitality Charm",EquipmentKind.Accessory,0,new StatValues{Vitality=2},Array.Empty<BattleActionOption>());
        var focus=Make("magic-charm","Magic Charm",EquipmentKind.Accessory,0,new StatValues{Magic=2},Array.Empty<BattleActionOption>());
        var names=new[]{"Prototype Balanced","Prototype Debt","Prototype Sentinel","Prototype Mystic","Prototype Skirmisher","Prototype Arcanist"};
        var starters=new[]{sword,great,great,staff,bow,staff};
        var allowed=new[]{new[]{WeaponType.Shortsword,WeaponType.Greatsword},new[]{WeaponType.Greatsword,WeaponType.Staff},new[]{WeaponType.Greatsword,WeaponType.Shortsword},new[]{WeaponType.Staff,WeaponType.Bow},new[]{WeaponType.Bow,WeaponType.Shortsword},new[]{WeaponType.Staff,WeaponType.Shortsword}};
        for(int i=0;i<names.Length;i++){
            string path="Assets/Prefabs/Player/"+names[i]+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{var profile=root.GetComponent<CharacterEquipment>();if(!profile){profile=root.AddComponent<CharacterEquipment>();profile.AllowedWeaponTypes=allowed[i];profile.RestoreSlots(new[]{"starter-weapon-"+i,"starter-armor-"+i,null,null});}PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var manager=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if(manager&&manager.Equipment.Owned.Count==0){
            manager.Equipment.Catalog=new List<EquipmentDefinition>{sword,great,staff,bow,coat,heavy,charm,focus};
            for(int i=0;i<names.Length;i++){
                manager.Equipment.Owned.Add(new OwnedEquipment{InstanceId="starter-weapon-"+i,Definition=starters[i]});
                manager.Equipment.Owned.Add(new OwnedEquipment{InstanceId="starter-armor-"+i,Definition=coat});
            }
            foreach(var item in new[]{sword,great,staff,bow,heavy,charm,focus})manager.Equipment.Owned.Add(new OwnedEquipment{InstanceId="spare-"+item.Id,Definition=item});
            foreach(var member in manager.BattleParty.Concat(manager.ReserveParty))if(member){
                int index=Array.FindIndex(names,n=>member.name.StartsWith(n));if(index<0)continue;
                var profile=member.GetComponent<CharacterEquipment>()??member.gameObject.AddComponent<CharacterEquipment>();
                profile.AllowedWeaponTypes=allowed[index];profile.RestoreSlots(new[]{"starter-weapon-"+index,"starter-armor-"+index,null,null});
                PrefabUtility.RecordPrefabInstancePropertyModifications(profile);EditorUtility.SetDirty(profile);
            }
            manager.BindEquipment();EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
    }
    static BattleActionOption Attack(string name,int damage,int reach,float scale)=>new(){Name=name,Description="Prototype weapon attack.",Category=BattleActionCategory.Attack,Damage=damage,Reach=reach,Radius=0,Scaling=ScalingStat.Strength,StatScaling=scale};
    [MenuItem("Tools/Game/Prepare prototype weapon mastery")]
    public static void PrepareMastery()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        foreach(var guid in AssetDatabase.FindAssets("t:EquipmentDefinition",new[]{"Assets/Data/Equipment"})){
            var item=AssetDatabase.LoadAssetAtPath<EquipmentDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if(item.Kind!=EquipmentKind.Weapon||item.Mastery.Count>0)continue;
            var move=Attack(item.WeaponType==WeaponType.Shortsword?"Precision Thrust":item.WeaponType==WeaponType.Greatsword?"Driving Cleave":item.WeaponType==WeaponType.Staff?"Staff Sweep":"Piercing Shot",12,item.WeaponType==WeaponType.Bow?5:item.WeaponType==WeaponType.Shortsword?1:2,1);
            if(item.WeaponType==WeaponType.Staff){move.Pattern=BattleTargetPattern.Sweep;move.Radius=1;}
            else if(item.WeaponType!=WeaponType.Shortsword)move.Pattern=BattleTargetPattern.Piercing;
            else move.StatScaling=1.25f;
            item.Mastery.Add(new WeaponMasteryMilestone{RequiredExperience=10,Name=move.Name,Description="Unlock this additional weapon Attack.",Attacks=new List<BattleActionOption>{move}});
            item.Mastery.Add(new WeaponMasteryMilestone{RequiredExperience=25,Name="Finisher",Description="Gain 1 additional SP when a weapon Attack defeats at least one hostile. Once per action.",FinisherSkillPoints=1});
            EditorUtility.SetDirty(item);
        }
        AssetDatabase.SaveAssets();
    }
    static EquipmentDefinition Make(string id,string name,EquipmentKind kind,WeaponType type,StatValues stats,BattleActionOption[] attacks)
    {
        string path="Assets/Data/Equipment/"+name+".asset";var item=AssetDatabase.LoadAssetAtPath<EquipmentDefinition>(path);if(item)return item;
        item=ScriptableObject.CreateInstance<EquipmentDefinition>();item.Id=id;item.DisplayName=name;item.Description="Prototype equipment for testing. Values and weapon access are provisional.";item.Kind=kind;item.WeaponType=type;item.Bonuses=stats;item.Attacks=attacks.ToList();AssetDatabase.CreateAsset(item,path);return item;
    }
}
