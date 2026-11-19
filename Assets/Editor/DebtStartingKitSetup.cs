using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.GridPrototype;
public static class DebtStartingKitSetup {
 [MenuItem("Tools/Game/Configure Debt starting kit")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
  const string path="Assets/Resources/DebtStartingKit.asset";
  var library=AssetDatabase.LoadAssetAtPath<SharedBattleActionLibrary>(path);
  if(!library){library=ScriptableObject.CreateInstance<SharedBattleActionLibrary>();library.Actions=DebtStartingKit.Create();AssetDatabase.CreateAsset(library,path);}
  foreach(var action in DebtSkillTree.Actions())if(!library.Actions.Any(a=>a.Name==action.Name))library.Actions.Add(action);library.NotifyChanged();EditorUtility.SetDirty(library);
  var growth=AssetDatabase.LoadAssetAtPath<GrowthPageLayout>("Assets/Resources/UI/Growth/Growth.asset");
  const string learn="Assets/Resources/UI/Growth/Learn.prefab";if(!AssetDatabase.LoadAssetAtPath<GameObject>(learn))AssetDatabase.CopyAsset("Assets/Resources/UI/Merchants/Action.prefab",learn);
  if(growth&&!growth.Action){growth.Action=AssetDatabase.LoadAssetAtPath<GameObject>(learn).GetComponent<InventoryButtonView>();EditorUtility.SetDirty(growth);}
  const string prefab="Assets/Prefabs/Player/Prototype Debt.prefab";
  var root=PrefabUtility.LoadPrefabContents(prefab);
  try{var loadout=root.GetComponent<BattleActionLoadout>();loadout.CharacterLibrary=library;var data=root.GetComponent<CharacterMenuData>();if(!data)data=root.AddComponent<CharacterMenuData>();if(!data.DebtTree){data.DebtTree=true;data.SkillTree=DebtSkillTree.Create();}PrefabUtility.SaveAsPrefabAsset(root,prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
  // Existing scene overrides may carry their own loadout; update only the identified Debt character.
  foreach(var info in UnityEngine.Object.FindObjectsByType<BattleCharacterInfo>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(info.DisplayName!="Prototype Debt")continue;var loadout=info.GetComponent<BattleActionLoadout>();if(!loadout)continue;
   loadout.CharacterLibrary=library;EditorUtility.SetDirty(loadout);PrefabUtility.RecordPrefabInstancePropertyModifications(loadout);
  }
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
}
