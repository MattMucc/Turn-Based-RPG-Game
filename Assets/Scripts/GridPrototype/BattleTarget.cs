using UnityEngine;

namespace Game.GridPrototype
{
    public sealed class BattleTarget
    {
        public readonly Transform Transform;
        public readonly GridUnit Unit;
        public readonly BattleDestructible Destructible;
        public readonly int ActorIndex;
        public readonly Renderer[] Renderers;
        public BattleTarget(Transform actor, GridUnit unit, int index)
        { Transform = actor; Unit = unit; ActorIndex = index; Renderers = actor.GetComponentsInChildren<Renderer>(); }
        public BattleTarget(BattleDestructible prop)
        { Transform = prop.transform; Destructible = prop; ActorIndex = -1; Renderers = prop.GetComponentsInChildren<Renderer>(); }
        public BattleTarget(BattleHoverInfo info)
        { Transform = info.transform; ActorIndex = -1; Renderers = info.GetComponentsInChildren<Renderer>(); }
        public bool HasHealth => Unit != null || Destructible;
        public Color HoverColor(int activeActor)
        {
            var info = Transform ? Transform.GetComponent<BattleHoverInfo>() : null;
            var kind = info ? info.Kind : BattleHoverKind.Automatic;
            if ((Destructible && Destructible.DangerousWhenDestroyed) || kind == BattleHoverKind.Dangerous || kind == BattleHoverKind.Hostile) return Color.red;
            if (kind == BattleHoverKind.Automatic) kind = Unit != null ? (ActorIndex == 0 ? BattleHoverKind.Ally : BattleHoverKind.Hostile) : BattleHoverKind.Interactable;
            if (kind == BattleHoverKind.Ally) return Unit != null && ActorIndex == activeActor ? new Color(.5f, 1f, 0f) : Color.cyan;
            return kind == BattleHoverKind.Hostile ? Color.red : Color.yellow;
        }
        public bool Alive => Transform && Transform.gameObject.activeInHierarchy && (!HasHealth || Health > 0);
        public int Health => Unit != null ? Unit.Health : Destructible ? Destructible.Health : 0;
        public int MaxHealth => Unit != null ? Unit.MaxHealth : Destructible ? Destructible.MaxHealth : 1;
        public string Name
        {
            get
            {
                if (Destructible && !string.IsNullOrWhiteSpace(Destructible.DisplayName)) return Destructible.DisplayName;
                var hover = Transform ? Transform.GetComponent<BattleHoverInfo>() : null;
                if (hover && !string.IsNullOrWhiteSpace(hover.DisplayName)) return hover.DisplayName;
                var info = Transform ? Transform.GetComponent<BattleCharacterInfo>() : null;
                if (info && !string.IsNullOrWhiteSpace(info.DisplayName)) return info.DisplayName;
                return Transform ? Transform.name.Replace("(Clone)", "").Trim() : "";
            }
        }
        public Bounds Bounds
        {
            get
            {
                bool found = false; var bounds = new Bounds(Transform ? Transform.position : Vector3.zero, Vector3.one);
                foreach (var renderer in Renderers) if (renderer && renderer.enabled)
                { if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds); }
                return bounds;
            }
        }
        public bool Occupies(TerrainBattleGrid grid, Vector2Int cell)
        {
            if (Unit != null) return Unit.Position == cell;
            if (!grid.Board.Contains(cell)) return false;
            var bounds = Bounds; var point = grid.Point(cell);
            float half = grid.CellSize * 0.49f;
            return bounds.min.x <= point.x + half && bounds.max.x >= point.x - half
                && bounds.min.z <= point.z + half && bounds.max.z >= point.z - half;
        }
    }

    // Hover temporarily replaces the pin. Dismiss suppresses the current hover until it changes.
    public sealed class BattleTargetFocus
    {
        public BattleTarget Hovered { get; private set; }
        public BattleTarget Pinned { get; private set; }
        private BattleTarget suppressed;
        public BattleTarget Displayed => Hovered?.Alive == true ? Hovered : Pinned?.Alive == true ? Pinned : null;
        public void Hover(BattleTarget target)
        {
            if (target != suppressed) suppressed = null;
            Hovered = target != suppressed && target?.Alive == true ? target : null;
        }
        public void Pin(BattleTarget target) { Pinned = target?.Alive == true ? target : null; }
        public void Dismiss() { suppressed = Hovered; Hovered = null; Pinned = null; }
    }
}
