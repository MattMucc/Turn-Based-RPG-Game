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
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (!player || !player.IsPossessed) return;

        BattleGroup.TriggerBattle();
    }
}