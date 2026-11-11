using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
public static class CheckpointRest
{
    public static bool TryApply(GameManager manager,SaveCheckpoint checkpoint,out string error,IEnumerable<BattleGroup> groupScope=null)
    {
        error="";
        if(!manager||!checkpoint||!checkpoint.isActiveAndEnabled||!manager.PossessedCharacter||!checkpoint.InRange(manager.PossessedCharacter.transform)||manager.IsSwitchingCharacter||(BattleManager.Instance&&BattleManager.Instance.InBattle)){error="Rest is only available at a nearby checkpoint outside combat.";return false;}
        var plans=new List<(BattleGroup group,BattleGroup.SpawnPlan plan)>();
        var reserved=new List<Vector3>();
        foreach(var group in (groupScope??Object.FindObjectsByType<BattleGroup>(FindObjectsSortMode.None)).Where(g=>g.gameObject.scene==checkpoint.gameObject.scene&&g.ResetsOnRest)){
            if(!group.TryPrepareSpawn(out var plan,out error,reserved))return false;plans.Add((group,plan));reserved.AddRange(plan.Positions);
        }
        // Plan every group first: missing space must not partially reset the area or grant recovery.
        foreach(var pair in plans)pair.group.ApplySpawn(pair.plan,true);
        foreach(var member in manager.Progression.Members)if(member){member.HealOutsideCombat(int.MaxValue);member.RestoreMagicOutsideCombat(int.MaxValue);}
        return true;
    }
}
