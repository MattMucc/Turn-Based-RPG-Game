using UnityEngine;

public class EnemyTrigger : MonoBehaviour
{
    public BattleGroup BattleGroup {  get; private set; }

    public void Initialize(BattleGroup battleGroup)
    {
        BattleGroup = battleGroup;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryEncounter(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryEncounter(other);
    }

    public static bool IsControlledActor(PlayerMovement player) => player && player.IsPossessed && GameManager.Instance && GameManager.Instance.PossessedCharacter == player;

    private void TryEncounter(Collider other)
    {
        if (!BattleGroup || (GameManager.Instance && GameManager.Instance.WorldInputBlocked)) return;
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (!IsControlledActor(player)) return;

        BattleGroup.TriggerBattle();
    }
}
