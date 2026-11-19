using UnityEngine;
public enum PartyPageKind { Formation, Equipment, Attributes }
[CreateAssetMenu(menuName="Game/UI/Party Page Layout")]
public sealed class PartyPageLayout : ScriptableObject
{
    public PartyPageKind Page;
    public PartyPageView Prefab;
}
