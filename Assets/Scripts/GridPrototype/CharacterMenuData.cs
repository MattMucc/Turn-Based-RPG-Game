using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public enum ProgressionEntryKind { Spell, Weapon, Gear }
    [Serializable] public sealed class UpgradeMilestone
    {
        [Min(1)] public int Level = 1;
        public string Name;
        [TextArea] public string Description;
    }
    [Serializable] public sealed class MenuProgressionEntry
    {
        public ProgressionEntryKind Kind;
        public string Name;
        public Texture Image;
        [TextArea] public string Description;
        [Min(1)] public int Level = 1, MaximumLevel = 1;
        [Min(0)] public int Experience;
        [Min(1)] public int NextLevelExperience = 100;
        public List<UpgradeMilestone> Milestones = new();
    }
    [Serializable] public sealed class MenuSkillNode
    {
        public string Id, Name;
        [TextArea] public string Description;
        public bool Passive;
        public Texture Icon;
        public Vector2 Position;
        public List<string> Prerequisites = new();
        [Min(1)] public int RequiredLevel = 1;
        [Min(0)] public int TrainingPointCost;
        [Tooltip("Name of the learned personal Skill, used to display its unlocked state.")]
        public string ActionName;
    }
    // Presentation records only. Learning/mastery awards will supply these when their systems are built.
    public sealed class CharacterMenuData : MonoBehaviour
    {
        public List<MenuProgressionEntry> Progression = new();
        public List<MenuSkillNode> SkillTree = new();
    }
}
