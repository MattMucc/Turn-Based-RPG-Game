using System;
using System.Reflection;
using Game.GridPrototype;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object=UnityEngine.Object;

public class StatusBadgeTests
{
    [Test] public void BadgesCenterTheirCountsAndHoverTooltipTracksLiveEffectThenCloses()
    {
        var root=new GameObject("Status overlay",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(800,600);
        try
        {
            var row=new GameObject("Row",typeof(RectTransform));row.transform.SetParent(root.transform,false);
            var type=Type.GetType("BattleStatusRow, Assembly-CSharp",true);var component=row.AddComponent(type);
            type.GetMethod("Initialize").Invoke(component,new object[]{root.transform,TMP_Settings.defaultFontAsset});
            var unit=new GridUnit("Target",Vector2Int.zero){BurnTickDamage=8};unit.ApplyStatus(CombatStatus.Burning,3);unit.ApplyStatus(CombatStatus.Paralyzed,2);
            type.GetMethod("Show").Invoke(component,new object[]{unit,false});
            Assert.AreEqual(-18,((RectTransform)row.transform.GetChild(0)).anchoredPosition.x);Assert.AreEqual(18,((RectTransform)row.transform.GetChild(1)).anchoredPosition.x);
            Assert.AreEqual("3",row.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text);
            var hoverType=Type.GetType("StatusBadgeHover, Assembly-CSharp",true);
            var hover=row.transform.GetChild(0).GetComponent(hoverType);((Action)hoverType.GetField("Enter").GetValue(hover))();
            var tooltip=root.transform.Find("Status tooltip");Assert.IsTrue(tooltip.gameObject.activeSelf);StringAssert.Contains("8 damage",tooltip.GetComponentInChildren<TMP_Text>().text);
            unit.RemoveStatus(CombatStatus.Burning);type.GetMethod("Show").Invoke(component,new object[]{unit,false});
            Assert.IsFalse(tooltip.gameObject.activeSelf);Assert.AreEqual("Paralyzed",row.transform.GetChild(0).name);Assert.AreEqual(0,((RectTransform)row.transform.GetChild(0)).anchoredPosition.x);
            Assert.IsFalse(row.transform.GetChild(1).gameObject.activeSelf);
        }
        finally{Object.DestroyImmediate(root);}
    }
}
