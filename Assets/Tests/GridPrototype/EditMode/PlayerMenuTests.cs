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
    void RightClickItem(string name)
    {
        var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
        var data=new UnityEngine.EventSystems.PointerEventData(events){button=UnityEngine.EventSystems.PointerEventData.InputButton.Right};
        UnityEngine.EventSystems.ExecuteEvents.Execute(Find(name).gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
    }
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
    TMP_Dropdown Dropdown(string name)=>menu.GetComponentsInChildren<TMP_Dropdown>().First(d=>d.name==name);
    [Test] public void DebtNegotiatedUnlockRequiresPermanentChoiceConfirmation(){
        var member=party[0];member.Level=12;var data=member.gameObject.AddComponent<CharacterMenuData>();data.DebtTree=true;data.SkillTree=DebtSkillTree.Create();
        Assert.IsTrue(member.LearnNode(data.SkillTree.Single(n=>n.Id=="debt-extension")));
        Open();Page("SkillTree");var node=data.SkillTree.Single(n=>n.Id=="debt-negotiated");
        menuType.GetMethod("ShowSkillNode",Hidden).Invoke(menu,new object[]{node});
        Find("On Credit").onClick.Invoke();Assert.IsFalse(member.KnowsNode(node.Id));Assert.AreEqual(9,member.SkillPoints);
        Find("Cancel").onClick.Invoke();Assert.IsFalse(member.KnowsNode(node.Id));
        Find("Borrowed Momentum").onClick.Invoke();Find("Confirm Borrowed Momentum").onClick.Invoke();
        Assert.IsTrue(member.KnowsNode(node.Id));Assert.AreEqual("Borrowed Momentum",member.NegotiatedSkill);Assert.AreEqual(6,member.SkillPoints);
        menuType.GetMethod("ShowSkillNode",Hidden).Invoke(menu,new object[]{node});
        Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.gameObject.activeInHierarchy&&b.name=="On Credit"));
    }
    [Test] public void SystemCombinesDestinationsAndCancelsPendingSettingsOnTabChange()
    {
        Open();Find("System").onClick.Invoke();
        var shell=(RectTransform)menuType.GetField("shell",Hidden).GetValue(menu);
        Assert.IsNull(shell.Find("Settings"));Assert.IsNull(shell.Find("Saves"));
        Assert.IsFalse(shell.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Selected member").gameObject.activeSelf);
        Assert.AreEqual("System",menuType.GetProperty("CurrentPage").GetValue(menu).ToString());
        bool screen=Screen.fullScreen;
        Find("Graphics").onClick.Invoke();Dropdown("Display mode").value=(Dropdown("Display mode").value+1)%3;
        Find("Save / Load").onClick.Invoke();Assert.AreEqual(screen,Screen.fullScreen);
        Assert.IsNull(menuType.GetField("pendingFullscreen",Hidden).GetValue(menu));
        Assert.IsNotNull(Find("Save Game"));Assert.IsNotNull(Find("Load Last Checkpoint"));
        Find("Graphics").onClick.Invoke();Assert.IsFalse(Find("Apply display change").interactable);
        Find("Audio").onClick.Invoke();Find("Restore defaults").onClick.Invoke();
        Find("Save / Load").onClick.Invoke();Find("Audio").onClick.Invoke();
        Assert.IsFalse((bool)menuType.GetField("resetSettingsPending",Hidden).GetValue(menu));
        Assert.IsNotNull(menu.GetComponentInChildren<Slider>());
        Page("Party");Assert.IsTrue(shell.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Selected member").gameObject.activeSelf);
    }
    [Test] public void SettingsVolumeRetainsFocusAndDefaultsRequireConfirmation()
    {
        WithSettingsSnapshot(()=>{
            Open();Page("Settings");Find("Audio").onClick.Invoke();AudioListener.volume=.5f;
            menu.GetComponentInChildren<Slider>().value=45;Assert.AreEqual(.45f,AudioListener.volume,.001f);Assert.AreEqual(.45f,PlayerPrefs.GetFloat("PlayerMenu.Volume"),.001f);Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Master Volume: 45%"));
            var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
            Assert.IsNotNull(menu.GetComponentInChildren<Slider>());
            Find("Restore defaults").onClick.Invoke();Assert.AreEqual(.45f,AudioListener.volume,.001f);
            Find("Cancel defaults").onClick.Invoke();Assert.AreEqual(.45f,AudioListener.volume,.001f);
            Find("Restore defaults").onClick.Invoke();Find("Confirm defaults").onClick.Invoke();Assert.AreEqual(1,AudioListener.volume);
            menu.GetComponentInChildren<Slider>().value=100;Assert.AreEqual(1,AudioListener.volume);
        });
    }
    [Test] public void SettingsBattleAppliesToWholeRosterAndSavedPreferencesReload()
    {
        WithSettingsSnapshot(()=>{
            PlayerPrefs.SetInt("PlayerMenu.Assisted",0);PlayerPrefs.SetInt("PlayerMenu.AutoCamera",1);PlayerPrefs.SetInt("PlayerMenu.OrderTop",1);
            Open();Page("Settings");Find("Battle").onClick.Invoke();Find("Defense timing").onClick.Invoke();
            foreach(var member in party.Concat(reserves))Assert.IsTrue(member.GetComponent<DefensePrototypeSettings>().Assisted);
            Find("Automatic battle camera").onClick.Invoke();Find("UI").onClick.Invoke();
            CollectionAssert.AreEqual(new[]{"Auto","Xbox","PlayStation","Keyboard"},Dropdown("Controller icons").options.Select(o=>o.text).ToArray());
            Dropdown("Controller icons").value=3;Assert.AreEqual(3,PlayerPrefs.GetInt("PlayerMenu.Icons"));
            Dropdown("Turn order").value=1;
            Assert.AreEqual(0,PlayerPrefs.GetInt("PlayerMenu.AutoCamera"));Assert.AreEqual(0,PlayerPrefs.GetInt("PlayerMenu.OrderTop"));
            foreach(var member in party.Concat(reserves))member.GetComponent<DefensePrototypeSettings>().Assisted=false;
            menuType.GetMethod("ApplySavedMenuSettings",Hidden).Invoke(menu,null);
            foreach(var member in party.Concat(reserves))Assert.IsTrue(member.GetComponent<DefensePrototypeSettings>().Assisted);
            Find("Restore defaults").onClick.Invoke();Find("Confirm defaults").onClick.Invoke();Find("Battle").onClick.Invoke();Find("Restore defaults").onClick.Invoke();Find("Confirm defaults").onClick.Invoke();
            foreach(var member in party.Concat(reserves))Assert.IsFalse(member.GetComponent<DefensePrototypeSettings>().Assisted);
            Assert.AreEqual(1,PlayerPrefs.GetInt("PlayerMenu.AutoCamera"));Assert.AreEqual(1,PlayerPrefs.GetInt("PlayerMenu.OrderTop"));
        });
    }
    [Test] public void DisplaySelectionIsStagedAndBackDoesNotChangeTheScreen()
    {
        WithSettingsSnapshot(()=>{
            bool original=Screen.fullScreen;Open();Page("Settings");Find("Graphics").onClick.Invoke();
            CollectionAssert.AreEqual(new[]{"Windowed","Borderless","Fullscreen"},Dropdown("Display mode").options.Select(o=>o.text).ToArray());
            Dropdown("Display mode").value=(Dropdown("Display mode").value+1)%3;Assert.AreEqual(original,Screen.fullScreen);Assert.IsTrue(Find("Apply display change").interactable);
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
        Assert.AreEqual(1,menu.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Locked sweep")));
        Assert.IsFalse(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains(" · Copy ")));
        equipment.ApplySlot(EquipmentSlot.Weapon,"mastered");Page("Abilities");
        Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name=="Locked sweep · Locked"));
        Find("Locked sweep").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Weapon XP 10 / 10")));
        Assert.AreEqual("mastered",equipment.InstanceAt(EquipmentSlot.Weapon));
    }
    [Test] public void CombinedAbilityWorkspaceSearchesLargeSpellCollectionAndKeepsFourSlots()
    {
        var loadout=party[0].GetComponent<BattleActionLoadout>();
        for(int i=0;i<100;i++)loadout.PersonalOptions.Add(new BattleActionOption{Name="Search spell "+i.ToString("000"),Category=BattleActionCategory.Magic});
        Open();Page("Abilities");Find("Spells").onClick.Invoke();
        Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name=="Shortcuts"&&b.transform.parent.name=="Menu frame"));
        Assert.AreEqual(4,menu.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Shortcut ")));
        var input=menu.GetComponentInChildren<TMP_InputField>();input.text="099";
        Assert.AreEqual(1,menu.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Spell family ")));
        Assert.IsNotNull(Find("Search spell 099 · Unlocked"));
        input.text="not present";Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="No matches"));
        input.text="";Assert.AreEqual(100,menu.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Spell family ")));
        Assert.AreEqual(4,menu.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Shortcut ")));
    }
    [Test] public void CombinedAbilityShortcutsConfirmCancelClearAndKeepSharedItems()
    {
        Open();Page("Abilities");Find("Strike").onClick.Invoke();var loadout=party[0].GetComponent<BattleActionLoadout>();
        Find("Shortcut 4").onClick.Invoke();Assert.IsNull(loadout.Favorite(BattleActionCategory.Attack,3));Find("Cancel").onClick.Invoke();
        Find("Shortcut 4").onClick.Invoke();Find("Confirm shortcut").onClick.Invoke();Assert.AreEqual("Strike",loadout.Favorite(BattleActionCategory.Attack,3).Name);
        Find("Shortcut 4").onClick.Invoke();Find("Clear slot").onClick.Invoke();Assert.IsNull(loadout.Favorite(BattleActionCategory.Attack,3));
        Find("Items").onClick.Invoke();Find("Mana Potion").onClick.Invoke();Find("Shortcut 4").onClick.Invoke();Find("Confirm shortcut").onClick.Invoke();
        Assert.AreEqual("Mana Potion",loadout.Favorite(BattleActionCategory.Item,3).Name);
        Assert.AreEqual(3,((PartyInventory)managerType.GetField("Inventory").GetValue(manager)).Count("mana-potion"));
    }
    [Test] public void CombinedSpellBrowserShowsAllTiersButCannotFavoriteLockedTier()
    {
        var loadout=party[0].GetComponent<BattleActionLoadout>();loadout.LearnedSpells.Add(new LearnedSpell{Id="fireball",Experience=0});
        Open();Page("Abilities");Find("Spells").onClick.Invoke();Find("Fireball III · Locked").onClick.Invoke();
        Find("Shortcut 4").onClick.Invoke();Assert.IsFalse(Find("Confirm shortcut").interactable);Find("Cancel").onClick.Invoke();
        Find("Fireball I · Unlocked").onClick.Invoke();Find("Shortcut 4").onClick.Invoke();Find("Confirm shortcut").onClick.Invoke();
        Assert.AreEqual(1,loadout.Favorite(BattleActionCategory.Magic,3).SpellTier);
        Assert.IsTrue(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Spell milestone"));
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
        Open();Page("Inventory");RightClickItem("Fireball Spellbook");Find("Use").onClick.Invoke();Find("Member 0   Learn spell").onClick.Invoke();
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
    [Test] public void GrowthPrefabPreviewRendersAndMovedMarkersReconnectWithoutChangingScene(){
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var layoutType=Type.GetType("GrowthPageLayout, Assembly-CSharp",true);
        var source=Resources.Load("UI/Growth/Growth",layoutType);Assert.IsNotNull(source);
        var layout=Object.Instantiate(source);var original=(Component)layoutType.GetField("Arrangement").GetValue(layout);
        var arrangement=Object.Instantiate(original);layoutType.GetField("Arrangement").SetValue(layout,arrangement);
        var window=ScriptableObject.CreateInstance(type);Texture2D image=null;GameObject root=null;
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        try{
            type.GetField("growthLayout",Hidden).SetValue(window,layout);
            type.GetMethod("BuildPreview").Invoke(window,null);root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            var before=(RectTransform)root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Skill node preview-left");float x=before.anchoredPosition.x;
            var marker=arrangement.transform.Find("preview-left");marker.localPosition+=new Vector3(40,30,0);
            type.GetMethod("BuildPreview").Invoke(window,null);Assert.IsTrue(root==null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            var moved=(RectTransform)root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Skill node preview-left");Assert.AreEqual(x+40,moved.anchoredPosition.x,.01f);
            Assert.AreEqual(6,root.GetComponentsInChildren<Transform>().Count(t=>t.name=="Connection"));
            var start=(RectTransform)root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Skill node preview-start");
            Assert.Greater(moved.anchoredPosition.y,start.anchoredPosition.y);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);Object.DestroyImmediate(arrangement.gameObject);Object.DestroyImmediate(layout);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase("Shop",0)] [TestCase("Shop",1)] [TestCase("Shop",2)]
    [TestCase("Blacksmith",0)] [TestCase("Blacksmith",1)] [TestCase("Blacksmith",2)]
    public void MerchantPrefabPreviewRendersWithoutChangingScene(string kind,int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Merchants/"+kind,Type.GetType("MerchantPageLayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("merchantLayout",Hidden).SetValue(window,layout);type.GetField("merchantPreview",Hidden).SetValue(window,mode);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
    [TestCase(false,false,true)]
    public void BattlePrefabPreviewRendersWithoutChangingScene(bool right,bool statuses,bool dimmed=false)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Battle/Battle",Type.GetType("BattleUILayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("battleLayout",Hidden).SetValue(window,layout);type.GetField("battleRight",Hidden).SetValue(window,right);type.GetField("battleStatuses",Hidden).SetValue(window,statuses);type.GetField("battleShortcutFocus",Hidden).SetValue(window,dimmed);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] 
    public void OverheadPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Overheads/Overheads",Type.GetType("OverheadUILayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("overheadLayout",Hidden).SetValue(window,layout);type.GetField("overheadPreview",Hidden).SetValue(window,mode);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] 
    public void SwapPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Swap/Swap",Type.GetType("BattleSwapLayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("swapLayout",Hidden).SetValue(window,layout);type.GetField("swapConfirm",Hidden).SetValue(window,mode==1);type.GetField("swapKnockedOut",Hidden).SetValue(window,mode==2);type.GetField("swapCount",Hidden).SetValue(window,mode==3?0:3);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] 
    public void InventoryInteractionPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/InventoryInteractions/Interactions",Type.GetType("InventoryInteractionLayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("interactionLayout",Hidden).SetValue(window,layout);type.GetField("interactionPreview",Hidden).SetValue(window,mode);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] 
    public void TooltipHintsPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Hints/Hints",Type.GetType("TooltipHintsLayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("hintsLayout",Hidden).SetValue(window,layout);type.GetField("hintsPreview",Hidden).SetValue(window,mode);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [Test] public void CheckpointTemplatesCancelRestAndRouteSaveWithoutRecovery(){
        var checkpoint=Make("Checkpoint").AddComponent(Type.GetType("SaveCheckpoint, Assembly-CSharp",true));
        Open();menuType.GetField("checkpointContext",Hidden).SetValue(menu,checkpoint);Page("Checkpoint");
        party[0].MaterializeVitals();int health=party[0].StoredHealth;
        Find("Rest").onClick.Invoke();Assert.IsNotNull(Find("Confirm rest"));
        Find("Cancel rest").onClick.Invoke();Assert.AreEqual(health,party[0].StoredHealth);Assert.IsNotNull(Find("Leave"));
        Find("Save").onClick.Invoke();Assert.IsNotNull(Find("Save slot 1"));Assert.AreEqual(health,party[0].StoredHealth);
        Assert.AreEqual(20,menu.GetComponentsInChildren<Component>().Count(c=>c&&c.GetType().Name=="SystemSaveSlotView"));
        Assert.IsNotNull(Find("Save slot 20"));
        menuType.GetField("savePendingSlot",Hidden).SetValue(menu,0);menuType.GetField("pendingLoad",Hidden).SetValue(menu,false);menuType.GetMethod("Rebuild",Hidden).Invoke(menu,null);
        var confirm=Find("Confirm overwrite");Assert.IsNotNull(confirm.GetComponent(Type.GetType("SystemControlView, Assembly-CSharp",true)));
        Find("Cancel save/load").onClick.Invoke();Assert.IsNotNull(Find("Save slot 20"));Assert.AreEqual(health,party[0].StoredHealth);
        Find("Back").onClick.Invoke();Assert.IsNotNull(Find("Rest"));
        Page("Checkpoint");Find("Leave").onClick.Invoke();Assert.IsFalse((bool)menuType.GetProperty("IsOpen").GetValue(menu));
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void CheckpointPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Checkpoint/Checkpoint",Type.GetType("CheckpointUILayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("checkpointLayout",Hidden).SetValue(window,layout);type.GetField("checkpointPreview",Hidden).SetValue(window,mode);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase("Merchant")] [TestCase("Blacksmith")] [TestCase("SaveCheckpoint")]
    public void WorldPromptBuildUsesTemplateWithoutCapturingInput(string name){
        var type=Type.GetType(name+", Assembly-CSharp",true);var obj=Make("Prompt test");var service=obj.AddComponent(type);
        type.GetMethod("BuildPrompt",Hidden).Invoke(service,null);
        var view=obj.GetComponentInChildren(Type.GetType("WorldPromptView, Assembly-CSharp",true));Assert.IsNotNull(view);
        Assert.IsNotNull(type.GetField("glyph",Hidden).GetValue(service));Assert.IsNotNull(type.GetField("caption",Hidden).GetValue(service));
        foreach(var graphic in obj.GetComponentsInChildren<Graphic>(true))Assert.IsFalse(graphic.raycastTarget);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] 
    public void WorldPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/World/World",Type.GetType("WorldUILayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("worldLayout",Hidden).SetValue(window,layout);type.GetField("worldPreview",Hidden).SetValue(window,mode);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] 
    public void TargetPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Targets/Targets",Type.GetType("TargetUILayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("targetLayout",Hidden).SetValue(window,layout);type.GetField("targetPreview",Hidden).SetValue(window,mode);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
    public void ReactionPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Reactions/Reactions",Type.GetType("ReactionUILayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("reactionLayout",Hidden).SetValue(window,layout);type.GetField("reactionPreview",Hidden).SetValue(window,mode);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
    public void ShellPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Shell/PlayerMenu",Type.GetType("PlayerMenuShellLayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("shellLayout",Hidden).SetValue(window,layout);type.GetField("shellPreview",Hidden).SetValue(window,mode);
            type.GetField("panelWidth",Hidden).SetValue(window,1280);type.GetField("panelHeight",Hidden).SetValue(window,720);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
    public void SystemPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/System/System",Type.GetType("SystemPageLayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("systemLayout",Hidden).SetValue(window,layout);type.GetField("systemPreview",Hidden).SetValue(window,mode);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void AbilitiesPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Abilities/Abilities",Type.GetType("AbilitiesPageLayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("abilitiesLayout",Hidden).SetValue(window,layout);type.GetField("abilitiesPreview",Hidden).SetValue(window,mode);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void InventoryPrefabPreviewRendersWithoutChangingScene(int mode)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Inventory/Inventory",Type.GetType("InventoryPageLayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("inventoryLayout",Hidden).SetValue(window,layout);type.GetField("inventoryPreview",Hidden).SetValue(window,mode);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [Test] public void InventoryLayoutReordersFiltersWithoutChangingTheirBinding()
    {
        var type=Type.GetType("InventoryPageLayout, Assembly-CSharp",true);
        var layout=(ScriptableObject)Object.Instantiate(Resources.Load("UI/Inventory/Inventory",type));equipmentAssets.Add(layout);
        var filters=(System.Collections.IList)type.GetField("Filters").GetValue(layout);
        var value=filters[1];filters.RemoveAt(1);filters.Insert(0,value);
        menuType.GetField("inventoryLayout",Hidden).SetValue(menu,layout);
        Open();Page("Inventory");
        var filterRoot=menu.GetComponentsInChildren<Transform>().Single(t=>t.name=="Filters");
        Assert.AreEqual("Consumables",filterRoot.GetChild(0).name);
        Find("Consumables").onClick.Invoke();Find("Mana Potion").onClick.Invoke();
        Assert.AreEqual("Mana Potion",((TMP_Text)menuType.GetField("detailName",Hidden).GetValue(menu)).text);
        RightClickItem("Mana Potion");Assert.IsNotNull(Find("Use"));
    }
    [TestCase("Formation", "PartyMemberCardView", 6)]
    [TestCase("Equipment", "PartyEquipmentSlotView", 4)]
    [TestCase("Attributes", "PartyAttributeRowView", 7)]
    public void PartyPrefabPreviewRendersBoundWidgetsWithoutChangingScene(string kind,string widget,int count)
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        try{
            var layout=Resources.Load("UI/Party/"+kind,Type.GetType("PartyPageLayout, Assembly-CSharp",true));Assert.IsNotNull(layout);
            type.GetField("partyLayout",Hidden).SetValue(window,layout);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            Assert.AreEqual(count,root.GetComponentsInChildren(Type.GetType(widget+", Assembly-CSharp",true)).Length);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [Test] public void DetailsEditModePreviewRendersAndCleansUpWithoutChangingScene()
    {
        var type=Type.GetType("DetailsPanelPreviewWindow, Assembly-CSharp-Editor",true);
        var window=ScriptableObject.CreateInstance(type);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        Texture2D image=null;GameObject root=null;
        var layout=Object.Instantiate((ScriptableObject)Resources.Load("UI/Details/Abilities"));equipmentAssets.Add(layout);
        try{
            type.GetField("layout",Hidden).SetValue(window,layout);
            type.GetMethod("BuildPreview").Invoke(window,null);
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
            Assert.AreNotEqual(scene,root.scene);
            image=(Texture2D)type.GetMethod("CapturePreview").Invoke(window,null);
            Assert.Greater(image.GetPixels32().Count(c=>c.r>140||c.g>140||c.b>140),100,"Preview must render UI, not just a blank background.");
            var before=(string)type.GetMethod("Signature",Hidden).Invoke(window,null);
            layout.GetType().GetField("Spacing").SetValue(layout,24f);
            Assert.AreNotEqual(before,type.GetMethod("Signature",Hidden).Invoke(window,null));
            type.GetMethod("BuildPreview").Invoke(window,null);Assert.IsTrue(root==null,"Rebuilding should clean up the previous preview.");
            root=(GameObject)type.GetField("canvasRoot",Hidden).GetValue(window);
        }finally{if(image)Object.DestroyImmediate(image);Object.DestroyImmediate(window);}
        Assert.IsTrue(root==null);Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene());Assert.AreEqual(dirty,scene.isDirty);
    }
    [Test] public void ModularAbilityLayoutReordersHidesAndUsesLivePatterns()
    {
        var layout=Object.Instantiate((ScriptableObject)Resources.Load("UI/Details/Abilities"));equipmentAssets.Add(layout);
        var sections=(System.Collections.IList)layout.GetType().GetField("Sections").GetValue(layout);
        var attacks=sections.Cast<object>().Single(v=>v.GetType().GetField("Binding").GetValue(v).ToString()=="Attacks");
        sections.Remove(attacks);sections.Insert(0,attacks);
        var availability=sections.Cast<object>().Single(v=>v.GetType().GetField("Binding").GetValue(v).ToString()=="Availability");
        availability.GetType().GetField("Visible").SetValue(availability,false);
        menuType.GetField("abilitiesDetailsLayout",Hidden).SetValue(menu,layout);
        Open();Page("Abilities");
        var content=menu.GetComponentsInChildren<Transform>().Single(t=>t.name=="Ability details").Find("Content");
        var blocks=content.Cast<Transform>().Where(t=>t.name.StartsWith("Section ")).ToArray();
        Assert.AreEqual("Section Attacks",blocks[0].name);
        Assert.IsFalse(blocks.Any(t=>t.name=="Section Availability"));
        Assert.IsTrue(blocks[0].GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Pattern ")));
        float previousBottom=0;
        foreach(var block in blocks){var r=(RectTransform)block;Assert.GreaterOrEqual(-r.anchoredPosition.y,previousBottom);previousBottom=-r.anchoredPosition.y+r.rect.height;}
        Assert.GreaterOrEqual(((RectTransform)content).rect.height,previousBottom);
    }
    [Test] public void ShopPreviewsMilestonesAndTradesOnlyAfterConfirmation()
    {
        var inventory=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);
        var sword=EquipmentAsset("Merchant sword");
        sword.Mastery.Add(new WeaponMasteryMilestone{RequiredExperience=10,Name="Training",FinisherActionPoints=1});
        inventory.Coins=100;inventory.Catalog.Add(sword);
        var merchantType=Type.GetType("Merchant, Assembly-CSharp",true);
        var merchant=Make("Merchant").AddComponent(merchantType);
        var offerType=merchantType.GetNestedType("Offer");
        var offer=Activator.CreateInstance(offerType);
        offerType.GetField("Equipment").SetValue(offer,sword);
        offerType.GetField("BuyPrice").SetValue(offer,50);
        offerType.GetField("SellPrice").SetValue(offer,10);
        ((System.Collections.IList)merchantType.GetField("Offers").GetValue(merchant)).Add(offer);
        var movementType=Type.GetType("PlayerMovement, Assembly-CSharp",true);
        managerType.GetField("<PossessedCharacter>k__BackingField",Hidden).SetValue(manager,party[0].GetComponent(movementType));
        merchant.transform.position=party[0].transform.position;
        menuType.GetMethod("OpenShop").Invoke(menu,new object[]{merchant});
        var frame=menu.GetComponentsInChildren<Transform>().Single(t=>t.name=="Menu frame");
        Assert.IsFalse(frame.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Inventory").gameObject.activeInHierarchy);
        Assert.IsFalse(frame.Find("Brand").gameObject.activeSelf);
        Assert.AreEqual(24,((RectTransform)frame.Find("Page content")).offsetMin.x);
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text!=null&&t.text.Contains("Training")));
        var expand=menu.GetComponentsInChildren<Button>().First(b=>b.name.Contains("Merchant sword hit"));
        expand.onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Pattern Merchant sword hit"));
        Assert.AreEqual("Buy · 50 coins",Find("Review purchase").GetComponentInChildren<TMP_Text>().text);
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="Currency"&&t.text=="100 coins"));
        foreach(var scroll in menu.GetComponentsInChildren<ScrollRect>()){Assert.IsNotNull(scroll.verticalScrollbar);Assert.IsNotNull(scroll.horizontalScrollbar);}
        Find("Review purchase").onClick.Invoke();Assert.AreEqual(100,inventory.Coins);
        Find("Cancel trade").onClick.Invoke();Assert.AreEqual(0,inventory.Owned.Count);
        Find("Review purchase").onClick.Invoke();Find("Confirm purchase").onClick.Invoke();
        Assert.AreEqual(50,inventory.Coins);Assert.AreEqual(1,inventory.Owned.Count);Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="Currency"&&t.text=="50 coins"));
        Find("Sell").onClick.Invoke();Find("Review sale").onClick.Invoke();Find("Confirm sale").onClick.Invoke();
        Assert.AreEqual(60,inventory.Coins);Assert.AreEqual(0,inventory.Owned.Count);
        Find("Buy").onClick.Invoke();Find("Review purchase").onClick.Invoke();
        merchant.transform.position+=Vector3.right*20;Find("Confirm purchase").onClick.Invoke();
        Assert.AreEqual(60,inventory.Coins);Assert.AreEqual(0,inventory.Owned.Count);
        merchant.transform.position=party[0].transform.position;
        object[] trade={manager,offer,null,false,49,null};
        Assert.IsFalse((bool)merchantType.GetMethod("Trade").Invoke(merchant,trade));Assert.AreEqual(60,inventory.Coins);
        trade[4]=50;Assert.IsTrue((bool)merchantType.GetMethod("Trade").Invoke(merchant,trade));
        var profile=party[0].gameObject.AddComponent<CharacterEquipment>();profile.AllowedWeaponTypes=new[]{WeaponType.Shortsword};
        profile.RestoreSlots(new[]{inventory.Owned[0].InstanceId,null,null,null});managerType.GetMethod("BindEquipment").Invoke(manager,null);
        trade=new object[]{manager,offer,inventory.Owned[0],true,10,null};
        Assert.IsFalse((bool)merchantType.GetMethod("Trade").Invoke(merchant,trade));Assert.AreEqual(1,inventory.Owned.Count);
        var potionOffer=Activator.CreateInstance(offerType);offerType.GetField("Item").SetValue(potionOffer,PartyInventory.ManaPotion(1));
        offerType.GetField("BuyPrice").SetValue(potionOffer,5);offerType.GetField("SellPrice").SetValue(potionOffer,2);
        ((System.Collections.IList)merchantType.GetField("Offers").GetValue(merchant)).Add(potionOffer);
        var shared=(PartyInventory)managerType.GetField("Inventory").GetValue(manager);int count=shared.Count("mana-potion");
        trade=new object[]{manager,potionOffer,null,false,5,null};Assert.IsTrue((bool)merchantType.GetMethod("Trade").Invoke(merchant,trade));
        Assert.AreEqual(count+1,shared.Count("mana-potion"));Assert.AreEqual(5,inventory.Coins);
        trade=new object[]{manager,potionOffer,null,true,2,null};Assert.IsTrue((bool)merchantType.GetMethod("Trade").Invoke(merchant,trade));
        Assert.AreEqual(count,shared.Count("mana-potion"));Assert.AreEqual(7,inventory.Coins);
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
        var frame=menu.GetComponentsInChildren<Transform>().Single(t=>t.name=="Menu frame");
        Assert.IsFalse(frame.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Inventory").gameObject.activeInHierarchy);
        Assert.IsFalse(frame.Find("Brand").gameObject.activeSelf);
        Assert.AreEqual(24,((RectTransform)frame.Find("Page content")).offsetMin.x);
        smith.transform.position+=Vector3.right*10;Assert.IsFalse((bool)managerType.GetMethod("TryUpgradeEquipment").Invoke(manager,outside));smith.transform.position=party[0].transform.position;
        Find("Armor").onClick.Invoke();
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="No equipment in this category."));
        Assert.IsNotNull(Find("Leave"));
        Find("Weapons").onClick.Invoke();
        var slots=menu.GetComponentsInChildren<Button>().Where(b=>b.name=="Upgrade sword").ToArray();
        Assert.AreEqual(2,slots.Length);
        foreach(var slot in slots){var rect=(RectTransform)slot.transform;Assert.AreEqual(rect.rect.width,rect.rect.height,.01f);}
        slots[1].onClick.Invoke();
        Assert.AreEqual("two",menuType.GetField("upgradeSelection",Hidden).GetValue(menu));
        menu.GetComponentsInChildren<Button>().First(b=>b.name=="Upgrade sword").onClick.Invoke();
        Find("Review upgrade").onClick.Invoke();
        Assert.IsFalse(Find("Weapons").interactable);
        Assert.IsTrue(menu.GetComponentsInChildren<Button>().Where(b=>b.name=="Upgrade sword").All(b=>!b.interactable));
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
            Open();Page("Saves");Assert.IsFalse(Find("Load Last Checkpoint").interactable);Find("Save Game").onClick.Invoke();Assert.IsNotNull(Find("Save slot 20"));Find("Save slot 1").onClick.Invoke();
            Assert.IsNotNull(Find("Confirm overwrite"));Find("Cancel save/load").onClick.Invoke();Assert.AreEqual("Test captain",store.Read(0,out _).CaptainName);
            menuType.GetMethod("Back").Invoke(menu,null);Find("Load Game").onClick.Invoke();Assert.AreEqual(1,menu.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Load slot ")));Find("Load slot 1").onClick.Invoke();Assert.IsNotNull(Find("Confirm load"));menuType.GetMethod("Back").Invoke(menu,null);
            var active=service.GetType().GetField("<ActiveSlot>k__BackingField",Hidden);
            var checkpointA=new GameSaveData{Scene=saved.Scene,CaptainName="Checkpoint A",Inventory=new PartyInventory()};checkpointA.Characters.Add(new SavedCharacter{Id="test"});
            saved.CheckpointPayload=SaveFileStore.Encode(checkpointA);store.Write(0,SaveFileStore.Encode(saved),true);
            saved.CheckpointPayload=null;store.Write(1,SaveFileStore.Encode(saved),false);
            store.Write(SaveFileStore.CheckpointSlot,SaveFileStore.Encode(checkpointA),false);
            active.SetValue(service,0);
            object[] arguments={false};var read=service.GetType().GetMethod("ReadCheckpoint");
            Assert.AreEqual("Checkpoint A",((GameSaveData)read.Invoke(service,arguments)).CaptainName);
            active.SetValue(service,1);Assert.IsFalse((bool)service.GetType().GetProperty("HasCheckpoint").GetValue(service),"Never fall back to another slot or the old global checkpoint.");
            Assert.Throws<TargetInvocationException>(()=>read.Invoke(service,new object[]{false}));
            active.SetValue(service,-1);Assert.IsTrue((bool)service.GetType().GetProperty("HasCheckpoint").GetValue(service));
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
        Assert.IsNotNull(Find("Save slot 20"));Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Load slot")));Assert.AreEqual(0,party[0].StoredHealth);
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
    [Test] public void InventoryContextUsesClickPositionAndStaysInsideCanvasAtEdges()
    {
        Open();Page("Inventory");Canvas.ForceUpdateCanvases();
        var canvas=(RectTransform)((GameObject)menuType.GetField("canvasObject",Hidden).GetValue(menu)).transform;
        var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
        foreach(var fraction in new[]{new Vector2(.45f,.35f),new Vector2(.99f,.99f),new Vector2(.01f,.01f)}){
            var local=new Vector2(canvas.rect.xMin+canvas.rect.width*fraction.x,canvas.rect.yMin+canvas.rect.height*fraction.y);
            var screen=RectTransformUtility.WorldToScreenPoint(null,canvas.TransformPoint(local));
            var data=new UnityEngine.EventSystems.PointerEventData(events){button=UnityEngine.EventSystems.PointerEventData.InputButton.Right,position=screen};
            UnityEngine.EventSystems.ExecuteEvents.Execute(Find("Health Potion").gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            var modal=(RectTransform)menuType.GetField("inventoryModal",Hidden).GetValue(menu);var panel=(RectTransform)modal.Find("Item context");
            Assert.AreEqual(0,modal.GetComponent<Image>().color.a,"Context menus should not dim the whole page.");
            float x=panel.anchoredPosition.x,y=-panel.anchoredPosition.y;
            Assert.GreaterOrEqual(x,8);Assert.GreaterOrEqual(y,8);
            Assert.LessOrEqual(x+panel.rect.width,canvas.rect.width-8+.1f);Assert.LessOrEqual(y+panel.rect.height,canvas.rect.height-8+.1f);
            if(fraction.x==.45f){Assert.AreEqual(canvas.rect.width*fraction.x+12,x,.1f);Assert.AreEqual(canvas.rect.height*(1-fraction.y)-12,y+panel.rect.height,.1f);}
            Find("Back").onClick.Invoke();
        }
    }
    [Test] public void InventoryCommandRailDetailsAndNavigationContextPreserveSelection()
    {
        Open();Page("Inventory");Find("Mana Potion").onClick.Invoke();
        Assert.IsNotNull(menu.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Selected item image"));
        Assert.AreEqual("Mana Potion",((TMP_Text)menuType.GetField("detailName",Hidden).GetValue(menu)).text);
        RightClickItem("Mana Potion");Assert.AreEqual("Mana Potion",((BattleActionOption)menuType.GetField("inventoryItem",Hidden).GetValue(menu)).Name);
        Find("Examine").onClick.Invoke();
        var modal=(RectTransform)menuType.GetField("inventoryModal",Hidden).GetValue(menu);
        Assert.AreEqual(new Vector2(.5f,.5f),((RectTransform)modal.Find("Item context")).pivot);
        Find("Back").onClick.Invoke();Find("Back").onClick.Invoke();
        Assert.AreEqual("Mana Potion",((TMP_Text)menuType.GetField("detailName",Hidden).GetValue(menu)).text);
        Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name=="Item actions"));
    }
    [Test] public void InventoryLeftClickSelectsAndHoverDoesNotReplacePinnedDetails()
    {
        Open();Page("Inventory");Find("Mana Potion").onClick.Invoke();
        Assert.IsFalse(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Inventory interaction"));
        Assert.AreEqual("Mana Potion",((TMP_Text)menuType.GetField("detailName",Hidden).GetValue(menu)).text);
        var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
        var data=new UnityEngine.EventSystems.PointerEventData(events);
        UnityEngine.EventSystems.ExecuteEvents.Execute(Find("Health Potion").gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
        Assert.AreEqual("Mana Potion",((TMP_Text)menuType.GetField("detailName",Hidden).GetValue(menu)).text);
        RightClickItem("Health Potion");Assert.IsNotNull(Find("Use"));
    }
    [Test] public void InventoryGridFiltersEquipmentMaterialsAndBooksWithPicturesAndSafeContext()
    {
        var gear=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);
        var sword=EquipmentAsset("Grid sword");var armor=EquipmentAsset("Grid armor",EquipmentKind.Armor);var charm=EquipmentAsset("Grid charm",EquipmentKind.Accessory);
        gear.Owned.AddRange(new[]{new OwnedEquipment{InstanceId="sword-one",Definition=sword},new OwnedEquipment{InstanceId="sword-two",Definition=sword},new OwnedEquipment{InstanceId="armor",Definition=armor},new OwnedEquipment{InstanceId="charm",Definition=charm}});
        gear.Materials.Add(new MaterialStack{Id="iron",Name="Iron",Quantity=7});
        var inventory=(PartyInventory)managerType.GetField("Inventory").GetValue(manager);
        inventory.Items.Add(new BattleActionOption{Name="Fireball Book",ItemId="book",Category=BattleActionCategory.Item,LearnSpellId="fireball",Quantity=2});
        Open();Page("Inventory");
        var potion=Find("Health Potion");Assert.IsFalse(potion.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Health Potion"));
        Assert.AreEqual(((RectTransform)potion.transform).rect.width,((RectTransform)potion.transform).rect.height,.01f);
        Assert.IsNotNull(potion.transform.Find("Icon/Item artwork").GetComponent<Graphic>());
        Assert.IsTrue(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Empty inventory slot"));
        Find("Weapons").onClick.Invoke();Assert.AreEqual(2,menu.GetComponentsInChildren<Button>().Count(b=>b.name=="Grid sword"));
        Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name=="Health Potion"));
        RightClickItem("Grid sword");Assert.IsNotNull(Find("Examine"));Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name=="Use"));Find("Back").onClick.Invoke();
        Find("Armor").onClick.Invoke();Assert.IsNotNull(Find("Grid armor"));
        Find("Accessories").onClick.Invoke();Assert.IsNotNull(Find("Grid charm"));
        Find("Materials").onClick.Invoke();Find("Iron").onClick.Invoke();Assert.AreEqual("Iron",((TMP_Text)menuType.GetField("detailName",Hidden).GetValue(menu)).text);
        Assert.AreEqual("7",Find("Iron").transform.Find("Quantity or upgrade").GetComponent<TMP_Text>().text);
        Find("Spell Books").onClick.Invoke();RightClickItem("Fireball Book");Assert.IsNotNull(Find("Use"));Find("Back").onClick.Invoke();
        Find("Other").onClick.Invoke();Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="No items in this category."));
    }
    [Test] public void InventoryCrossCategorySwapOnlyChangesSavedSlotOrder()
    {
        var gear=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);var sword=EquipmentAsset("Drag sword");
        var copy=new OwnedEquipment{InstanceId="drag-sword",Definition=sword,MasteryExperience=4};gear.Owned.Add(copy);
        var inventory=(PartyInventory)managerType.GetField("Inventory").GetValue(manager);int count=inventory.Items[0].Quantity;
        Open();Page("Inventory");
        var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
        var source=Find("Health Potion").gameObject;var target=Find("Drag sword").gameObject;
        var data=new UnityEngine.EventSystems.PointerEventData(events){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,pointerDrag=source};
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(target,data,UnityEngine.EventSystems.ExecuteEvents.dropHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.endDragHandler);
        Assert.AreEqual("equipment:drag-sword",inventory.SlotOrder[0]);Assert.AreSame(copy,gear.Owned[0]);Assert.AreEqual(4,copy.MasteryExperience);Assert.AreEqual(count,inventory.Items[0].Quantity);
        source=Find("Drag sword").gameObject;target=menu.GetComponentsInChildren<Transform>().First(t=>t.name=="Empty inventory slot").gameObject;data.pointerDrag=source;
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(target,data,UnityEngine.EventSystems.ExecuteEvents.dropHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.endDragHandler);
        Assert.IsTrue(inventory.SlotOrder[0].StartsWith("empty:"));Assert.AreSame(copy,gear.Owned[0]);
        var saved=JsonUtility.FromJson<PartyInventory>(JsonUtility.ToJson(inventory));CollectionAssert.AreEqual(inventory.SlotOrder,saved.SlotOrder);
    }
    [Test] public void InventoryEquipPreviewsCancelsConfirmsAndUnequipsWithoutHealing()
    {
        var gear=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);var sword=EquipmentAsset("Context sword");
        sword.Upgrades.Add(new EquipmentUpgradeStep{AddedStats=new StatValues{Strength=2}});
        gear.Owned.Add(new OwnedEquipment{InstanceId="context-sword",Definition=sword,UpgradeLevel=1});
        var profile=party[0].gameObject.AddComponent<CharacterEquipment>();profile.AllowedWeaponTypes=new[]{WeaponType.Shortsword};
        var incompatible=party[1].gameObject.AddComponent<CharacterEquipment>();incompatible.AllowedWeaponTypes=new[]{WeaponType.Bow};
        managerType.GetMethod("BindEquipment").Invoke(manager,null);party[0].MaterializeVitals();int hp=party[0].StoredHealth,mp=party[0].StoredMagicPoints,before=party[0].Stat(CharacterStat.Strength);
        Open();Page("Inventory");RightClickItem("Context sword");Find("Equip").onClick.Invoke();
        Assert.IsFalse(Find("Member 1 · incompatible").interactable);Find("Member 0").onClick.Invoke();
        Assert.IsNull(profile.At(EquipmentSlot.Weapon));Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Strength")&&t.text.Contains((before+5).ToString())));
        Find("Back").onClick.Invoke();Assert.IsNull(profile.At(EquipmentSlot.Weapon));Find("Member 0").onClick.Invoke();
        var confirm=Find("Confirm equip");confirm.onClick.Invoke();confirm.onClick.Invoke();
        Assert.AreEqual("context-sword",profile.InstanceAt(EquipmentSlot.Weapon));Assert.AreEqual(before+5,party[0].Stat(CharacterStat.Strength));Assert.AreEqual(hp,party[0].StoredHealth);Assert.AreEqual(mp,party[0].StoredMagicPoints);
        RightClickItem("Context sword");Assert.IsFalse(Find("Equip").interactable);Find("Unequip").onClick.Invoke();Find("Confirm unequip").onClick.Invoke();Assert.IsNull(profile.At(EquipmentSlot.Weapon));Assert.AreEqual(1,gear.Owned.Count);
        RightClickItem("Context sword");Find("Upgrade details").onClick.Invoke();Assert.AreEqual("Upgrades",menuType.GetProperty("CurrentPage").GetValue(menu).ToString());Assert.IsFalse(Find("Review upgrade").interactable);Find("Back").onClick.Invoke();Assert.AreEqual("Inventory",menuType.GetProperty("CurrentPage").GetValue(menu).ToString());
    }
    [Test] public void InventoryAccessoryChoosesSecondSlotAndSidebarUsesConsolidatedPages()
    {
        var gear=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);var charm=EquipmentAsset("Context charm",EquipmentKind.Accessory);
        gear.Owned.Add(new OwnedEquipment{InstanceId="charm",Definition=charm});
        var profile=reserves[0].gameObject.AddComponent<CharacterEquipment>();managerType.GetMethod("BindEquipment").Invoke(manager,null);
        Open();Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.transform.parent.name=="Menu frame"&&(b.name=="Equipment"||b.name=="Attributes"||b.name=="Progression"||b.name=="Upgrades")));
        Page("Inventory");RightClickItem("Context charm");Find("Equip").onClick.Invoke();Find("Member 3").onClick.Invoke();Find("Accessory 2:").onClick.Invoke();Find("Confirm equip").onClick.Invoke();
        Assert.AreEqual("charm",profile.InstanceAt(EquipmentSlot.AccessoryTwo));Assert.IsNull(profile.At(EquipmentSlot.AccessoryOne));
        Page("Abilities");Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name=="Mastery & milestones"));Assert.AreEqual("Abilities",menuType.GetProperty("CurrentPage").GetValue(menu).ToString());
    }
    [Test] public void InventoryDragSwapsWholeStacksAndInvalidDropsOrPageChangesCancel()
    {
        var inventory=(PartyInventory)managerType.GetField("Inventory").GetValue(manager);
        var health=inventory.Items[0];var mana=inventory.Items[1];int hpCount=health.Quantity,mpCount=mana.Quantity;
        Open();Page("Inventory");
        var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
        var source=Find("Health Potion").gameObject;var target=Find("Mana Potion").gameObject;
        var data=new UnityEngine.EventSystems.PointerEventData(events){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,pointerDrag=source,eligibleForClick=true,pointerCurrentRaycast=new UnityEngine.EventSystems.RaycastResult{gameObject=target}};
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
        Assert.IsFalse(data.eligibleForClick);
        Assert.IsTrue(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Dragged inventory item"));
        Assert.IsTrue(target.GetComponent<Outline>().enabled);
        UnityEngine.EventSystems.ExecuteEvents.Execute(target,data,UnityEngine.EventSystems.ExecuteEvents.dropHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.endDragHandler);
        Assert.AreSame(mana,inventory.Items[0]);Assert.AreSame(health,inventory.Items[1]);Assert.AreEqual(hpCount,health.Quantity);Assert.AreEqual(mpCount,mana.Quantity);
        Assert.AreEqual("Health Potion",((TMP_Text)menuType.GetField("detailName",Hidden).GetValue(menu)).text);
        Assert.IsFalse(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Inventory interaction"||t.name=="Dragged inventory item"));
        source=Find("Health Potion").gameObject;data.pointerDrag=source;data.pointerCurrentRaycast=new UnityEngine.EventSystems.RaycastResult();
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.endDragHandler);
        Assert.AreSame(mana,inventory.Items[0]);
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);Page("Party");
        Assert.IsFalse(menu.GetComponentsInChildren<Transform>().Any(t=>t.name=="Dragged inventory item"));Assert.AreSame(mana,inventory.Items[0]);
        var saved=JsonUtility.FromJson<PartyInventory>(JsonUtility.ToJson(inventory));Assert.AreEqual(mana.ItemId,saved.Items[0].ItemId);Assert.AreEqual(hpCount,saved.Items[1].Quantity);
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
    [Test] public void PartyFormationSwapRequiresConfirmationAndSupportsContextSelection()
    {
        Open(); var outgoing=party[1];var incoming=reserves[0];
        Find("Member 3").onClick.Invoke(); Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name=="Character actions")); RightClickItem("Member 3");
        Find("Swap with").onClick.Invoke();
        var modal=(RectTransform)menuType.GetField("partyModal",Hidden).GetValue(menu);
        modal.GetComponentsInChildren<Button>().First(b=>b.name=="Member 1").onClick.Invoke();
        Assert.AreSame(outgoing,party[1]);Find("Cancel").onClick.Invoke();Assert.AreSame(outgoing,party[1]);
        menuType.GetMethod("StagePartySwap",Hidden).Invoke(menu,new object[]{incoming,outgoing});
        Find("Confirm swap").onClick.Invoke(); Assert.AreSame(incoming,party[1]);Assert.Contains(outgoing,reserves);
        Assert.AreEqual(3,party.Count);Assert.AreEqual(3,reserves.Count);
        var before=party.ToArray();menuType.GetMethod("StagePartySwap",Hidden).Invoke(menu,new object[]{party[0],party[1]});
        CollectionAssert.AreEqual(before,party);Assert.IsNull(menuType.GetField("partyModal",Hidden).GetValue(menu));
    }
    [Test] public void PartyPortraitDragOnlyStagesSwapAndOutsideDropCancels()
    {
        Open();var source=Find("Member 3").gameObject;var target=Find("Member 1").gameObject;
        var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
        var data=new UnityEngine.EventSystems.PointerEventData(events){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,pointerDrag=source,position=Vector2.one};
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
        Assert.IsNotNull(menuType.GetField("partyDragGhost",Hidden).GetValue(menu));
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.endDragHandler);
        Assert.IsNull(menuType.GetField("partyModal",Hidden).GetValue(menu));Assert.AreEqual("Member 1",party[1].name);
        UnityEngine.EventSystems.ExecuteEvents.Execute(source,data,UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(target,data,UnityEngine.EventSystems.ExecuteEvents.dropHandler);
        Assert.IsNotNull(menuType.GetField("partyModal",Hidden).GetValue(menu));Assert.AreEqual("Member 1",party[1].name);
        Find("Confirm swap").onClick.Invoke();Assert.AreEqual("Member 3",party[1].name);
    }
    [Test] public void PartyAllocationStagesSeveralStatsAndCommitsOnceWithoutHealing()
    {
        var member=party[0];member.GrantLevel(2,3);member.MaterializeVitals();int hp=member.StoredHealth,mp=member.StoredMagicPoints;
        int magic=member.Stat(CharacterStat.Magic),soul=member.Stat(CharacterStat.Soul);
        Open();Find("Attributes").onClick.Invoke();Find("Increase Magic").onClick.Invoke();Find("Increase Magic").onClick.Invoke();Find("Increase Soul").onClick.Invoke();
        Assert.AreEqual(3,member.UnspentPoints);Assert.AreEqual(magic,member.Stat(CharacterStat.Magic));Assert.IsFalse(Find("Increase Magic").interactable);
        Find("Decrease Magic").onClick.Invoke();Find("Apply allocation").onClick.Invoke();Find("Cancel").onClick.Invoke();Assert.AreEqual(3,member.UnspentPoints);
        Find("Apply allocation").onClick.Invoke();var confirm=Find("Confirm allocation");confirm.onClick.Invoke();
        Assert.AreEqual(1,member.UnspentPoints);Assert.AreEqual(magic+1,member.Stat(CharacterStat.Magic));Assert.AreEqual(soul+1,member.Stat(CharacterStat.Soul));
        Assert.AreEqual(hp,member.StoredHealth);Assert.AreEqual(mp,member.StoredMagicPoints);
        confirm.onClick.Invoke();Assert.AreEqual(1,member.UnspentPoints);
    }
    [Test] public void PartyAllocationProtectsPendingEditsOnNavigationAndRejectsStaleConfirmation()
    {
        var member=party[0];member.GrantLevel(2,3);Open();Find("Attributes").onClick.Invoke();Find("Increase Strength").onClick.Invoke();
        menuType.GetMethod("Cycle",Hidden).Invoke(menu,new object[]{1});Find("Keep editing").onClick.Invoke();
        Assert.AreEqual(0,menuType.GetField("memberIndex",Hidden).GetValue(menu));
        Find("Apply allocation").onClick.Invoke();int before=member.Stat(CharacterStat.Strength);member.Strength=20;
        Find("Confirm allocation").onClick.Invoke();Assert.AreEqual(3,member.UnspentPoints);Assert.AreEqual(0,member.AllocatedPoints.Strength);
        Find("Increase Magic").onClick.Invoke();menuType.GetMethod("Cycle",Hidden).Invoke(menu,new object[]{1});Find("Discard changes").onClick.Invoke();
        Assert.AreEqual(1,menuType.GetField("memberIndex",Hidden).GetValue(menu));Assert.AreEqual(3,member.UnspentPoints);
    }
    [Test] public void PartyEquipmentUsesSharedComparisonAndReturnsToSlotPickerOnBack()
    {
        var gear=(PartyEquipment)managerType.GetField("Equipment").GetValue(manager);var sword=EquipmentAsset("Party sword");var bow=EquipmentAsset("Party bow",EquipmentKind.Weapon,WeaponType.Bow);
        gear.Owned.Add(new OwnedEquipment{InstanceId="party-sword",Definition=sword});gear.Owned.Add(new OwnedEquipment{InstanceId="party-bow",Definition=bow});
        var profile=party[0].gameObject.AddComponent<CharacterEquipment>();profile.AllowedWeaponTypes=new[]{WeaponType.Shortsword};
        Open();Find("Equipment").onClick.Invoke();
        var slot=(RectTransform)Find("Weapon").transform;Assert.AreEqual(slot.rect.width,slot.rect.height);
        Assert.IsTrue(menu.GetComponentsInChildren<RawImage>().Any(i=>i.name=="Equipped character"));
        Find("Weapon").onClick.Invoke();
        menuType.GetMethod("UpdateMenuInteractionState",Hidden).Invoke(menu,null);
        Assert.IsTrue(Find("Cancel").IsInteractable());Assert.IsTrue(Find("Party sword").IsInteractable());
        Find("Cancel").onClick.Invoke();Assert.IsNull(menuType.GetField("partyModal",Hidden).GetValue(menu));Find("Weapon").onClick.Invoke();
        Assert.IsFalse(menu.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Party bow")));
        Find("Party sword").onClick.Invoke();Assert.IsNull(profile.At(EquipmentSlot.Weapon));
        Find("Back").onClick.Invoke();Assert.IsNotNull(menuType.GetField("partyModal",Hidden).GetValue(menu));
        Find("Party sword").onClick.Invoke();Find("Confirm equip").onClick.Invoke();Assert.AreEqual("party-sword",profile.InstanceAt(EquipmentSlot.Weapon));
        Assert.AreEqual("Party",menuType.GetProperty("CurrentPage").GetValue(menu).ToString());
        Find("Weapon").onClick.Invoke();menuType.GetMethod("UpdateMenuInteractionState",Hidden).Invoke(menu,null);Assert.IsTrue(Find("Unequip").IsInteractable());Find("Unequip").onClick.Invoke();Find("Confirm unequip").onClick.Invoke();Assert.IsNull(profile.At(EquipmentSlot.Weapon));
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
    [TestCase(CharacterStat.PhysicalDefense)] [TestCase(CharacterStat.MagicalDefense)] [TestCase(CharacterStat.Speed)] [TestCase(CharacterStat.Soul)]
    public void AttributePreviewMatchesActualAllocationWithoutMutatingMember(CharacterStat stat)
    {
        var member=party[0]; member.GrantLevel(2,2); int before=member.Stat(stat);
        var preview=AttributePreview.AfterPoint(member,stat);
        Assert.AreEqual(before,member.Stat(stat)); Assert.AreEqual(2,member.UnspentPoints);
        Assert.IsTrue(member.SpendPoint(stat)); var actual=new GridUnit("After",Vector2Int.zero);member.Configure(actual);
        Assert.AreEqual(actual.MaxHealth,preview.MaxHealth);Assert.AreEqual(actual.MaxMagicPoints,preview.MaxMagicPoints);
        Assert.AreEqual(actual.Soul,preview.Soul);Assert.AreEqual(actual.Strength,preview.Strength);Assert.AreEqual(actual.Magic,preview.Magic);
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
    [Test] public void GrowthPrototypeStartsAtBottomAndNavigatesUpWithoutCreatingACharacterKit()
    {
        var loadout=party[0].GetComponent<BattleActionLoadout>();var before=loadout.Options.Select(a=>a.Name).ToArray();int points=party[0].UnspentPoints;
        Open();Find("Growth").onClick.Invoke();Canvas.ForceUpdateCanvases();
        var root=Find("Skill node preview-start");var left=Find("Skill node preview-left");var top=Find("Skill node preview-left-top");
        Assert.Greater(((RectTransform)left.transform).anchoredPosition.y,((RectTransform)root.transform).anchoredPosition.y);
        Assert.Greater(((RectTransform)top.transform).anchoredPosition.y,((RectTransform)left.transform).anchoredPosition.y);
        Assert.AreEqual(left,root.navigation.selectOnUp);Assert.AreEqual(root,left.navigation.selectOnDown);
        var events=(UnityEngine.EventSystems.EventSystem)menuType.GetField("events",Hidden).GetValue(menu);
        Assert.AreEqual(root.gameObject,events.currentSelectedGameObject);
        left.onClick.Invoke();Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Layout prototype")&&t.text.Contains("Skill Point cost: Not set")));
        menuType.GetMethod("Rebuild",Hidden).Invoke(menu,null);Assert.AreEqual("Skill node preview-left",events.currentSelectedGameObject.name);
        Assert.IsNull(party[0].GetComponent<CharacterMenuData>());CollectionAssert.AreEqual(before,loadout.Options.Select(a=>a.Name));Assert.AreEqual(points,party[0].UnspentPoints);
        menuType.GetMethod("Cycle",Hidden).Invoke(menu,new object[]{1});Assert.AreEqual("Skill node preview-start",events.currentSelectedGameObject.name);
    }
    [Test] public void SkillTreeShowsRequirementsAndScrollsToOffscreenNodesWithoutLearning()
    {
        var data=party[0].gameObject.AddComponent<CharacterMenuData>();
        var root=new MenuSkillNode{Id="root",Name="Dash root",ActionName="Dash",Position=new Vector2(-300,-80)};
        var child=new MenuSkillNode{Id="child",Name="Future Skill",RequiredLevel=5,SkillPointCost=3,Position=new Vector2(1000,720),Prerequisites=new List<string>{"root"}};
        data.SkillTree.Add(root);data.SkillTree.Add(child);
        Open();Page("SkillTree");
        var scroll=menu.GetComponentsInChildren<ScrollRect>().First(r=>r.name=="Skill tree");
        Assert.IsTrue(scroll.horizontal);Assert.Greater(scroll.content.rect.width,scroll.viewport.rect.width);
        var first=Find("Skill node root");var last=Find("Skill node child");
        Assert.AreEqual(last,first.navigation.selectOnRight);Assert.AreEqual(first,last.navigation.selectOnLeft);
        Assert.AreEqual(last,first.navigation.selectOnUp);Assert.AreEqual(first,last.navigation.selectOnDown);
        Assert.Greater(((RectTransform)last.transform).anchoredPosition.y,((RectTransform)first.transform).anchoredPosition.y);
        last.onClick.Invoke();Canvas.ForceUpdateCanvases();
        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,last.transform);
        Assert.LessOrEqual(bounds.max.x,scroll.viewport.rect.xMax+.1f);Assert.GreaterOrEqual(bounds.min.y,scroll.viewport.rect.yMin-.1f);
        Assert.IsTrue(menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Dash root · Learned")&&t.text.Contains("Skill Point cost: 3")&&t.text.Contains("Locked")));
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
