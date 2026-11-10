using Game.GridPrototype;
using UnityEngine;

public partial class GameManager
{
    public bool TrySetCaptain(BattleCharacterInfo member)
    {
        if (!member || IsSwitchingCharacter || (BattleManager.Instance && BattleManager.Instance.InBattle)) return false;
        int index = BattleParty.IndexOf(member);
        if (index <= 0) return false;
        BattleParty.RemoveAt(index); BattleParty.Insert(0, member);
        return true;
    }
    // The selected reserve replaces an existing member at their actual overworld position.
    public bool TrySetActiveMember(int slot, BattleCharacterInfo incoming)
    {
        if (!incoming || slot<0 || slot>=BattleParty.Count || slot>=3 || IsSwitchingCharacter
            || (WorldTools && WorldTools.IsOpen) || (BattleManager.Instance && BattleManager.Instance.InBattle)
            || BattleParty.Contains(incoming) || !ReserveParty.Contains(incoming)) return false;
        var outgoing=BattleParty[slot];
        var movement=incoming.GetComponent<PlayerMovement>();
        if (!outgoing || !movement) return false;
        bool controlled=PossessedCharacter && PossessedCharacter.transform==outgoing.transform;
        int reserveSlot=ReserveParty.IndexOf(incoming);
        incoming.transform.SetPositionAndRotation(outgoing.transform.position,outgoing.transform.rotation);
        var oldMovement=outgoing.GetComponent<PlayerMovement>();if(oldMovement)oldMovement.Unpossess();
        outgoing.gameObject.SetActive(false);
        BattleParty[slot]=incoming;ReserveParty[reserveSlot]=outgoing;
        if(controlled)PossessCharacter(movement);else ConfigureFollowers();
        Physics.SyncTransforms();return true;
    }
}
