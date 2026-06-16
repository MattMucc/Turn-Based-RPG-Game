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
        if (!other.CompareTag("Player")) return;

        BattleGroup.TriggerBattle();
    }
}