using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Game.GridPrototype;
public class BattleFavoritesTests : InputTestFixture
{
    [Test] public void StableSlotsSurviveReorderingDepletionAndReassignment()
    {
        var obj = new GameObject("Favorites test");
        try
        {
            var loadout = obj.AddComponent<BattleActionLoadout>(); loadout.InitializeFavorites();
            var attack = loadout.Favorite(BattleActionCategory.Attack, 0);
            var item = loadout.Favorite(BattleActionCategory.Item, 0); item.Quantity = 0;
            loadout.Options.Reverse();
            Assert.AreSame(item, loadout.Favorite(BattleActionCategory.Item, 0));
            Assert.AreSame(attack, loadout.Favorite(BattleActionCategory.Attack, 0)); Assert.IsFalse(item.Available);
            loadout.AssignFavorite(attack, 3); Assert.IsNull(loadout.Favorite(BattleActionCategory.Attack, 0));
            Assert.AreSame(attack, loadout.Favorite(BattleActionCategory.Attack, 3));
            loadout.AssignFavorite(attack, -1); Assert.IsNull(loadout.Favorite(BattleActionCategory.Attack, 3));
            Assert.Contains(attack, loadout.Options);
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
    }
    [Test] public void MoreThanFourAbilitiesRemainAvailableWhenFavoritesAreReplaced()
    {
        var obj = new GameObject("Favorites test");
        try
        {
            var loadout = obj.AddComponent<BattleActionLoadout>(); loadout.Options.Clear();
            for (int i = 0; i < 6; i++) loadout.Options.Add(new BattleActionOption { Name = "Ability " + i });
            loadout.InitializeFavorites(); Assert.AreEqual(-1, loadout.Options[4].FavoriteSlot);
            Assert.IsTrue(loadout.AssignFavorite(loadout.Options[5], 0));
            Assert.AreEqual(-1, loadout.Options[0].FavoriteSlot); Assert.IsTrue(loadout.Options[0].Available);
            Assert.AreEqual(6, loadout.Options.Count); Assert.IsFalse(loadout.AssignFavorite(loadout.Options[0], 4));
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
    }
    [Test] public void EndTurnUsesKeyboardTAndRightStickClickButNotStart()
    {
        var method = Type.GetType("BattleMenuUI, Assembly-CSharp").GetMethod("PressedEndTurn", BindingFlags.Static | BindingFlags.NonPublic);
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var pad = InputSystem.AddDevice<Gamepad>();
        Press(keyboard.tKey);
        Assert.AreEqual(true, method.Invoke(null, new object[] { keyboard, pad }));
        InputSystem.Update();
        Assert.AreEqual(false, method.Invoke(null, new object[] { keyboard, pad }));
        Release(keyboard.tKey);
        Press(pad.startButton);
        Assert.AreEqual(false, method.Invoke(null, new object[] { keyboard, pad }));
        Release(pad.startButton);
        Press(pad.rightStickButton);
        Assert.AreEqual(true, method.Invoke(null, new object[] { keyboard, pad }));
        InputSystem.Update();
        Assert.AreEqual(false, method.Invoke(null, new object[] { keyboard, pad }));
    }
    [Test] public void ControllerFavoritesRequireTriggerAndMapABXY()
    {
        var method = Type.GetType("BattleMenuUI, Assembly-CSharp").GetMethod("PressedFavorite", BindingFlags.Static | BindingFlags.NonPublic);
        var pad = InputSystem.AddDevice<Gamepad>();
        try
        {
            var buttons = new[] { GamepadButton.South, GamepadButton.East, GamepadButton.West, GamepadButton.North };
            for (int i = 0; i < 4; i++)
            {
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(buttons[i])); InputSystem.Update();
                Assert.AreEqual(-1, method.Invoke(null, new object[] { null, pad }));
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                InputSystem.QueueStateEvent(pad, new GamepadState { leftTrigger = 1 }.WithButton(buttons[i])); InputSystem.Update();
                Assert.AreEqual(i, method.Invoke(null, new object[] { null, pad }));
            }
        }
        finally { InputSystem.RemoveDevice(pad); }
    }
}
