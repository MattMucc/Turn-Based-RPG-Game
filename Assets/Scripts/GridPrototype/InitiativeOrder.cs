using System;
using System.Collections.Generic;

namespace Game.GridPrototype
{
    // Actor IDs stay stable for targeting/portraits; only the schedule is sorted.
    public sealed class InitiativeOrder
    {
        readonly List<int> normal = new List<int>();
        readonly List<int> opening;
        readonly List<int> values;
        List<int> current;
        public int Round { get; private set; } = 1;
        public InitiativeOrder(IReadOnlyList<int> values, int preemptiveActor = -1)
        {
            this.values = new List<int>(values);
            for (int i = 0; i < values.Count; i++) normal.Add(i);
            normal.Sort((a, b) => { int c = values[b].CompareTo(values[a]); return c != 0 ? c : a.CompareTo(b); });
            opening = new List<int>(normal);
            if (opening.Remove(preemptiveActor)) opening.Insert(0, preemptiveActor);
            current = new List<int>(opening);
        }
        List<int> Current => current;
        public void SetInitiative(int actor, int value)
        {
            values[actor] = value;
            normal.Sort((a, b) => { int c = values[b].CompareTo(values[a]); return c != 0 ? c : a.CompareTo(b); });
        }
        public int First => Current.Count == 0 ? -1 : Current[0];
        public int Next(int actor, Func<int, bool> alive)
        {
            int position = Current.IndexOf(actor) + 1;
            for (int attempts = 0; attempts <= normal.Count; attempts++)
            {
                if (position >= Current.Count) { Round++; current = new List<int>(normal); position = 0; }
                if (Current.Count == 0) return -1;
                int candidate = Current[position++];
                if (alive(candidate)) return candidate;
            }
            return -1;
        }
        public List<int> Preview(int actor, Func<int, bool> alive)
        {
            var result = new List<int>();
            int position = Math.Max(0, Current.IndexOf(actor));
            for (int i = position; i < Current.Count; i++) if (alive(Current[i])) result.Add(Current[i]);
            int count = normal.FindAll(i => alive(i)).Count;
            foreach (int i in normal) { if (result.Count >= count) break; if (alive(i)) result.Add(i); }
            return result;
        }
    }
}
