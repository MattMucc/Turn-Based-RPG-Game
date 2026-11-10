using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public enum BattleActionCategory { Attack, Skill, Item, Magic }
    public enum BattleTargetPattern { Single, Area, Piercing, Multiple, Sweep, CustomSingle, CustomArea }

    [Serializable]
    public sealed class BattleActionOption
    {
        public string Name;
        [Range(-1, 3)] public int FavoriteSlot = -1;
        [TextArea] public string Description;
        public BattleActionCategory Category;
        [Tooltip("Used only for Skills. Costs are paid once when the action is committed.")]
        [Range(1, 9)] public int SkillPointCost = 1;
        [Tooltip("Optional self recovery once after a Skill completes, not per hit. Normally zero.")]
        [Range(0, 9)] public int SkillPointRecovery;
        public int EffectiveSkillPointCost => Category == BattleActionCategory.Skill ? Mathf.Clamp(SkillPointCost, 1, 9) : 0;
        public bool CanUse(GridUnit source) => Available && source != null && source.CanAffordSkillPoints(EffectiveSkillPointCost);
        public void CompleteResourceRecovery(GridUnit source)
        {
            if (Category == BattleActionCategory.Attack && Effect == ActionEffect.Damage) source.RestoreSkillPoints(source.AttackSkillPointGain);
            else if (Category == BattleActionCategory.Skill) source.RestoreSkillPoints(Mathf.Clamp(SkillPointRecovery, 0, 9));
        }
        public BattleTargetPattern Pattern;
        [Min(1)] public int Reach = 1;
        [Min(0)] public int Radius = 1;
        [Tooltip("Gray cells in the custom pattern. Unlisted cells are enabled; offsets outside the current size are preserved but ignored.")]
        public List<Vector2Int> DisabledPatternCells = new();
        [Tooltip("Custom AoE only: rotate the footprint in quarter turns toward the aim direction. Top of the editor means forward; ties choose the horizontal direction.")]
        public bool RotateAreaTowardTarget;
        [Min(1)] public int MaxTargets = 3;
        [Min(0)] public int Damage = 25;
        public string ItemId;
        [UnityEngine.Serialization.FormerlySerializedAs("Quantity")]
        [SerializeField, Min(0)] private int quantity;
        [NonSerialized] private BattleActionOption stock;
        public int Quantity { get => stock != null ? stock.Quantity : quantity; set { if (stock != null) stock.Quantity = value; else quantity = Mathf.Max(0, value); } }
        public void BindStock(BattleActionOption source) { stock = source == this ? null : source; }
        [Min(0)] public int Healing;
        public ActionEffect Effect;
        public DamageKind DamageType;
        public CombatElement Element;
        public ScalingStat Scaling;
        [Min(0)] public float StatScaling;
        [Range(0, 100)] public float HitChance = 100;
        public CombatStatus InflictedStatus;
        [Range(0, 100)] public float StatusChance = 100;
        [Range(1, 10)] public int StatusStacks = 1;
        [Min(0)] public int BarrierGranted;
        public bool SignatureQte;
        public QteMode QteType;
        [Min(1)] public int QteRequiredTaps = 12;
        [Min(0.1f)] public float QteHoldDuration = 2;
        [Tooltip("Fraction of a full tap/hold meter lost per second without input. 0.2 means 20 percentage points per second.")]
        [Min(0)] public float QteProgressDrainPerSecond = 0.2f;
        [Tooltip("Seconds after the most recent tap before the tap meter starts draining. Holding the button does not count as tapping.")]
        [Min(0)] public float QteTapDrainDelay = 0.3f;
        [Tooltip("SinglePress ignores this array. FixedSequence: up to 8 face/D-pad inputs, shared deadline. Staged: up to 8 prompts, QteDuration per prompt; left stick allowed. DualInput: exactly two entries, left-stick direction then face button.")]
        public QteButton[] QteSequence = Array.Empty<QteButton>();
        [Min(0.1f)] public float QteDuration = 1.5f;
        [Min(0)] public int QteBonusDamage = 15;
        public int CalculateDamage(GridUnit source, GridUnit target, int power) => CombatMath.Damage(source, target, power, DamageType, Scaling, StatScaling);
        public bool ApplyHit(GridUnit source, GridUnit target, int power, float hitRoll, float statusRoll, out int healthLost)
        {
            healthLost = 0;
            if (target.Health <= 0 || !CombatMath.Roll(HitChance, hitRoll)) return false;
            if (Effect == ActionEffect.Damage)
                healthLost = target.TakeDamage(CombatMath.Damage(source, target, power, DamageType, Scaling, StatScaling));
            if (Effect == ActionEffect.Heal) healthLost = -target.Heal(Healing);
            if (CombatMath.Roll(StatusChance, statusRoll)) target.ApplyStatus(InflictedStatus, StatusStacks);
            target.AddBarrier(BarrierGranted);
            return true;
        }
        public bool Available => Category != BattleActionCategory.Item || Quantity > 0;
        public string Tooltip => TooltipFor(null);
        public string TooltipFor(GridUnit source, GridUnit target = null)
        {
            string result = $"<b>{Name}</b>\n{Description}\n";
            string Row(string label, string value) => $"\n<b>{label}</b><pos=105>{value}";
            if (Category == BattleActionCategory.Skill)
            {
                result += Row("Cost", $"{EffectiveSkillPointCost} SP");
                if (source != null && !source.CanAffordSkillPoints(EffectiveSkillPointCost)) result += Row("Available", $"{source.SkillPoints} SP — insufficient");
                if (SkillPointRecovery > 0) result += Row("Recovery", $"{Mathf.Clamp(SkillPointRecovery, 0, 9)} SP after use");
            }
            if (Effect == ActionEffect.Dash) return result + Row("Movement", "+3 points");
            result += Row("Reach", $"{Reach} tiles");
            if (Effect == ActionEffect.Damage)
            {
                int damage = CombatMath.Damage(source, target, Damage, DamageType, Scaling, StatScaling);
                string amount = damage.ToString();
                if (SignatureQte && QteBonusDamage > 0)
                    amount += "–" + CombatMath.Damage(source, target, Damage + QteBonusDamage, DamageType, Scaling, StatScaling) + " (QTE)";
                if (Pattern == BattleTargetPattern.Multiple) amount += " per hit";
                result += Row("Damage", amount);
                result += Row("Defense", target == null ? "Before mitigation" : "Target included");
            }
            if (Effect == ActionEffect.Heal) result += Row("Healing", $"Up to {Healing} HP");
            result += Row("Hit Chance", $"{Mathf.Clamp(HitChance, 0, 100):0.#}%");
            bool single = Pattern == BattleTargetPattern.Single || Pattern == BattleTargetPattern.CustomSingle;
            result += Row("Type", Pattern == BattleTargetPattern.Multiple ? "Multi-Target" : single ? "Single" : Pattern == BattleTargetPattern.CustomArea ? "Area" : Pattern.ToString());
            if (single || Pattern == BattleTargetPattern.Multiple)
                result += Row("Targets", Pattern == BattleTargetPattern.Multiple ? MaxTargets.ToString() : "1");
            result += Row("Element", Element.ToString());
            if (Effect == ActionEffect.Damage) result += Row("Damage Type", DamageType.ToString());
            if (Category == BattleActionCategory.Item) result += Row("Cost", $"1 item ({Quantity} carried)");
            if (InflictedStatus != CombatStatus.None)
                result += Row("Afflictions", $"{InflictedStatus} ×{Mathf.Clamp(StatusStacks, 1, 10)}")
                    + $"\n<indent=105>{Mathf.Clamp(StatusChance, 0, 100):0.#}% per landed hit</indent>";
            if (BarrierGranted > 0) result += Row("Barrier", BarrierGranted.ToString());
            return result;
        }

    }

    // Ability favorites belong to characters; runtime item stocks are supplied by the party inventory.
    // These are demonstration options, not approved character kits or balance.
    public sealed class BattleActionLoadout : MonoBehaviour
    {
        public SharedBattleActionLibrary SharedLibrary;
        [UnityEngine.Serialization.FormerlySerializedAs("Options")]
        [SerializeField] private List<BattleActionOption> personalOptions = CreateDefaultOptions();
        public List<BattleActionOption> PersonalOptions => personalOptions;
        private List<BattleActionOption> combinedOptions;
        private SharedBattleActionLibrary cachedLibrary;
        private int cachedRevision = -1, cachedInventoryRevision = -1, cachedItemCount = -1;
        private PartyInventory inventory;
        private readonly Dictionary<string, BattleActionOption> itemInstances = new();
        public void BindInventory(PartyInventory shared)
        {
            if (ReferenceEquals(inventory, shared)) return;
            inventory = shared; combinedOptions = null; cachedInventoryRevision = -1; favoritesInitialized = false;
        }
        private readonly Dictionary<string, BattleActionOption> sharedInstances = new();
        public List<BattleActionOption> Options
        {
            get
            {
                if (!SharedLibrary && inventory == null) return personalOptions;
                if (combinedOptions != null && cachedLibrary == SharedLibrary && cachedRevision == (SharedLibrary ? SharedLibrary.Revision : -1) && cachedInventoryRevision == (inventory?.Revision ?? -1) && cachedItemCount == (inventory?.Items.Count ?? -1)) return combinedOptions;
                if (cachedLibrary != SharedLibrary) sharedInstances.Clear();
                combinedOptions = new List<BattleActionOption>();
                var names = new HashSet<string>();
                foreach (var definition in SharedLibrary ? SharedLibrary.Actions : new List<BattleActionOption>())
                {
                    if (definition == null || definition.Category == BattleActionCategory.Item || string.IsNullOrWhiteSpace(definition.Name) || !names.Add(definition.Name)) continue;
                    var copy = JsonUtility.FromJson<BattleActionOption>(JsonUtility.ToJson(definition));
                    if (sharedInstances.TryGetValue(definition.Name, out var previous)) copy.FavoriteSlot = previous.FavoriteSlot;
                    sharedInstances[definition.Name] = copy; combinedOptions.Add(copy);
                }
                foreach (var option in personalOptions) if (option != null && !names.Contains(option.Name) && (inventory == null || option.Category != BattleActionCategory.Item)) combinedOptions.Add(option);
                if (inventory != null)
                {
                    var itemIds = new HashSet<string>();
                    foreach (var source in inventory.Items)
                    {
                        if (source == null || source.Category != BattleActionCategory.Item) continue;
                        string id = string.IsNullOrEmpty(source.ItemId) ? source.Name : source.ItemId;
                        if (string.IsNullOrEmpty(id) || !itemIds.Add(id)) continue;
                        var copy = JsonUtility.FromJson<BattleActionOption>(JsonUtility.ToJson(source));
                        if (itemInstances.TryGetValue(id, out var previous)) copy.FavoriteSlot = previous.FavoriteSlot;
                        copy.BindStock(source); itemInstances[id] = copy; combinedOptions.Add(copy);
                    }
                }
                cachedLibrary = SharedLibrary; cachedRevision = SharedLibrary ? SharedLibrary.Revision : -1;
                cachedInventoryRevision = inventory?.Revision ?? -1; cachedItemCount = inventory?.Items.Count ?? -1; favoritesInitialized = false;
                return combinedOptions;
            }
        }
        [SerializeField] private bool favoritesInitialized;
        public void InitializeFavorites()
        {
            var options = Options; // Refresh a changed library before checking the favorites flag.
            if (favoritesInitialized) return;
            favoritesInitialized = true;
            foreach (BattleActionCategory category in Enum.GetValues(typeof(BattleActionCategory)))
            {
                var used = new HashSet<int>();
                foreach (var option in Options) if (option != null && option.Category == category && option.FavoriteSlot >= 0)
                    if (!used.Add(option.FavoriteSlot)) option.FavoriteSlot = -1;
                foreach (var option in Options)
                {
                    if (option == null || option.Category != category || option.FavoriteSlot >= 0) continue;
                    for (int slot = 0; slot < 4; slot++) if (used.Add(slot)) { option.FavoriteSlot = slot; break; }
                }
            }
        }
        public BattleActionOption Favorite(BattleActionCategory category, int slot)
        { InitializeFavorites(); return slot < 0 || slot >= 4 ? null : Options.Find(o => o != null && o.Category == category && o.FavoriteSlot == slot); }
        public bool AssignFavorite(BattleActionOption option, int slot)
        {
            InitializeFavorites();
            if (option == null || !Options.Contains(option) || slot < -1 || slot >= 4) return false;
            if (slot >= 0) foreach (var other in Options)
                if (other != null && other.Category == option.Category && other.FavoriteSlot == slot) other.FavoriteSlot = -1;
            option.FavoriteSlot = slot; return true;
        }
        public void EnsureUniversalAttacks()
        {
            if (SharedLibrary) { var shared = Options; return; } // Removing a common action must really remove it.
            if (!Options.Exists(o => o != null && o.Effect == ActionEffect.Push))
                Options.Add(new BattleActionOption { Name = "Push", Effect = ActionEffect.Push, Damage = 0, Description = "Push one cell on clear, level ground. Requires strictly greater Strength." });
            int basics = Options.FindAll(o => o != null && o.Category == BattleActionCategory.Attack && o.Effect == ActionEffect.Damage).Count;
            if (basics == 0) Options.Add(new BattleActionOption { Name = "Strike", Damage = 12, Scaling = ScalingStat.Strength, StatScaling = 1, Description = "A simple adjacent physical attack." });
            if (basics < 2) Options.Add(new BattleActionOption { Name = "Quick Strike", Damage = 8, Scaling = ScalingStat.Strength, StatScaling = .75f, Description = "A lighter adjacent physical attack." });
        }




        private void OnValidate()
        {
            combinedOptions = null;
            foreach (var option in Options)
                if (option != null && (option.QteType == QteMode.FixedSequence || option.QteType == QteMode.Staged || option.QteType == QteMode.DualInput))
                    option.QteSequence = PressQte.Normalize(option.QteType, option.QteSequence);
        }


        private static List<BattleActionOption> CreateDefaultOptions()
        {
            var options = new List<BattleActionOption>
        {
            new BattleActionOption { Name = "Strike", Description = "Prototype adjacent attack." },
            new BattleActionOption { Name = "Piercing Attack", Description = "Prototype line targeting.", Pattern = BattleTargetPattern.Piercing, Reach = 4, Damage = 18 },
            new BattleActionOption { Name = "Dash", Description = "Gain movement to reposition this turn.", Category = BattleActionCategory.Skill, Effect = ActionEffect.Dash, Damage = 0 },
            new BattleActionOption { Name = "Push", Category = BattleActionCategory.Attack, Effect = ActionEffect.Push, Damage = 0, Description = "Push one cell forward. Requires higher Strength." }
        };
            return options;
        }
    }

    public static class GridTargeting
    {
        public static Quaternion Facing(Vector3 direction)
        {
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return Quaternion.Euler(0, Mathf.Round(yaw / 45f) * 45f, 0);
        }

        public static int Distance(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        // Supercover line: diagonals cannot shoot through two touching wall corners.
        public static bool Clear(GridBoard board, Vector2Int from, Vector2Int to, bool allowBlockedDestination = false)
        {
            if (!board.Contains(from) || !board.Contains(to)) return false;
            int steps = Distance(from, to);
            Vector2Int last = from;
            for (int i = 1; i <= steps; i++)
            {
                var p = Vector2Int.RoundToInt(Vector2.Lerp(from, to, i / (float)steps));
                if (!board.Contains(p) || (board.At(p).Ground == Ground.Obstacle && !(p == to && allowBlockedDestination))) return false;
                if (p.x != last.x && p.y != last.y &&
                    (board.At(new Vector2Int(last.x, p.y)).Ground == Ground.Obstacle ||
                     board.At(new Vector2Int(p.x, last.y)).Ground == Ground.Obstacle)) return false;
                last = p;
            }
            return true;
        }

        public static bool CanTarget(GridBoard board, Vector2Int from, Vector2Int to, BattleActionOption action,
            Func<Vector2Int, Vector2Int, bool> sight = null, Func<Vector2Int, bool> targetableObstacle = null)
        {
            int distance = Distance(from, to);
            if ((distance == 0 && action.Pattern == BattleTargetPattern.Piercing) || distance > action.Reach || !board.Contains(to)) return false;
            var d = to - from;
            if (action.Pattern == BattleTargetPattern.CustomSingle && action.DisabledPatternCells != null && action.DisabledPatternCells.Contains(d)) return false;
            if (action.Pattern == BattleTargetPattern.Sweep && (distance == 0 || (d.x != 0 && d.y != 0))) return false;
            if (action.Pattern == BattleTargetPattern.Piercing && d.x != 0 && d.y != 0 && Mathf.Abs(d.x) != Mathf.Abs(d.y)) return false;
            return Clear(board, from, to, targetableObstacle?.Invoke(to) ?? false) && (sight == null || sight(from, to));
        }

        public static HashSet<Vector2Int> Footprint(GridBoard board, Vector2Int from, Vector2Int target,
            BattleActionOption action, Func<Vector2Int, Vector2Int, bool> sight = null, Func<Vector2Int, bool> targetableObstacle = null)
        {
            var result = new HashSet<Vector2Int>();
            if (!CanTarget(board, from, target, action, sight, targetableObstacle)) return result;
            if (action.Pattern == BattleTargetPattern.Piercing)
            {
                var delta = target - from;
                var step = new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
                for (int i = 1; i <= action.Reach; i++)
                {
                    var p = from + step * i;
                    if (!CanTarget(board, from, p, action, sight, targetableObstacle)) break;
                    result.Add(p);
                    if (board.At(p).Ground == Ground.Obstacle) break;
                }
            }
            else if (action.Pattern == BattleTargetPattern.Sweep)
            {
                var across = target.x == from.x ? Vector2Int.right : Vector2Int.up;
                for (int i = -action.Radius; i <= action.Radius; i++)
                {
                    var p = target + across * i;
                    if (p != from && board.Contains(p) && Clear(board, from, p, targetableObstacle?.Invoke(p) ?? false) && (sight == null || sight(from, p))) result.Add(p);
                }
            }
            else if (action.Pattern == BattleTargetPattern.Area || action.Pattern == BattleTargetPattern.CustomArea)
            {
                for (int x = -action.Radius; x <= action.Radius; x++) for (int y = -action.Radius; y <= action.Radius; y++)
                {
                    var offset = new Vector2Int(x, y);
                    if (action.Pattern == BattleTargetPattern.CustomArea)
                    {
                        if (action.DisabledPatternCells != null && action.DisabledPatternCells.Contains(offset)) continue;
                        if (action.RotateAreaTowardTarget) offset = RotateOffset(offset, target - from);
                    }
                    var p = target + offset;
                    if (board.Contains(p) && Clear(board, target, p, targetableObstacle?.Invoke(p) ?? false) && (sight == null || sight(target, p))) result.Add(p);
                }
            }
            else result.Add(target);
            return result;
        }

        // Cardinal rotations preserve exact authored tiles, unlike rounding a 45-degree rotation.
        public static Vector2Int RotateOffset(Vector2Int offset, Vector2Int direction)
        {
            if (direction == Vector2Int.zero) return offset;
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                return direction.x > 0 ? new Vector2Int(offset.y, -offset.x) : new Vector2Int(-offset.y, offset.x);
            return direction.y > 0 ? offset : -offset;
        }
    }
}
