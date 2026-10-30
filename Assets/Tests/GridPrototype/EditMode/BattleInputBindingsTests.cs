using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleInputBindingsTests
{
    [Test] public void BlockAndRunUseDistinctRequestedKeys()
    {
        var asset = InputActionAsset.FromJson(File.ReadAllText("Assets/PlayerInputActions.inputactions"));
        try
        {
            var map = asset.FindActionMap("Battle", true);
            Assert.IsTrue(Has(map.FindAction("Block"), "<Keyboard>/4"));
            Assert.IsTrue(Has(map.FindAction("Block"), "<Gamepad>/rightShoulder"));
            Assert.IsTrue(Has(map.FindAction("Run"), "<Keyboard>/5"));
            Assert.IsFalse(Has(map.FindAction("Run"), "<Keyboard>/4"));
            Assert.IsTrue(Has(map.FindAction("Run"), "<Gamepad>/buttonEast"));
        }
        finally { Object.DestroyImmediate(asset); }
    }
    private static bool Has(InputAction action, string path)
    { foreach (var binding in action.bindings) if (binding.path == path) return true; return false; }
}
