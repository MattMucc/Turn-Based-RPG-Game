using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class PartyMemberCardView : MonoBehaviour
{
    [Range(0,5)] public int RosterIndex;
    public Button Select;
    public RawImage Portrait;
    public TMP_Text Name,Role,Level,Vitals;
    public Image ExperienceFill;
}
