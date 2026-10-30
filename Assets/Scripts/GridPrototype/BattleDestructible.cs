using UnityEngine;

namespace Tenebrasia.GridPrototype
{
    // Opt in deliberately: ordinary scenery never becomes attackable automatically.
    public sealed class BattleDestructible : MonoBehaviour
    {
        public string DisplayName;
        [Min(1)] public int MaxHealth = 50;
        [Min(0)] public int Health = 50;
        public bool Alive => gameObject.activeInHierarchy && Health > 0;
        public void TakeDamage(int damage)
        {
            Health = Mathf.Max(0, Health - Mathf.Max(0, damage));
            if (Health == 0) gameObject.SetActive(false);
        }
    }
}
