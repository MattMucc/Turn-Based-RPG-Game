using UnityEngine;

namespace Game.GridPrototype
{
    // Opt in deliberately: ordinary scenery never becomes attackable automatically.
    public sealed class BattleDestructible : MonoBehaviour
    {
        public string DisplayName;
        [Tooltip("Red hover warning for objects that can harm characters when destroyed. Does not implement the harmful effect.")]
        public bool DangerousWhenDestroyed;
        public bool Pushable;
        [Min(0)] public int PushResistance;
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
