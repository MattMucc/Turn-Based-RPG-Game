using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Game.GridPrototype;
using Object = UnityEngine.Object;

public class PortraitBarTests
{
    [Test] public void CompactBarsStackAtBottomAndUseFittingCurrentMaximumLabels()
    {
        var root = new GameObject("Test HUD", typeof(RectTransform));
        try
        {
            var type = Type.GetType("BattleHud, Assembly-CSharp", true);
            var hud = root.AddComponent(type);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("orderContent", flags).SetValue(hud, root.transform);
            type.GetField("font", flags).SetValue(hud, TMP_Settings.defaultFontAsset);
            var card = type.GetMethod("MakeCard", flags).Invoke(hud, null);
            var cardType = card.GetType();
            var rect = (RectTransform)cardType.GetField("Rect").GetValue(card); rect.sizeDelta = new Vector2(52, 62);
            var health = (TMP_Text)cardType.GetField("Health").GetValue(card);
            var shield = (RectTransform)cardType.GetField("Shield").GetValue(card);
            var shieldText = (TMP_Text)cardType.GetField("ShieldText").GetValue(card);
            var fill = (RectTransform)cardType.GetField("ShieldFill").GetValue(card);
            var hp = (RectTransform)health.transform.parent;
            Assert.AreEqual(7.5f, hp.rect.height); Assert.AreEqual(7.5f, shield.rect.height);
            Assert.AreEqual(3, hp.offsetMin.y); Assert.Greater(shield.offsetMin.y, hp.offsetMax.y);
            Assert.IsTrue(health.enableAutoSizing); Assert.IsTrue(shieldText.enableAutoSizing);
            Assert.AreEqual(TextWrappingModes.NoWrap, health.textWrappingMode);
            var unit = new GridUnit("Test", Vector2Int.zero); unit.AddBarrier(30); unit.TakeDamage(7);
            type.GetMethod("UpdateShield", flags).Invoke(hud, new object[] { shield, fill, shieldText, unit });
            Assert.AreEqual("23 / 30", shieldText.text);
            var fitting = type.GetMethod("FitPortraitNumber", BindingFlags.Static | BindingFlags.NonPublic);
            fitting.Invoke(null, new object[] { health, rect, .17f }); float small = health.fontSizeMax;
            rect.sizeDelta = new Vector2(104, 62); fitting.Invoke(null, new object[] { health, rect, .17f });
            Assert.Greater(health.fontSizeMax, small);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
