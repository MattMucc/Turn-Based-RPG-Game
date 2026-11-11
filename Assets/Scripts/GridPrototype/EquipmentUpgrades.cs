using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.GridPrototype
{
    [Serializable] public sealed class MaterialStack { public string Id, Name; [Min(0)] public int Quantity; }
    [Serializable] public sealed class UpgradeMaterialCost { public string MaterialId; [Min(1)] public int Quantity=1; }
    [Serializable] public sealed class EquipmentUpgradeStep
    {
        [Min(1)] public int Fee=25;
        public List<UpgradeMaterialCost> Materials=new();
        [Tooltip("Additional stats gained at this step, added to earlier upgrades.")]
        public StatValues AddedStats=new();
    }
    public static class EquipmentUpgrades
    {
        public static int Bonus(OwnedEquipment item,CharacterStat stat,int? level=null)
        {
            if(item==null||!item.Definition)return 0;
            long value=item.Definition.Bonuses?.Get(stat)??0;
            foreach(var step in item.Definition.Upgrades.Take(Mathf.Clamp(level??item.UpgradeLevel,0,item.Definition.Upgrades.Count)))value+=step?.AddedStats?.Get(stat)??0;
            return (int)Math.Max(int.MinValue,Math.Min(int.MaxValue,value));
        }
        public static bool Requirements(PartyEquipment inventory,OwnedEquipment item,int expectedLevel,out Dictionary<string,int> costs,out string error)
        {
            costs=new();error="";
            if(item==null||!item.Definition||!inventory.Owned.Contains(item)){error="Equipment is unavailable.";return false;}
            if(item.UpgradeLevel!=expectedLevel){error="Upgrade level changed. Review the preview again.";return false;}
            if(expectedLevel<0||expectedLevel>=item.Definition.Upgrades.Count){error="No further upgrades available.";return false;}
            var step=item.Definition.Upgrades[expectedLevel];
            if(step==null||step.Fee<1||step.Materials==null||step.Materials.Count==0){error="No valid upgrade recipe and fee configured.";return false;}
            foreach(var cost in step.Materials){
                if(cost==null||string.IsNullOrWhiteSpace(cost.MaterialId)||cost.Quantity<=0){error="Invalid material recipe.";return false;}
                long sum=(long)(costs.TryGetValue(cost.MaterialId,out int n)?n:0)+cost.Quantity;
                if(sum>int.MaxValue){error="Material cost is too large.";return false;}costs[cost.MaterialId]=(int)sum;
            }
            foreach(var pair in costs){
                var stacks=inventory.Materials.Where(m=>m!=null&&m.Id==pair.Key).ToList();
                if(stacks.Count!=1||stacks[0].Quantity<pair.Value){error="Insufficient materials.";return false;}
            }
            if(inventory.Coins<step.Fee){error="Insufficient coins for the blacksmith fee.";return false;}
            return true;
        }
        public static bool TryApply(PartyEquipment inventory,OwnedEquipment item,int expectedLevel,out string error)
        {
            if(!Requirements(inventory,item,expectedLevel,out var costs,out error))return false;
            foreach(var pair in costs)inventory.Materials.Single(m=>m.Id==pair.Key).Quantity-=pair.Value;
            inventory.Coins-=item.Definition.Upgrades[expectedLevel].Fee;
            item.UpgradeLevel++;return true;
        }
    }
}
