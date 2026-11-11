using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game.GridPrototype
{
    [Serializable] public sealed class GameSaveData
    {
        public int Version=1;
        public string Scene, SavedUtc, CaptainName, CheckpointName;
        public long Experience;
        public string Controlled;
        public List<string> Active=new(), Reserve=new();
        public PartyInventory Inventory;
        public bool HasEquipment;
        public List<SavedEquipment> Equipment = new();
        public List<SavedCharacter> Characters=new();
        public List<SavedWorldObject> World=new();
        public List<SavedGroup> Groups=new();
    }
    [Serializable] public sealed class SavedCharacter
    {
        public string Id;public int Level,Unspent,Rewarded,Health,Magic,HalfRemainder;public long Experience;
        public StatValues Allocated;
        public string[] EquipmentSlots;
        public List<BattleActionOption> PersonalActions=new();
        public List<SavedFavorite> Favorites=new();
        public List<SavedMastery> Mastery=new();
    }
    [Serializable] public sealed class SavedEquipment { public string InstanceId,DefinitionId; public int MasteryExperience; }
    [Serializable] public sealed class SavedFavorite {public string Name,ItemId,EquipmentActionId;public BattleActionCategory Category;public int Slot;}
    [Serializable] public sealed class SavedMastery {public string Name;public ProgressionEntryKind Kind;public int Level,Experience;}
    [Serializable] public sealed class SavedWorldObject {public string Id;public Vector3 Position;public Quaternion Rotation;public Vector3 Scale;public bool Active,Used;public int Health=-1;}
    [Serializable] public sealed class SavedGroup {public string Id;public bool Defeated,HasComposition;public List<string> SpawnKeys=new();public List<Vector3> Positions=new();public List<Quaternion> Rotations=new();}
}
