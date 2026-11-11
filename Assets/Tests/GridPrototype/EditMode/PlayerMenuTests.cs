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
        owned.Clear(); Time.timeScale = previousTime;
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
        Assert.IsTrue(text.Any(t=>t.Contains("Unlocked · Level 1")));
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
