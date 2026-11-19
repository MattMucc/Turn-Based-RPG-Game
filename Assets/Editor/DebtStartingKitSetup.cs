using System;
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
  const string prefab="Assets/Prefabs/Player/Prototype Debt.prefab";
  var root=PrefabUtility.LoadPrefabContents(prefab);
  try{var loadout=root.GetComponent<BattleActionLoadout>();loadout.CharacterLibrary=library;PrefabUtility.SaveAsPrefabAsset(root,prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
  // Existing scene overrides may carry their own loadout; update only the identified Debt character.
  foreach(var info in UnityEngine.Object.FindObjectsByType<BattleCharacterInfo>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(info.DisplayName!="Prototype Debt")continue;var loadout=info.GetComponent<BattleActionLoadout>();if(!loadout)continue;
   loadout.CharacterLibrary=library;EditorUtility.SetDirty(loadout);PrefabUtility.RecordPrefabInstancePropertyModifications(loadout);
  }
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
}
