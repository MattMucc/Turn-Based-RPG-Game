using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class GameSaveSnapshot
{
    static Dictionary<string,SaveIdentity> Identities(IEnumerable<SaveIdentity> scope=null)
    {
        var map=new Dictionary<string,SaveIdentity>();
        foreach(var identity in scope??UnityEngine.Object.FindObjectsByType<SaveIdentity>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(i=>i.gameObject.scene==SceneManager.GetActiveScene())){
            if(string.IsNullOrEmpty(identity.Id)||map.ContainsKey(identity.Id))throw new InvalidDataException("Save identities are missing or duplicated. Prepare the scene's save identities first.");
            map.Add(identity.Id,identity);
        }return map;
    }
    static string Id(Component value){var identity=value?value.GetComponent<SaveIdentity>():null;if(!identity||string.IsNullOrEmpty(identity.Id))throw new InvalidDataException("A party member or encounter is missing its save identity.");return identity.Id;}
    public static GameSaveData Capture(GameManager manager,string checkpoint,IEnumerable<SaveIdentity> scope=null)
    {
        if(scope==null&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path))throw new InvalidDataException("Save this Unity scene before creating game saves.");
        var ids=Identities(scope);manager.BindEquipment();
        var data=new GameSaveData{Scene=SceneManager.GetActiveScene().path,SavedUtc=DateTime.UtcNow.ToString("O"),CheckpointName=checkpoint,
            CaptainName=ProgressionMenu.DisplayName(manager.BattleParty[0]),Controlled=Id(manager.PossessedCharacter),Experience=manager.Progression.Experience,
            Inventory=JsonUtility.FromJson<PartyInventory>(JsonUtility.ToJson(manager.Inventory))};
        data.HasEquipment=true;data.HasMaterialUpgrades=true;data.HasCoins=true;data.Coins=manager.Equipment.Coins;
        data.Materials=manager.Equipment.Materials.Select(m=>new MaterialStack{Id=m.Id,Name=m.Name,Quantity=m.Quantity}).ToList();
        data.Equipment=manager.Equipment.Owned.Select(e=>new SavedEquipment{InstanceId=e.InstanceId,MasteryExperience=e.MasteryExperience,UpgradeLevel=e.UpgradeLevel,DefinitionId=e.Definition?e.Definition.Id:null}).ToList();
        data.Active=manager.BattleParty.Select(Id).ToList();data.Reserve=manager.ReserveParty.Select(Id).ToList();
        foreach(var member in manager.Progression.Members){
            var saved=new SavedCharacter{Id=Id(member),Experience=manager.Progression.ExperienceFor(member),HalfRemainder=manager.Progression.HalfRemainderFor(member)};
            saved.EquipmentSlots=member.GetComponent<CharacterEquipment>()?.CaptureSlots();
            member.CaptureProgress(saved);var loadout=member.GetComponent<BattleActionLoadout>();loadout.BindInventory(manager.Inventory);loadout.InitializeFavorites();
            saved.PersonalActions=loadout.PersonalOptions.Select(a=>JsonUtility.FromJson<BattleActionOption>(JsonUtility.ToJson(a))).ToList();
            saved.Spells=loadout.LearnedSpells.Select(s=>new LearnedSpell{Id=s.Id,Experience=s.Experience}).ToList();
            foreach(var option in loadout.Options)saved.Favorites.Add(new SavedFavorite{EquipmentActionId=option.EquipmentActionId,Name=option.Name,ItemId=option.ItemId,Category=option.Category,Slot=option.FavoriteSlot});
            var menu=member.GetComponent<CharacterMenuData>();if(menu)foreach(var entry in menu.Progression)if(entry!=null)saved.Mastery.Add(new SavedMastery{Name=entry.Name,Kind=entry.Kind,Level=entry.Level,Experience=entry.Experience});
            data.Characters.Add(saved);
        }
        foreach(var pair in ids){
            var obj=pair.Value;var destructible=obj.GetComponent<BattleDestructible>();var usable=obj.GetComponent<BattleUsable>();
            data.World.Add(new SavedWorldObject{Id=pair.Key,Position=obj.transform.position,Rotation=obj.transform.rotation,Scale=obj.transform.localScale,Active=obj.gameObject.activeSelf,Health=destructible?destructible.Health:-1,Used=usable&&usable.Used});
            var group=obj.GetComponent<BattleGroup>();if(group)data.Groups.Add(group.CaptureSave(pair.Key));
        }
        Validate(data,manager,scope);return data;
    }
    public static void Validate(GameSaveData data,GameManager manager,IEnumerable<SaveIdentity> scope=null)
    {
        if(data.Scene!=SceneManager.GetActiveScene().path)throw new InvalidDataException("This save belongs to a different area. Open that area before loading it in this prototype.");
        var ids=Identities(scope);var members=manager.Progression.Members;
        if(data.Version!=1||data.Characters==null||data.Characters.Count!=members.Count||data.Active==null||data.Reserve==null||data.Active.Count!=3||data.Reserve.Count!=members.Count-3)throw new InvalidDataException("The saved party is incompatible with this project.");
        var roster=data.Active.Concat(data.Reserve).ToList();
        if(roster.Distinct().Count()!=roster.Count||!data.Active.Contains(data.Controlled)||!new HashSet<string>(roster).SetEquals(data.Characters.Select(c=>c.Id)))throw new InvalidDataException("Invalid saved party order.");
        foreach(var member in data.Characters){
            if(!ids.TryGetValue(member.Id,out var identity)||!identity.GetComponent<BattleCharacterInfo>()||!members.Contains(identity.GetComponent<BattleCharacterInfo>())||!identity.GetComponent<PlayerMovement>()||!identity.GetComponent<BattleActionLoadout>())throw new InvalidDataException("A saved character is unavailable.");
            if(member.Level<1||member.Level>100||member.Unspent<0||member.Rewarded<1||member.Experience<0||member.HalfRemainder<0||member.HalfRemainder>1||member.Health< -1||member.Magic< -1||member.Allocated==null||member.PersonalActions==null||member.PersonalActions.Any(a=>a==null)||member.Favorites==null||member.Mastery==null)throw new InvalidDataException("Invalid character progression.");
            foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))if(member.Allocated.Get(stat)<0)throw new InvalidDataException("Invalid allocated stats.");
            if(member.Spells!=null&&(member.Spells.Any(s=>s==null||!SpellDefinition.Find(s.Id)||s.Experience<0||s.Experience>SpellDefinition.Find(s.Id).MaximumExperience)||member.Spells.Select(s=>s.Id).Distinct().Count()!=member.Spells.Count))throw new InvalidDataException("Invalid learned spell mastery.");
            if(member.Favorites.Any(f=>f==null||f.Slot< -1||f.Slot>3||!Enum.IsDefined(typeof(BattleActionCategory),f.Category)))throw new InvalidDataException("Invalid saved shortcuts.");
        }
        if(data.Inventory==null||data.Inventory.Items==null||data.Inventory.Items.Any(i=>i==null||i.Category!=BattleActionCategory.Item)||data.Experience<0||data.World==null||data.Groups==null)throw new InvalidDataException("Invalid inventory or world data.");
        if(data.World.Select(w=>w.Id).Distinct().Count()!=data.World.Count||!roster.All(id=>data.World.Any(w=>w.Id==id)))throw new InvalidDataException("Incomplete world data.");
        foreach(var world in data.World)if(!ids.ContainsKey(world.Id)||!Finite(world.Position)||!Finite(world.Scale)||!Finite(new Vector3(world.Rotation.x,world.Rotation.y,world.Rotation.z))||float.IsNaN(world.Rotation.w)||float.IsInfinity(world.Rotation.w))throw new InvalidDataException("Saved objects are missing or invalid.");
        foreach(var group in data.Groups)if(!ids.ContainsKey(group.Id)||!ids[group.Id].GetComponent<BattleGroup>()||group.Positions==null||group.Rotations==null||group.Positions.Count!=group.Rotations.Count||group.Positions.Any(p=>!Finite(p)))throw new InvalidDataException("Invalid encounter data.");
        ValidateEquipment(data,manager,ids);
        foreach(var group in data.Groups)ids[group.Id].GetComponent<BattleGroup>().ValidateSavedComposition(group);
    }
    static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
    static int Depth(Transform t){int depth=0;while(t.parent){depth++;t=t.parent;}return depth;}
    public static void Apply(GameSaveData data,GameManager manager,IEnumerable<SaveIdentity> scope=null)
    {
        Validate(data,manager,scope);var ids=Identities(scope);
        RestoreEquipment(data,manager,ids);
        manager.Inventory=data.Inventory;manager.Inventory.NotifyChanged();manager.Progression.RestoreTotal(data.Experience);
        foreach(var saved in data.Characters){
            var member=ids[saved.Id].GetComponent<BattleCharacterInfo>();member.RestoreProgress(saved);manager.Progression.RestoreMember(member,saved.Experience,saved.HalfRemainder);
            member.GetComponent<BattleActionLoadout>().RestoreSavedActions(saved,manager.Inventory);
            var menu=member.GetComponent<CharacterMenuData>();if(menu)foreach(var progress in saved.Mastery){var entry=menu.Progression.Find(e=>e!=null&&e.Name==progress.Name&&e.Kind==progress.Kind);if(entry!=null){entry.Level=progress.Level;entry.Experience=progress.Experience;}}
        }
        manager.BattleParty=data.Active.Select(id=>ids[id].GetComponent<BattleCharacterInfo>()).ToList();manager.ReserveParty=data.Reserve.Select(id=>ids[id].GetComponent<BattleCharacterInfo>()).ToList();
        foreach(var saved in data.World.OrderBy(w=>Depth(ids[w.Id].transform))){
            var obj=ids[saved.Id];var controllers=obj.GetComponentsInChildren<CharacterController>(true);var enabled=controllers.Select(c=>c.enabled).ToArray();foreach(var controller in controllers)controller.enabled=false;
            obj.transform.SetPositionAndRotation(saved.Position,saved.Rotation);obj.transform.localScale=saved.Scale;
            for(int i=0;i<controllers.Length;i++)controllers[i].enabled=enabled[i];
            var destructible=obj.GetComponent<BattleDestructible>();if(destructible)destructible.Health=Mathf.Clamp(saved.Health,0,destructible.MaxHealth);
            var usable=obj.GetComponent<BattleUsable>();if(usable)usable.RestoreUsed(saved.Used);
            obj.gameObject.SetActive(saved.Active);
        }
        foreach(var group in data.Groups)ids[group.Id].GetComponent<BattleGroup>().RestoreSave(group);
        manager.PossessCharacter(ids[data.Controlled].GetComponent<PlayerMovement>());Physics.SyncTransforms();
    }
}
