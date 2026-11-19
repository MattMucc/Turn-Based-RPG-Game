using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class BattleResourceView:MonoBehaviour {
 public Image Background;
 public RectTransform Health,Mana,Statuses;
 public TMP_Text Name,HealthText,ManaText,Resources;
 [Range(12,30)] public float StatusIconSize=16;
}
