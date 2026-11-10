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
        Open(); Page("Shortcuts"); Find("Strike").onClick.Invoke(); Find("4 / ").onClick.Invoke();
        var loadout = party[0].GetComponent<BattleActionLoadout>();
        Assert.AreEqual("Strike", loadout.Favorite(BattleActionCategory.Attack, 3).Name);
        var inventory = (PartyInventory)managerType.GetField("Inventory").GetValue(manager);
        Assert.AreEqual(3, inventory.Count("mana-potion")); Assert.AreEqual(3, inventory.Count("health-potion"));
    }
    [Test] public void CaptainSelectionReordersOnlyActiveParty()
    {
        var selected = party[2]; var position = selected.transform.position;
        Assert.IsTrue((bool)managerType.GetMethod("TrySetCaptain").Invoke(manager, new object[] { selected }));
        Assert.AreSame(selected, party[0]); Assert.AreEqual(position, selected.transform.position);
        Assert.AreEqual(3, party.Count); Assert.AreEqual(3, reserves.Count);
        Assert.IsFalse((bool)managerType.GetMethod("TrySetCaptain").Invoke(manager, new object[] { reserves[0] }));
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
