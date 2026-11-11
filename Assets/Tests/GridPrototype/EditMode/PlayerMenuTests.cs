using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.GridPrototype;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class PlayerMenuTests
{
    readonly List<GameObject> owned = new();
    readonly List<ScriptableObject> equipmentAssets = new();
    Type managerType, menuType;
    Component manager, menu;
    object previousManager;
    float previousTime;
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    List<BattleCharacterInfo> party, reserves;
    GameObject Make(string name) { var go = new GameObject(name); owned.Add(go); return go; }
    [SetUp] public void Setup()
    {
        previousTime = Time.timeScale;
        managerType = Type.GetType("GameManager, Assembly-CSharp", true);
        menuType = Type.GetType("ProgressionMenu, Assembly-CSharp", true);
        var singleton = managerType.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        previousManager = singleton.GetValue(null);
        manager = Make("Test manager").AddComponent(managerType); singleton.SetValue(null, manager);
        party = (List<BattleCharacterInfo>)managerType.GetField("BattleParty").GetValue(manager);
        reserves = (List<BattleCharacterInfo>)managerType.GetField("ReserveParty").GetValue(manager);
        var progression = (PartyProgression)managerType.GetField("Progression").GetValue(manager);
        for (int i = 0; i < 6; i++)
        {
            var go = Make("Member " + i); go.AddComponent<CharacterController>();
            go.AddComponent(Type.GetType("PlayerMovement, Assembly-CSharp", true));
            var info = go.AddComponent<BattleCharacterInfo>(); info.DisplayName = go.name;
            go.AddComponent<BattleActionLoadout>(); go.AddComponent<DefensePrototypeSettings>();
            progression.Register(info); if (i < 3) party.Add(info); else reserves.Add(info);
        }
        menu = manager.gameObject.AddComponent(menuType);
        managerType.GetField("<ProgressionMenu>k__BackingField", Hidden).SetValue(manager, menu);
    }
    [TearDown] public void Cleanup()
    {
        if (menu) menuType.GetMethod("Close").Invoke(menu, null);
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i]) Object.DestroyImmediate(owned[i]);
        owned.Clear(); foreach(var asset in equipmentAssets)if(asset)Object.DestroyImmediate(asset);equipmentAssets.Clear(); Time.timeScale = previousTime;
        managerType.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, previousManager);
    }
    void Open() => menuType.GetMethod("Open").Invoke(menu, null);
    void Page(string name) => menuType.GetMethod("ShowPage").Invoke(menu, new[] { Enum.Parse(menuType.GetNestedType("Page"), name) });
    Button Find(string name) => menu.GetComponentsInChildren<Button>(true).First(b => b.gameObject.activeInHierarchy && b.name.StartsWith(name));
    void WithSettingsSnapshot(Action action)
    {
        var keys=new[]{"PlayerMenu.Icons","PlayerMenu.Assisted","PlayerMenu.AutoCamera","PlayerMenu.OrderTop","PlayerMenu.Fullscreen"};
        var saved=keys.ToDictionary(k=>k,k=>PlayerPrefs.HasKey(k)?(int?)PlayerPrefs.GetInt(k):null);
        float volume=AudioListener.volume;float? savedVolume=PlayerPrefs.HasKey("PlayerMenu.Volume")?(float?)PlayerPrefs.GetFloat("PlayerMenu.Volume"):null;
        try{action();}finally{
            foreach(var pair in saved){if(pair.Value.HasValue)PlayerPrefs.SetInt(pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(pair.Key);}
            if(savedVolume.HasValue)PlayerPrefs.SetFloat("PlayerMenu.Volume",savedVolume.Value);else PlayerPrefs.DeleteKey("PlayerMenu.Volume");
            AudioListener.volume=volume;PlayerPrefs.Save();
        }
    }
    [Test] public void SettingsVolumeRetainsFocusAndDefaultsRequireConfirmation()
    {
        WithSettingsSnapshot(()=>{
            Open();Page("Settings");Find("Audio").onClick.Invoke();AudioListener.volume=.5f;
            Find("Volume -").onClick.Invoke();Assert.AreEqual(.45f,AudioListener.volume,.001f);Assert.AreEqual(.45f,PlayerPrefs.GetFloat("PlayerMenu.Volume"),.001f);
            var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
            Assert.AreEqual("Volume -",events.currentSelectedGameObject.name);
            Find("Restore defaults").onClick.Invoke();Assert.AreEqual(.45f,AudioListener.volume,.001f);
            Find("Cancel defaults").onClick.Invoke();Assert.AreEqual(.45f,AudioListener.volume,.001f);
            Find("Restore defaults").onClick.Invoke();Find("Confirm defaults").onClick.Invoke();Assert.AreEqual(1,AudioListener.volume);
            Find("Volume +").onClick.Invoke();Assert.AreEqual(1,AudioListener.volume);
        });
    }
    [Test] public void SettingsBattleAppliesToWholeRosterAndSavedPreferencesReload()
    {
        WithSettingsSnapshot(()=>{
            PlayerPrefs.SetInt("PlayerMenu.Assisted",0);PlayerPrefs.SetInt("PlayerMenu.AutoCamera",1);PlayerPrefs.SetInt("PlayerMenu.OrderTop",1);
            Open();Page("Settings");Find("Battle").onClick.Invoke();Find("Defense timing").onClick.Invoke();
            foreach(var member in party.Concat(reserves))Assert.IsTrue(member.GetComponent<DefensePrototypeSettings>().Assisted);
            Find("Automatic battle camera").onClick.Invoke();Find("Turn order").onClick.Invoke();
            Assert.AreEqual(0,PlayerPrefs.GetInt("PlayerMenu.AutoCamera"));Assert.AreEqual(0,PlayerPrefs.GetInt("PlayerMenu.OrderTop"));
            foreach(var member in party.Concat(reserves))member.GetComponent<DefensePrototypeSettings>().Assisted=false;
            menuType.GetMethod("ApplySavedMenuSettings",Hidden).Invoke(menu,null);
            foreach(var member in party.Concat(reserves))Assert.IsTrue(member.GetComponent<DefensePrototypeSettings>().Assisted);
            Find("Restore defaults").onClick.Invoke();Find("Confirm defaults").onClick.Invoke();
            foreach(var member in party.Concat(reserves))Assert.IsFalse(member.GetComponent<DefensePrototypeSettings>().Assisted);
            Assert.AreEqual(1,PlayerPrefs.GetInt("PlayerMenu.AutoCamera"));Assert.AreEqual(1,PlayerPrefs.GetInt("PlayerMenu.OrderTop"));
        });
    }
    [Test] public void DisplaySelectionIsStagedAndBackDoesNotChangeTheScreen()
    {
        WithSettingsSnapshot(()=>{
            bool original=Screen.fullScreen;Open();Page("Settings");Find("Display").onClick.Invoke();
            Find("Display mode").onClick.Invoke();Assert.AreEqual(original,Screen.fullScreen);Assert.IsTrue(Find("Apply display change").interactable);
            menuType.GetMethod("Back").Invoke(menu,null);Assert.AreEqual(original,Screen.fullScreen);Assert.IsFalse(Find("Apply display change").interactable);
            // Validate the deadline independently; never exercise Screen setters in editor tests.
            var preview=new DisplayChangePreview();preview.Begin(false,true,100);
            Assert.IsTrue(preview.Active);Assert.IsFalse(preview.Original);Assert.IsTrue(preview.Requested);
            Assert.IsFalse(preview.Expired(114.99f));Assert.IsTrue(preview.Expired(115));preview.End();Assert.IsFalse(preview.Expired(120));
        });
    }
    EquipmentDefinition EquipmentAsset(string id,EquipmentKind kind=EquipmentKind.Weapon,WeaponType type=WeaponType.Shortsword)
    {
        var item=ScriptableObject.CreateInstance<EquipmentDefinition>();equipmentAssets.Add(item);item.Id=id;item.DisplayName=id;item.Kind=kind;item.WeaponType=type;item.Bonuses.Strength=3;
        if(kind==EquipmentKind.Weapon)item.Attacks.Add(new BattleActionOption{Name=id+" hit",Damage=12,Reach=2,Pattern=BattleTargetPattern.Sweep,Radius=1});return item;
    }
    [Test] public void AbilityBrowserShowsLockedAttacksPerCopyWithoutGrantingThem()
    {
        var inventory=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);
        var sword=EquipmentAsset("Browser sword");
        sword.Mastery.Add(new WeaponMasteryMilestone{RequiredExperience=10,Name="Technique",Attacks=new(){new BattleActionOption{Name="Locked sweep",Pattern=BattleTargetPattern.Sweep,Reach=2,Radius=1}}});
        inventory.Catalog.Add(sword);
        inventory.Owned.Add(new OwnedEquipment{InstanceId="new",Definition=sword});
        inventory.Owned.Add(new OwnedEquipment{InstanceId="mastered",Definition=sword,MasteryExperience=10});
        var equipment=party[0].gameObject.AddComponent<CharacterEquipment>();equipment.AllowedWeaponTypes=new[]{WeaponType.Shortsword};
        managerType.GetMethod("BindEquipment").Invoke(manager,null);equipment.ApplySlot(EquipmentSlot.Weapon,"new");
        var loadout=party[0].GetComponent<BattleActionLoadout>();
        Open();Page("Abilities");Find("Locked sweep · Locked").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Requires 10 cumulative weapon XP")));
        Assert.IsTrue(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Pattern Locked sweep"));
        Assert.IsFalse(loadout.Options.Any(a=>a.Name=="Locked sweep"));
        Assert.AreEqual(0,inventory.Owned[0].MasteryExperience);
        Find("Locked sweep · Unlocked").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Weapon XP 10 / 10")));
        Assert.AreEqual("new",equipment.InstanceAt(EquipmentSlot.Weapon));
    }
    [Test] public void AbilityBrowserShowsSkillsSpellsAndEmptyCategoryWithoutChangingFavorites()
    {
        var loadout=party[0].GetComponent<BattleActionLoadout>();
        loadout.PersonalOptions.Add(new BattleActionOption{Name="Browser spell",Category=BattleActionCategory.Magic,FavoriteSlot=2});
        Open();Page("Abilities");Find("Spells").onClick.Invoke();Find("Browser spell").onClick.Invoke();
        Assert.AreEqual(2,loadout.PersonalOptions.First(a=>a.Name=="Browser spell").FavoriteSlot);
        menuType.GetMethod("Cycle",Hidden).Invoke(menu,new object[]{1});
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="No spells available"));
        Find("Skills").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Dash")));
        Find("Dash").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Self only")));
        Assert.IsFalse(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Pattern Dash"));
    }
    [Test] public void SpellBookMenuConfirmsLearnerAndBrowserShowsLockedTiers()
    {
        var inventory=(PartyInventory)managerType.GetField("Inventory").GetValue(manager);
        var book=new BattleActionOption{Name="Fireball Spellbook",ItemId="spellbook-fireball",LearnSpellId="fireball",Category=BattleActionCategory.Item,Quantity=2};inventory.Items.Add(book);
        Open();Page("Inventory");Find("Fireball Spellbook").onClick.Invoke();Find("Use").onClick.Invoke();Find("Member 0   Learn spell").onClick.Invoke();
        Assert.AreEqual(2,book.Quantity);Find("Back").onClick.Invoke();Assert.AreEqual(2,book.Quantity);
        Find("Member 0   Learn spell").onClick.Invoke();Find("Confirm learning").onClick.Invoke();
        Assert.AreEqual(1,book.Quantity);Assert.IsTrue(party[0].GetComponent<BattleActionLoadout>().KnowsSpell("fireball"));Assert.IsFalse(party[1].GetComponent<BattleActionLoadout>().KnowsSpell("fireball"));
        Page("Abilities");Find("Spells").onClick.Invoke();Find("Fireball III · Locked").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Requires 25 cumulative Spell XP")));
        Assert.IsFalse(party[0].GetComponent<BattleActionLoadout>().Options.Any(s=>s.Name=="Fireball III"));
    }
    [Test] public void AbilityPatternUsesLegalCustomAnchorAndRetainsSelfHitWarning()
    {
        var loadout=party[0].GetComponent<BattleActionLoadout>();
        var action=new BattleActionOption{Name="Custom preview",Category=BattleActionCategory.Skill,Pattern=BattleTargetPattern.CustomSingle,Reach=1};
        for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)if(x!=1||y!=0)action.DisabledPatternCells.Add(new Vector2Int(x,y));
        loadout.PersonalOptions.Add(action);
        Open();Page("Abilities");Find("Skills").onClick.Invoke();Find("Custom preview").onClick.Invoke();
        var frame=menu.GetComponentsInChildren<Transform>().Single(t=>t.name=="Pattern Custom preview");
        var aim=frame.GetComponentsInChildren<Transform>().Single(t=>t.name=="Aim marker");
        var tile=(RectTransform)aim.parent;
        Assert.Greater(tile.anchoredPosition.x,frame.GetComponentsInChildren<Transform>().Single(t=>t.name=="User marker").parent.GetComponent<RectTransform>().anchoredPosition.x);
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("self-hit is possible")));
    }
    [Test] public void UpgradeMenuPreviewsCancelsAndConfirmsOneCopyWithoutHealing()
    {
        var inventory=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);var item=EquipmentAsset("Upgrade sword");
        item.Upgrades.Add(new EquipmentUpgradeStep{Materials=new(){new(){MaterialId="iron",Quantity=2}},AddedStats=new StatValues{Vitality=2,Magic=2,Strength=1}});
        inventory.Coins=100;inventory.Catalog.Add(item);inventory.Materials.Add(new MaterialStack{Id="iron",Name="Iron",Quantity=3});
        inventory.Owned.Add(new OwnedEquipment{InstanceId="one",Definition=item});inventory.Owned.Add(new OwnedEquipment{InstanceId="two",Definition=item});
        var profile=party[0].gameObject.AddComponent<CharacterEquipment>();profile.AllowedWeaponTypes=new[]{WeaponType.Shortsword};profile.RestoreSlots(new[]{"one",null,null,null});
        managerType.GetMethod("BindEquipment").Invoke(manager,null);party[0].MaterializeVitals();int hp=party[0].StoredHealth,mp=party[0].StoredMagicPoints;
        Open();Page("Upgrades");Assert.IsFalse(Find("Review upgrade").interactable);
        object[] outside={"one",0,null};Assert.IsFalse((bool)managerType.GetMethod("TryUpgradeEquipment").Invoke(manager,outside));
        menuType.GetMethod("Close").Invoke(menu,null);menuType.GetField("closedFrame",Hidden).SetValue(menu,-1);
        var movementType=Type.GetType("PlayerMovement, Assembly-CSharp",true);managerType.GetField("<PossessedCharacter>k__BackingField",Hidden).SetValue(manager,party[0].GetComponent(movementType));
        var smith=Make("Test blacksmith").AddComponent(Type.GetType("Blacksmith, Assembly-CSharp",true));smith.transform.position=party[0].transform.position;
        menuType.GetMethod("OpenBlacksmith").Invoke(menu,new object[]{smith});
        smith.transform.position+=Vector3.right*10;Assert.IsFalse((bool)managerType.GetMethod("TryUpgradeEquipment").Invoke(manager,outside));smith.transform.position=party[0].transform.position;
        Find("Review upgrade").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Iron: 2 / 3")));
        Assert.AreEqual(0,inventory.Owned[0].UpgradeLevel);Find("Cancel upgrade").onClick.Invoke();Assert.AreEqual(3,inventory.Materials[0].Quantity);
        Find("Review upgrade").onClick.Invoke();Find("Confirm upgrade").onClick.Invoke();
        Assert.AreEqual(1,inventory.Owned[0].UpgradeLevel);Assert.AreEqual(0,inventory.Owned[1].UpgradeLevel);Assert.AreEqual(1,inventory.Materials[0].Quantity);Assert.AreEqual(75,inventory.Coins);
        Assert.AreEqual(hp,party[0].StoredHealth);Assert.AreEqual(mp,party[0].StoredMagicPoints);Assert.IsFalse(Find("Review upgrade").interactable);
    }
    [Test] public void EquipmentMenuConfirmsOwnershipAndShowsPatternsWithoutGrantingMastery()
    {
        var equipment=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);
        var sword=EquipmentAsset("Test sword");var bow=EquipmentAsset("Test bow",EquipmentKind.Weapon,WeaponType.Bow);
        equipment.Catalog.AddRange(new[]{sword,bow});equipment.Owned.Add(new OwnedEquipment{InstanceId="s",Definition=sword});equipment.Owned.Add(new OwnedEquipment{InstanceId="b",Definition=bow});
        var profile=party[0].gameObject.AddComponent<CharacterEquipment>();profile.AllowedWeaponTypes=new[]{WeaponType.Shortsword};
        var other=party[1].gameObject.AddComponent<CharacterEquipment>();other.AllowedWeaponTypes=new[]{WeaponType.Shortsword};
        Open();Page("Equipment");Find("Test sword").onClick.Invoke();Assert.IsNull(profile.At(EquipmentSlot.Weapon));
        Assert.IsTrue(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Pattern Test sword hit"));
        Find("Cancel").onClick.Invoke();Assert.IsNull(profile.At(EquipmentSlot.Weapon));
        Find("Test sword").onClick.Invoke();Find("Confirm equipment").onClick.Invoke();Assert.AreEqual(sword,profile.At(EquipmentSlot.Weapon));
        object[] request={party[1],EquipmentSlot.Weapon,"s",null};Assert.IsFalse((bool)managerType.GetMethod("TryEquip").Invoke(manager,request));
        request=new object[]{party[0],EquipmentSlot.Weapon,"b",null};Assert.IsFalse((bool)managerType.GetMethod("TryEquip").Invoke(manager,request));
        Page("Progression");Find("Weapons").onClick.Invoke();Find("Test sword").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Pattern Test sword hit"));
        Assert.IsFalse(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Mastery experience"));
        var battleType=Type.GetType("BattleManager, Assembly-CSharp",true);var singleton=battleType.GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);var previous=singleton.GetValue(null);
        try{var battle=Make("Equipment combat guard").AddComponent(battleType);singleton.SetValue(null,battle);battleType.GetField("<InBattle>k__BackingField",Hidden).SetValue(battle,true);
            request=new object[]{party[0],EquipmentSlot.Weapon,null,null};Assert.IsFalse((bool)managerType.GetMethod("TryEquip").Invoke(manager,request));Assert.AreEqual(sword,profile.At(EquipmentSlot.Weapon));
        }finally{singleton.SetValue(null,previous);}
    }
    [Test] public void SaveSnapshotRoundTripRestoresRosterPoolsXpInventoryAndEmptyShortcuts()
    {
        var members=party.Concat(reserves).ToList();var ids=new List<SaveIdentity>();
        foreach(var member in members){var identity=member.gameObject.AddComponent<SaveIdentity>();identity.AssignEditorId(Guid.NewGuid().ToString("N"));ids.Add(identity);}
        var crate=Make("Saved crate");var crateId=crate.AddComponent<SaveIdentity>();crateId.AssignEditorId(Guid.NewGuid().ToString("N"));ids.Add(crateId);
        var destructible=crate.AddComponent<BattleDestructible>();var usable=crate.AddComponent<BattleUsable>();usable.OneUseOnly=true;
        var movementType=Type.GetType("PlayerMovement, Assembly-CSharp",true);
        managerType.GetField("<PossessedCharacter>k__BackingField",Hidden).SetValue(manager,members[1].GetComponent(movementType));
        var progression=(PartyProgression)managerType.GetField("Progression").GetValue(manager);progression.AwardExperience(201,party);progression.AwardExperience(1,party);
        var original=members[0];original.SpendPoint(CharacterStat.Magic);
        var unit=new GridUnit("Saved",Vector2Int.zero);original.Configure(unit);unit.Health=0;unit.InitializeMagicPoints(unit.MaxMagicPoints,7);original.StoreHealth(unit);original.StoreMagicPoints(unit);
        var inventory=(PartyInventory)managerType.GetField("Inventory").GetValue(manager);inventory.Items[0].Quantity=1;
        var loadout=original.GetComponent<BattleActionLoadout>();loadout.BindInventory(inventory);loadout.InitializeFavorites();
        var attack=loadout.Options.First(o=>o.Category==BattleActionCategory.Attack);loadout.AssignFavorite(attack,-1);
        var gear=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);gear.Coins=83;var sword=EquipmentAsset("Saved sword");sword.Upgrades.Add(new EquipmentUpgradeStep{AddedStats=new StatValues{Strength=2}});gear.Materials.Add(new MaterialStack{Id="iron",Name="Iron",Quantity=7});gear.Catalog.Add(sword);gear.Owned.Add(new OwnedEquipment{InstanceId="saved-copy",Definition=sword,MasteryExperience=7,UpgradeLevel=1});
        var equipment=original.gameObject.AddComponent<CharacterEquipment>();equipment.AllowedWeaponTypes=new[]{WeaponType.Shortsword};managerType.GetMethod("BindEquipment").Invoke(manager,null);equipment.ApplySlot(EquipmentSlot.Weapon,"saved-copy");
        loadout.AssignFavorite(loadout.Options.First(a=>a.Name=="Saved sword hit"),2);
        attack=loadout.Options.First(a=>a.Effect==ActionEffect.Push);loadout.AssignFavorite(attack,-1);
        original.transform.position=new Vector3(40,2,30);
        loadout.LearnSpell("fireball");for(int i=0;i<12;i++)loadout.AwardSpellExperience("fireball");
        loadout.AssignFavorite(loadout.Options.First(a=>a.Name=="Fireball II"),1);
        var snapshotType=Type.GetType("GameSaveSnapshot, Assembly-CSharp",true);
        var data=(GameSaveData)snapshotType.GetMethod("Capture").Invoke(null,new object[]{manager,"Test checkpoint",ids});
        string testScene=data.Scene;data.Scene="Assets/Scenes/SampleScene.unity";data=SaveFileStore.Decode(SaveFileStore.Encode(data));data.Scene=testScene;int points=original.UnspentPoints;long xp=progression.ExperienceFor(original);int allocated=original.AllocatedPoints.Magic;
        equipment.ApplySlot(EquipmentSlot.Weapon,null);gear.Owned.Clear();gear.Materials[0].Quantity=0;gear.Coins=0;
        loadout.AwardSpellExperience("fireball");
        inventory.Items[0].Quantity=0;progression.AwardExperience(1000);original.transform.position=Vector3.zero;party.Reverse();usable.TryUse();destructible.TakeDamage(50);
        snapshotType.GetMethod("Apply").Invoke(null,new object[]{data,manager,ids});
        Assert.AreEqual("saved-copy",equipment.InstanceAt(EquipmentSlot.Weapon));Assert.AreEqual(sword,equipment.At(EquipmentSlot.Weapon));Assert.AreEqual(1,gear.Owned.Count);Assert.AreEqual(7,gear.Owned[0].MasteryExperience);Assert.AreEqual(1,gear.Owned[0].UpgradeLevel);Assert.AreEqual(7,gear.Materials[0].Quantity);Assert.AreEqual(83,gear.Coins);
        Assert.AreEqual("Saved sword hit",loadout.Favorite(BattleActionCategory.Attack,2).Name);
        Assert.AreEqual(12,loadout.LearnedSpells.Single(s=>s.Id=="fireball").Experience);
        Assert.AreEqual("Fireball II",loadout.Favorite(BattleActionCategory.Magic,1).Name);
        Assert.IsFalse(members[1].GetComponent<BattleActionLoadout>().KnowsSpell("fireball"));
        var equipmentCopy=data.Characters.First(c=>c.Id==original.GetComponent<SaveIdentity>().Id);string previousSlot=equipmentCopy.EquipmentSlots[0];equipmentCopy.EquipmentSlots[0]="missing-copy";
        Assert.Throws<TargetInvocationException>(()=>snapshotType.GetMethod("Validate").Invoke(null,new object[]{data,manager,ids}));Assert.AreEqual(sword,equipment.At(EquipmentSlot.Weapon));equipmentCopy.EquipmentSlots[0]=previousSlot;
        Assert.AreEqual(50,destructible.Health);Assert.IsTrue(crate.activeSelf);Assert.IsTrue(usable.Available);
        Assert.AreEqual(0,original.StoredHealth);Assert.AreEqual(7,original.StoredMagicPoints);Assert.AreEqual(points,original.UnspentPoints);Assert.AreEqual(allocated,original.AllocatedPoints.Magic);
        Assert.AreEqual(xp,progression.ExperienceFor(original));Assert.AreEqual(data.Characters[3].HalfRemainder,progression.HalfRemainderFor(members[3]));
        Assert.AreEqual(new Vector3(40,2,30),original.transform.position);
        var restoredParty=(List<BattleCharacterInfo>)managerType.GetField("BattleParty").GetValue(manager);Assert.AreEqual(original,restoredParty[0]);
        Assert.AreEqual(members[1].GetComponent(movementType),managerType.GetProperty("PossessedCharacter").GetValue(manager));
        var restoredInventory=(PartyInventory)managerType.GetField("Inventory").GetValue(manager);Assert.AreEqual(1,restoredInventory.Count("health-potion"));
        Assert.AreEqual(-1,loadout.Options.First(o=>o.Name==attack.Name).FavoriteSlot);loadout.InitializeFavorites();Assert.AreEqual(-1,loadout.Options.First(o=>o.Name==attack.Name).FavoriteSlot);
        progression.Register(original);Assert.AreEqual(points,original.UnspentPoints,"Loading must not grant level rewards a second time.");
        data.Active[0]="missing";Assert.Throws<TargetInvocationException>(()=>snapshotType.GetMethod("Apply").Invoke(null,new object[]{data,manager,ids}));Assert.AreEqual(1,restoredInventory.Count("health-potion"));
    }
    [Test] public void SavesPageConfirmsOverwriteAndLoadAndServiceRejectsBattle()
    {
        string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"rpg-menu-save-test-"+Guid.NewGuid().ToString("N"));
        var store=new SaveFileStore(directory);var saved=new GameSaveData{Scene="Assets/Scenes/SampleScene.unity",CaptainName="Test captain",SavedUtc=DateTime.UtcNow.ToString("O"),Inventory=new PartyInventory()};saved.Characters.Add(new SavedCharacter{Id="test"});
        store.Write(0,SaveFileStore.Encode(saved),false);
        var movementType=Type.GetType("PlayerMovement, Assembly-CSharp",true);managerType.GetField("<PossessedCharacter>k__BackingField",Hidden).SetValue(manager,party[0].GetComponent(movementType));
        var service=(Component)managerType.GetProperty("Saves").GetValue(manager);service.GetType().GetField("store",Hidden).SetValue(service,store);
        var battleType=Type.GetType("BattleManager, Assembly-CSharp",true);var singleton=battleType.GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);var previous=singleton.GetValue(null);
        try{
            Open();Page("Saves");Assert.IsFalse(Find("Load checkpoint").interactable);Find("Save slot 1").onClick.Invoke();
            Assert.IsNotNull(Find("Confirm overwrite"));Find("Cancel save/load").onClick.Invoke();Assert.AreEqual("Test captain",store.Read(0,out _).CaptainName);
            Find("Load slot 1").onClick.Invoke();Assert.IsNotNull(Find("Confirm load"));menuType.GetMethod("Back").Invoke(menu,null);
            var battle=Make("Test save guard").AddComponent(battleType);singleton.SetValue(null,battle);battleType.GetField("<InBattle>k__BackingField",Hidden).SetValue(battle,true);
            Assert.IsFalse((bool)service.GetType().GetProperty("CanOperate").GetValue(service));
            battleType.GetField("<InBattle>k__BackingField",Hidden).SetValue(battle,false);Assert.IsTrue((bool)service.GetType().GetProperty("CanOperate").GetValue(service));
        }finally{singleton.SetValue(null,previous);if(System.IO.Directory.Exists(directory)){Assert.IsTrue(System.IO.Path.GetFullPath(directory).StartsWith(System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()),StringComparison.OrdinalIgnoreCase));System.IO.Directory.Delete(directory,true);}}
    }
    [Test] public void CheckpointMenuRequiresInteractionAndRestConfirmationWhileSaveDoesNotHeal()
    {
        var movementType=Type.GetType("PlayerMovement, Assembly-CSharp",true);managerType.GetField("<PossessedCharacter>k__BackingField",Hidden).SetValue(manager,party[0].GetComponent(movementType));
        var unit=new GridUnit("KO",Vector2Int.zero);party[0].Configure(unit);unit.Health=0;party[0].StoreHealth(unit);
        var checkpoint=Make("Checkpoint UI test").AddComponent(Type.GetType("SaveCheckpoint, Assembly-CSharp",true));
        menuType.GetMethod("OpenCheckpoint").Invoke(menu,new object[]{checkpoint});Find("Rest").onClick.Invoke();Assert.IsNotNull(Find("Confirm rest"));
        Assert.AreEqual(0,party[0].StoredHealth);Find("Cancel rest").onClick.Invoke();Find("Save").onClick.Invoke();
        Assert.IsNotNull(Find("Save slot 3"));Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Load slot")));Assert.AreEqual(0,party[0].StoredHealth);
        menuType.GetMethod("Back").Invoke(menu,null);Find("Leave").onClick.Invoke();Assert.IsFalse((bool)menuType.GetProperty("IsOpen").GetValue(menu));
    }
    [Test] public void RestRecoversReservesResetsOnlyRoamingAndRestoresExactSavedComposition()
    {
        var randomState=UnityEngine.Random.state;
        try{
            var origin=new Vector3(30000,0,30000);foreach(var member in party.Concat(reserves)){
                member.transform.position=origin;var unit=new GridUnit("KO",Vector2Int.zero);member.Configure(unit);unit.Health=0;unit.InitializeMagicPoints(unit.MaxMagicPoints,0);member.StoreHealth(unit);member.StoreMagicPoints(unit);
            }
            var movementType=Type.GetType("PlayerMovement, Assembly-CSharp",true);managerType.GetField("<PossessedCharacter>k__BackingField",Hidden).SetValue(manager,party[0].GetComponent(movementType));
            var checkpoint=Make("Rest checkpoint").AddComponent(Type.GetType("SaveCheckpoint, Assembly-CSharp",true));checkpoint.transform.position=origin;
            var floor=Make("Rest test ground");floor.transform.position=origin+new Vector3(15,-.5f,0);var floorCollider=floor.AddComponent<BoxCollider>();floorCollider.size=new Vector3(40,1,30);
            var prefab=Make("Test enemy definition");prefab.transform.position=origin+Vector3.up*100;
            var groupType=Type.GetType("BattleGroup, Assembly-CSharp",true);var formationType=Type.GetType("RoamingFormation, Assembly-CSharp",true);var entryType=Type.GetType("EnemySpawnEntry, Assembly-CSharp",true);
            var groups=Array.CreateInstance(groupType,3);
            for(int i=0;i<3;i++){
                var go=Make("Rest group "+i);go.transform.position=origin+new Vector3(16,1,0);var bounds=go.AddComponent<BoxCollider>();bounds.isTrigger=true;bounds.size=new Vector3(18,4,18);
                var group=go.AddComponent(groupType);groups.SetValue(group,i);groupType.GetField("Kind").SetValue(group,(EncounterKind)i);groupType.GetField("experienceClaimed",Hidden).SetValue(group,true);
                var pool=(System.Collections.IList)groupType.GetField("RoamingPool").GetValue(group);
                for(int count=2;count<=3;count++){
                    var formation=Activator.CreateInstance(formationType);var entry=Activator.CreateInstance(entryType);entryType.GetField("SaveKey").SetValue(entry,"variant-"+count);entryType.GetField("enemyPrefab").SetValue(entry,prefab);entryType.GetField("spawnAmount").SetValue(entry,count);
                    var entries=Array.CreateInstance(entryType,1);entries.SetValue(entry,0);formationType.GetField("Enemies").SetValue(formation,entries);pool.Add(formation);
                }
            }
            Physics.SyncTransforms();var rest=Type.GetType("CheckpointRest, Assembly-CSharp",true).GetMethod("TryApply");var args=new object[]{manager,checkpoint,null,groups};
            Assert.IsTrue((bool)rest.Invoke(null,args),args[2] as string);
            foreach(var member in party.Concat(reserves)){Assert.Greater(member.StoredHealth,0);Assert.AreEqual(member.EffectiveMaxMagicPoints,member.StoredMagicPoints);}
            Assert.IsFalse((bool)groupType.GetProperty("Defeated").GetValue(groups.GetValue(0)));Assert.IsTrue((bool)groupType.GetProperty("Defeated").GetValue(groups.GetValue(1)));Assert.IsTrue((bool)groupType.GetProperty("Defeated").GetValue(groups.GetValue(2)));
            var roaming=groups.GetValue(0);var saved=(SavedGroup)groupType.GetMethod("CaptureSave").Invoke(roaming,new object[]{"test-group"});Assert.That(saved.SpawnKeys.Count,Is.InRange(2,3));
            foreach(var position in saved.Positions)Assert.Greater(Vector3.Distance(position,origin),5);
            foreach(var formation in (System.Collections.IList)groupType.GetField("RoamingPool").GetValue(roaming))formationType.GetField("Weight").SetValue(formation,0);
            groupType.GetMethod("RestoreSave").Invoke(roaming,new object[]{saved});var restored=(SavedGroup)groupType.GetMethod("CaptureSave").Invoke(roaming,new object[]{"test-group"});CollectionAssert.AreEqual(saved.SpawnKeys,restored.SpawnKeys);CollectionAssert.AreEqual(saved.Positions,restored.Positions);
            Assert.Greater((int)groupType.GetMethod("ClaimVictoryExperience").Invoke(roaming,null),0);Assert.AreEqual(0,(int)groupType.GetMethod("ClaimVictoryExperience").Invoke(roaming,null));
            // Invalid pools fail before recovery or resetting any encounter.
            var ko=new GridUnit("KO",Vector2Int.zero){Health=0};party[0].StoreHealth(ko);Assert.IsFalse((bool)rest.Invoke(null,args));Assert.AreEqual(0,party[0].StoredHealth);
        }finally{UnityEngine.Random.state=randomState;}
    }
    [Test] public void InventoryContextExaminesThenPreviewsReservePotionAndCancelsWithoutConsumption()
    {
        var unit=new GridUnit("test",Vector2Int.zero);reserves[0].Configure(unit);unit.Health=0;reserves[0].StoreHealth(unit);
        var inventory=(PartyInventory)managerType.GetField("Inventory").GetValue(manager);
        Open();Page("Inventory");
        var itemButton=Find("Health Potion");
        var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
        var pointer=new UnityEngine.EventSystems.PointerEventData(events){button=UnityEngine.EventSystems.PointerEventData.InputButton.Right};
        UnityEngine.EventSystems.ExecuteEvents.Execute(itemButton.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
        Assert.IsTrue(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Item hover tooltip"));
        UnityEngine.EventSystems.ExecuteEvents.Execute(itemButton.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        Assert.IsFalse(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Item hover tooltip"));
        Find("Examine").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("knocked-out")));
        menuType.GetMethod("Back").Invoke(menu,null);Find("Use").onClick.Invoke();Find("Member 3").onClick.Invoke();
        Assert.AreEqual(0,reserves[0].StoredHealth);Assert.AreEqual(3,inventory.Count("health-potion"));
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Recovers 50 HP")));
        menuType.GetMethod("Back").Invoke(menu,null);Assert.AreEqual(3,inventory.Count("health-potion"));
        Find("Member 3").onClick.Invoke();Find("Confirm use").onClick.Invoke();
        Assert.AreEqual(50,reserves[0].StoredHealth);Assert.AreEqual(2,inventory.Count("health-potion"));
        Assert.IsFalse(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Inventory interaction"));
    }
    [Test] public void MenuPausesRestoresAndBuildsEveryPageWithoutRuntimePlaytesting()
    {
        Time.timeScale = .75f; Open(); Assert.AreEqual(0, Time.timeScale);
        Assert.IsTrue((bool)menuType.GetProperty("BlocksWorldInput").GetValue(menu));
        foreach (var page in new[] { "Party", "Attributes", "Shortcuts", "Progression", "SkillTree", "Inventory", "Settings" })
        {
            Page(page);
            var events = (UnityEngine.EventSystems.EventSystem)menuType.GetField("events", Hidden).GetValue(menu);
            Assert.IsNotNull(events.currentSelectedGameObject, "Controller navigation must remain available on empty pages: " + page);
            Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t => !string.IsNullOrEmpty(t.text)), page);
            foreach (var rect in menu.GetComponentsInChildren<RectTransform>()) Assert.IsFalse(float.IsNaN(rect.rect.height), rect.name);
        }
        menuType.GetMethod("Close").Invoke(menu, null); Assert.AreEqual(.75f, Time.timeScale);
        Assert.IsFalse((bool)menuType.GetProperty("IsOpen").GetValue(menu));
    }
    [Test] public void MenuAssignsFavoritesToExistingBattleBindingsWithoutChangingSharedStock()
    {
        Open(); Page("Shortcuts"); Find("Strike").onClick.Invoke(); Find("Shortcut 4").onClick.Invoke();
        var loadout = party[0].GetComponent<BattleActionLoadout>();
        Assert.AreEqual(0,loadout.Options.Find(o=>o.Name=="Strike").FavoriteSlot,"Preview must not change the binding.");
        Find("Confirm shortcut").onClick.Invoke();
        Assert.AreEqual("Strike", loadout.Favorite(BattleActionCategory.Attack, 3).Name);
        var inventory = (PartyInventory)managerType.GetField("Inventory").GetValue(manager);
        Assert.AreEqual(3, inventory.Count("mana-potion")); Assert.AreEqual(3, inventory.Count("health-potion"));
    }
    [Test] public void ShortcutReplacementCanBeCancelledAndPreviewExplainsDisplacedAbility()
    {
        Open(); Page("Shortcuts");
        var loadout=party[0].GetComponent<BattleActionLoadout>();
        Find("Shortcut 2").onClick.Invoke();Find("Strike").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Piercing Attack loses this shortcut")));
        Find("Cancel").onClick.Invoke();Assert.AreEqual("Strike",loadout.Favorite(BattleActionCategory.Attack,0).Name);
        Assert.AreEqual("Piercing Attack",loadout.Favorite(BattleActionCategory.Attack,1).Name);
        Find("Strike").onClick.Invoke();Find("Shortcut 2").onClick.Invoke();Find("Confirm shortcut").onClick.Invoke();
        Assert.AreEqual("Strike",loadout.Favorite(BattleActionCategory.Attack,1).Name);Assert.IsNull(loadout.Favorite(BattleActionCategory.Attack,0));
        Assert.AreEqual(-1,loadout.Options.Find(o=>o.Name=="Piercing Attack").FavoriteSlot);
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Piercing Attack"));
    }
    [Test] public void ShortcutGlyphsResolveKeyboardXboxAndPlayStationWithoutControllerPairing()
    {
        Open();Page("Shortcuts");
        var glyphType=Type.GetType("PlayerShortcutGlyph, Assembly-CSharp",true);
        var readerType=Type.GetType("PlayerInputReader, Assembly-CSharp",true);
        var source=Make("Input reader").AddComponent(readerType);
        managerType.GetField("<Input>k__BackingField",Hidden).SetValue(manager,source);
        var style=readerType.GetField("ControllerIcons");
        var glyph=menu.GetComponentInChildren(glyphType);
        foreach(string scheme in new[]{"Xbox","PlayStation"})
        {
            style.SetValue(source,Enum.Parse(style.FieldType,scheme));glyphType.GetMethod("Refresh").Invoke(glyph,new object[]{true});
            Assert.IsTrue(glyph.transform.Find("Trigger").gameObject.activeSelf);
            Assert.IsNotNull(glyph.transform.Find("Key").GetComponent<Image>().sprite);
        }
        glyphType.GetMethod("Refresh").Invoke(glyph,new object[]{false});
        Assert.IsFalse(glyph.transform.Find("Trigger").gameObject.activeSelf);
        Assert.IsNotNull(glyph.transform.Find("Key").GetComponent<Image>().sprite);
    }
    [Test] public void CaptainSelectionReordersOnlyActiveParty()
    {
        var selected = party[2]; var position = selected.transform.position;
        Assert.IsTrue((bool)managerType.GetMethod("TrySetCaptain").Invoke(manager, new object[] { selected }));
        Assert.AreSame(selected, party[0]); Assert.AreEqual(position, selected.transform.position);
        Assert.AreEqual(3, party.Count); Assert.AreEqual(3, reserves.Count);
        Assert.IsFalse((bool)managerType.GetMethod("TrySetCaptain").Invoke(manager, new object[] { reserves[0] }));
    }
    [Test] public void MenuFrameFillsCanvasAndPartySeparatesActiveAndReserves()
    {
        Open(); Canvas.ForceUpdateCanvases();
        var frame = (RectTransform)menuType.GetField("shell", Hidden).GetValue(menu);
        Assert.AreEqual(Vector2.zero, frame.anchorMin); Assert.AreEqual(Vector2.one, frame.anchorMax);
        Assert.AreEqual(Vector2.zero, frame.offsetMin); Assert.AreEqual(Vector2.zero, frame.offsetMax);
        Assert.AreEqual(((RectTransform)frame.parent).rect.size, frame.rect.size);
        var texts = menu.GetComponentsInChildren<TMP_Text>();
        Assert.IsTrue(texts.Any(t => t.text == "ACTIVE   3 / 3"));
        Assert.IsTrue(texts.Any(t => t.text == "RESERVES   3 / 3"));
        Assert.AreEqual(6, menu.GetComponentsInChildren<RectTransform>().Count(r => r.name == "XP track"));
    }
    [Test] public void AttributePreviewAndCancelAreFreeAndConfirmationSpendsExactlyOnePoint()
    {
        var member=party[0]; member.GrantLevel(2,2); int points=member.UnspentPoints, magic=member.Stat(CharacterStat.Magic), mp=member.EffectiveMaxMagicPoints;
        Open(); Page("Attributes"); Find("Magic   ").onClick.Invoke();
        Assert.AreEqual(points, member.UnspentPoints); Assert.AreEqual(magic, member.Stat(CharacterStat.Magic));
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains($"Maximum MP: {mp} → {mp+member.MagicPointsPerMagic}")));
        menuType.GetMethod("Back").Invoke(menu,null); Assert.AreEqual(points,member.UnspentPoints);
        Find("Magic   ").onClick.Invoke(); var confirm=Find("Confirm +1 Magic"); confirm.onClick.Invoke();
        Assert.AreEqual(points-1,member.UnspentPoints); Assert.AreEqual(magic+1,member.Stat(CharacterStat.Magic));
        Assert.AreEqual(mp+member.MagicPointsPerMagic,member.EffectiveMaxMagicPoints);
        // A stale click cannot spend again after the pending choice has been cleared.
        confirm.onClick.Invoke(); Assert.AreEqual(points-1,member.UnspentPoints);
    }
    [Test] public void ChangingCharacterDiscardsPendingAttributeChoice()
    {
        party[0].GrantLevel(2,2); Open(); Page("Attributes"); Find("Vitality   ").onClick.Invoke();
        menuType.GetMethod("Cycle",Hidden).Invoke(menu,new object[]{1});
        Assert.AreEqual(2,party[0].UnspentPoints); Assert.AreEqual(0,party[1].UnspentPoints);
        Assert.IsNull(menuType.GetField("pendingStat",Hidden).GetValue(menu));
    }
    [TestCase(CharacterStat.Vitality)] [TestCase(CharacterStat.Strength)] [TestCase(CharacterStat.Magic)]
    [TestCase(CharacterStat.PhysicalDefense)] [TestCase(CharacterStat.MagicalDefense)] [TestCase(CharacterStat.Initiative)]
    public void AttributePreviewMatchesActualAllocationWithoutMutatingMember(CharacterStat stat)
    {
        var member=party[0]; member.GrantLevel(2,2); int before=member.Stat(stat);
        var preview=AttributePreview.AfterPoint(member,stat);
        Assert.AreEqual(before,member.Stat(stat)); Assert.AreEqual(2,member.UnspentPoints);
        Assert.IsTrue(member.SpendPoint(stat)); var actual=new GridUnit("After",Vector2Int.zero);member.Configure(actual);
        Assert.AreEqual(actual.MaxHealth,preview.MaxHealth);Assert.AreEqual(actual.MaxMagicPoints,preview.MaxMagicPoints);
        Assert.AreEqual(actual.Strength,preview.Strength);Assert.AreEqual(actual.Magic,preview.Magic);
        Assert.AreEqual(actual.PhysicalDefense,preview.PhysicalDefense);Assert.AreEqual(actual.MagicalDefense,preview.MagicalDefense);
    }
    [Test] public void ProgressionShowsOrderedMilestonesAndPreservesSelectionWithoutGrantingUpgrades()
    {
        var data=party[0].gameObject.AddComponent<CharacterMenuData>();
        var entry=new MenuProgressionEntry{Name="Test bow",Kind=ProgressionEntryKind.Weapon,Level=2,MaximumLevel=5,Experience=25,NextLevelExperience=100};
        entry.Milestones.Add(new UpgradeMilestone{Level=5,Name="Final bonus",Description="Later"});
        entry.Milestones.Add(new UpgradeMilestone{Level=1,Name="Base attack",Description="Already available"});
        entry.Milestones.Add(new UpgradeMilestone{Level=3,Name="Triple Shot",Description="Future attack"});
        data.Progression.Add(entry);
        var other=new MenuProgressionEntry{Name="Second bow",Kind=ProgressionEntryKind.Weapon,Level=1,MaximumLevel=3};data.Progression.Add(other);
        Open();Page("Progression");Find("Weapons").onClick.Invoke();Find("Test bow").onClick.Invoke();
        var text=menu.GetComponentsInChildren<TMP_Text>().Select(t=>t.text).ToList();
        Assert.IsTrue(text.Any(t=>t.Contains("Unlocked · Level 1")),string.Join("\n",text));
        Assert.IsTrue(text.Any(t=>t.Contains("Next · Level 3")));
        Assert.IsTrue(text.Any(t=>t.Contains("Locked · Level 5")));
        var content=menu.GetComponentsInChildren<ScrollRect>().First(r=>r.name=="Progression details").content;
        CollectionAssert.AreEqual(new[]{"Milestone 1","Milestone 3","Milestone 5"},content.Cast<Transform>().Where(t=>t.name.StartsWith("Milestone ")&&t.GetComponent<Image>()).Select(t=>t.name));
        var fill=content.GetComponentsInChildren<RectTransform>().First(r=>r.name=="Mastery fill");
        Assert.AreEqual(.25f,fill.rect.width/((RectTransform)fill.parent).rect.width,.001f);
        Assert.IsFalse(party[0].GetComponent<BattleActionLoadout>().Options.Any(o=>o.Name=="Triple Shot"));
        Find("Second bow").onClick.Invoke();menuType.GetMethod("Rebuild",Hidden).Invoke(menu,null);
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="Progression name"&&t.text=="Second bow"));
        Assert.AreEqual(2,entry.Level);Assert.AreEqual(25,entry.Experience);
    }
    [Test] public void ProgressionIncludesUnrecordedLearnedSpellsAndHidesXPForSingleLevelEntries()
    {
        var data=party[0].gameObject.AddComponent<CharacterMenuData>();
        data.Progression.Add(new MenuProgressionEntry{Name="Recorded spell",Kind=ProgressionEntryKind.Spell});
        party[0].GetComponent<BattleActionLoadout>().PersonalOptions.Add(new BattleActionOption{Name="Other learned spell",Category=BattleActionCategory.Magic,Description="A spell without milestones"});
        Open();Page("Progression");Find("Other learned spell").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="A spell without milestones"));
        var scroll=menu.GetComponentsInChildren<ScrollRect>().First(r=>r.name=="Progression details");
        Assert.IsFalse(scroll.GetComponentsInChildren<Transform>().Any(t=>t.name=="Mastery experience"));
        Find("Weapons").onClick.Invoke();Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="No weapon records available yet."));
    }
    [Test] public void SkillTreeShowsRequirementsAndScrollsToOffscreenNodesWithoutLearning()
    {
        var data=party[0].gameObject.AddComponent<CharacterMenuData>();
        var root=new MenuSkillNode{Id="root",Name="Dash root",ActionName="Dash",Position=new Vector2(-300,-80)};
        var child=new MenuSkillNode{Id="child",Name="Future Skill",RequiredLevel=5,TrainingPointCost=3,Position=new Vector2(1000,720),Prerequisites=new List<string>{"root"}};
        data.SkillTree.Add(root);data.SkillTree.Add(child);
        Open();Page("SkillTree");
        var scroll=menu.GetComponentsInChildren<ScrollRect>().First(r=>r.name=="Skill tree");
        Assert.IsTrue(scroll.horizontal);Assert.Greater(scroll.content.rect.width,scroll.viewport.rect.width);
        var first=Find("Skill node root");var last=Find("Skill node child");
        Assert.AreEqual(last,first.navigation.selectOnRight);Assert.AreEqual(first,last.navigation.selectOnLeft);
        last.onClick.Invoke();Canvas.ForceUpdateCanvases();
        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,last.transform);
        Assert.LessOrEqual(bounds.max.x,scroll.viewport.rect.xMax+.1f);Assert.GreaterOrEqual(bounds.min.y,scroll.viewport.rect.yMin-.1f);
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Dash root · Learned")&&t.text.Contains("Training Point cost: 3")&&t.text.Contains("Locked")));
        menuType.GetMethod("Rebuild",Hidden).Invoke(menu,null);
        var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
        Assert.AreEqual("Skill node child",events.currentSelectedGameObject.name);
        Assert.IsFalse(party[0].GetComponent<BattleActionLoadout>().Options.Any(o=>o.Name=="Future Skill"));
    }
    [Test] public void SkillTreeDoesNotMistakeAttackOrPassiveForLearnedSkillAndHandlesMissingPrerequisites()
    {
        var data=party[0].gameObject.AddComponent<CharacterMenuData>();
        data.SkillTree.Add(new MenuSkillNode{Id="attack",Name="Not a learned Skill",ActionName="Strike",Prerequisites=new List<string>{"missing"}});
        data.SkillTree.Add(new MenuSkillNode{Id="passive",Name="Future passive",ActionName="Dash",Passive=true,Position=new Vector2(150,150)});
        Open();Page("SkillTree");Find("Skill node attack").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Unavailable prerequisite")&&t.text.Contains("Locked")));
        Find("Skill node passive").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Passive · Level and prerequisites met")&&t.text.Contains("Learning is not available yet")));
        var frame=Find("Skill node passive").transform.Find("Node frame").GetComponent(Type.GetType("SkillTreeNodeGraphic, Assembly-CSharp",true));
        Assert.IsTrue((bool)frame.GetType().GetField("Passive").GetValue(frame));
    }
    [Test] public void ProgressionAndTreeRenderAuthoredRecordsWithoutGrantingSkills()
    {
        var data = party[0].gameObject.AddComponent<CharacterMenuData>();
        data.Progression.Add(new MenuProgressionEntry { Name="Test spell family", Kind=ProgressionEntryKind.Spell, MaximumLevel=3,
            Milestones=new List<UpgradeMilestone>{new UpgradeMilestone {Level=2, Name="Test II", Description="Future version"}} });
        data.SkillTree.Add(new MenuSkillNode {Id="one", Name="Test root", Position=new Vector2(80,20)});
        data.SkillTree.Add(new MenuSkillNode {Id="two", Name="Test child", Position=new Vector2(160,120), Prerequisites=new List<string>{"one"}});
        Open(); Page("Progression"); Find("Test spell family").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Future version")));
        Page("SkillTree"); Assert.IsTrue(menu.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="Connection"));
        Assert.IsFalse(party[0].GetComponent<BattleActionLoadout>().Options.Any(o=>o.Name=="Test child"));
    }
}
