using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.GridPrototype
{
    [Serializable] public sealed class WeaponMasteryMilestone
    {
        [Tooltip("Cumulative XP required. Use distinct positive thresholds.")]
        [Min(1)] public int RequiredExperience = 10;
        public string Name;
        [TextArea] public string Description;
        public List<BattleActionOption> Attacks = new();
        [Tooltip("Extra SP once after a weapon Attack defeats at least one hostile. Not per hit or target.")]
        [Range(0,9)] public int FinisherSkillPoints;
    }
    public static class WeaponMastery
    {
        public static IEnumerable<WeaponMasteryMilestone> Milestones(EquipmentDefinition definition) =>
            definition && definition.Kind==EquipmentKind.Weapon ? (definition.Mastery??new()).Where(m=>m!=null&&m.RequiredExperience>0).OrderBy(m=>m.RequiredExperience) : Enumerable.Empty<WeaponMasteryMilestone>();
        public static int MaximumExperience(EquipmentDefinition definition) => Milestones(definition).Select(m=>m.RequiredExperience).DefaultIfEmpty(0).Max();
        public static int Level(OwnedEquipment weapon) => 1+Milestones(weapon?.Definition).Where(m=>weapon.MasteryExperience>=m.RequiredExperience).Select(m=>m.RequiredExperience).Distinct().Count();
        public static IEnumerable<BattleActionOption> Attacks(OwnedEquipment weapon)
        {
            if(weapon==null||!weapon.Definition)return Enumerable.Empty<BattleActionOption>();
            return (weapon.Definition.Attacks??new()).Concat(Milestones(weapon.Definition).Where(m=>weapon.MasteryExperience>=m.RequiredExperience).SelectMany(m=>m.Attacks??new()))
                .Where(a=>a!=null&&a.Category==BattleActionCategory.Attack).GroupBy(a=>a.Name).Select(g=>g.First());
        }
        public static int FinisherBonus(OwnedEquipment weapon) => weapon==null?0:Mathf.Clamp(Milestones(weapon.Definition).Where(m=>weapon.MasteryExperience>=m.RequiredExperience).Sum(m=>Mathf.Clamp(m.FinisherSkillPoints,0,9)),0,9);
    }
    // One transaction per committed Attack: hit slots and friendly fire cannot multiply XP.
    public sealed class WeaponMasteryUse
    {
        readonly OwnedEquipment weapon;
        readonly int award, finisherBonus;
        bool successful, defeated, completed;
        public WeaponMasteryUse(OwnedEquipment weapon,BattleActionOption action)
        {
            if(weapon==null||!weapon.Definition||action==null||action.Category!=BattleActionCategory.Attack||action.Effect!=ActionEffect.Damage)return;
            if(!WeaponMastery.Attacks(weapon).Any(a=>weapon.Definition.Id+":"+a.Name==action.EquipmentActionId))return;
            this.weapon=weapon;award=Mathf.Max(1,weapon.Definition.MasteryExperiencePerAttack);finisherBonus=WeaponMastery.FinisherBonus(weapon);
        }
        public void Record(bool landed,bool hostile,bool killed)
        { if(completed||weapon==null||!landed||!hostile)return;successful=true;defeated|=killed; }
        public int Complete(GridUnit source)
        {
            if(completed)return 0;completed=true;
            if(weapon==null||!successful)return 0;
            int before=weapon.MasteryExperience,maximum=WeaponMastery.MaximumExperience(weapon.Definition);
            weapon.MasteryExperience=(int)Math.Max(before,Math.Min(maximum,(long)before+award));
            if(defeated&&source!=null&&source.Health>0)source.RestoreSkillPoints(finisherBonus);
            return weapon.MasteryExperience-before;
        }
    }
}
