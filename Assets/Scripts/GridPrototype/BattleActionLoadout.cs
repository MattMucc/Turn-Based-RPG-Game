using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public enum BattleActionCategory { Attack, Skill, Item, Magic }
    public enum BattleTargetPattern { Single, Area, Piercing, Multiple, Sweep }

    [Serializable]
    public sealed class BattleActionOption
    {
        public string Name;
        [Range(-1, 3)] public int FavoriteSlot = -1;
        [TextArea] public string Description;
        public BattleActionCategory Category;
        public BattleTargetPattern Pattern;
        [Min(1)] public int Reach = 1;
        [Min(0)] public int Radius = 1;
        [Min(1)] public int MaxTargets = 3;
        [Min(0)] public int Damage = 25;
        [Min(0)] public int Quantity;
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
        public bool ApplyHit(GridUnit source, GridUnit target, int power, float hitRoll, float statusRoll, out int healthLost)
        {
            healthLost = 0;
            if (target.Health <= 0 || !CombatMath.Roll(HitChance, hitRoll)) return false;
            if (Effect == ActionEffect.Damage)
                healthLost = target.TakeDamage(CombatMath.Damage(source, target, power, DamageType, Scaling, StatScaling));
            if (CombatMath.Roll(StatusChance, statusRoll)) target.ApplyStatus(InflictedStatus, StatusStacks);
            target.AddBarrier(BarrierGranted);
            return true;
        }
        public bool Available => Category != BattleActionCategory.Item || Quantity > 0;
        public string EffectTooltip => Effect == ActionEffect.Push ? "\n<b>Push</b>  1 cell forward; requires strictly higher Strength. Blocked destinations stop the push." :
            $"\n<b>Hit chance</b>  {HitChance:0.#}%" +
            (Effect == ActionEffect.Damage ? $"\n{DamageType} / {Element}; scaling: {Scaling} × {StatScaling:0.##}. Defense and Block reduce damage." : "") +
            (InflictedStatus != CombatStatus.None ? $"\n<b>{InflictedStatus}</b>  {StatusChance:0.#}% per landed hit; +{StatusStacks} stacks (maximum 10)." : "") +
            (BarrierGranted > 0 ? $"\n<b>Barrier</b>  +{BarrierGranted} absorption." : "");
        public string Tooltip => $"<b>{Name}</b>\n{Description}\n\n<b>Reach</b>   {Reach} tiles from the user\n<b>Damage</b>   {Damage} per hit\n<b>Cost</b>   1 action\n\n<b>Targets</b>\n"
            + (Pattern == BattleTargetPattern.Sweep ? $"A {Radius * 2 + 1}-cell line across the selected direction. Aim north, south, east or west; no diagonal aiming. Allies in the line can be hit." :
               Pattern == BattleTargetPattern.Area ? $"Everyone within {Radius} tile(s) of the chosen cell. Allies and destructible objects are also hit." :
               Pattern == BattleTargetPattern.Piercing ? "Hits characters and destructible objects in a straight or diagonal line, including allies. Stops at terrain." :
               Pattern == BattleTargetPattern.Multiple ? $"Assign {MaxTargets} hits to start. Repeated cells and allies are allowed. Hits resolve in selection order." : "One cell. One hit. Can hit allies or destructible objects.")
            + (Category == BattleActionCategory.Item ? $"\n\n<b>Carried</b>   {Quantity}\nUses one item when the action starts." : "")
            + (SignatureQte ? $"\n\n<b>Signature QTE</b>\n{(QteType == QteMode.DualInput ? "Hold the direction and face button together; either input may be held first" : QteType == QteMode.Staged ? "Respond to each prompt; success or failure advances to the next. Timer resets per prompt. All prompts must succeed for bonus damage" : QteType == QteMode.FixedSequence ? "Complete the displayed fixed sequence" : QteType == QteMode.RepeatedTap ? $"Fill the meter by tapping ({Mathf.Max(1, QteRequiredTaps)} taps without decay); pausing gradually drains it" : QteType == QteMode.Hold ? $"Fill the meter by holding ({QteHoldDuration:0.0}s without decay); releasing gradually drains it" : "Press Space / south face button")} before {Mathf.Max(0.1f, QteDuration):0.0}s expires.\nSuccess: +{Mathf.Max(0, QteBonusDamage)} damage per hit ({Damage + Mathf.Max(0, QteBonusDamage)} total).\nWrong input or timeout: normal damage." : "")
            + EffectTooltip
            + "\nEmpty cells still spend the action. Out-of-reach targets can be approached using remaining movement; all selected hits must fit from one destination.\n\n<color=#80BEFF>Blue tiles</color>: within reach now\n<color=#FF9090>Red tiles</color>: affected by this action";

    }

    // Carried quantities belong to the character, not a newly created encounter.
    // These are demonstration options, not approved character kits or balance.
    public sealed class BattleActionLoadout : MonoBehaviour
    {
        [SerializeField] private bool favoritesInitialized;
        public void InitializeFavorites()
        {
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
            if (!Options.Exists(o => o != null && o.Effect == ActionEffect.Push))
                Options.Add(new BattleActionOption { Name = "Push", Effect = ActionEffect.Push, Damage = 0, Description = "Push one cell on clear, level ground. Requires strictly greater Strength." });
            int basics = Options.FindAll(o => o != null && o.Category == BattleActionCategory.Attack && o.Effect == ActionEffect.Damage).Count;
            if (basics == 0) Options.Add(new BattleActionOption { Name = "Strike", Damage = 12, Scaling = ScalingStat.Strength, StatScaling = 1, Description = "A simple adjacent physical attack." });
            if (basics < 2) Options.Add(new BattleActionOption { Name = "Quick Strike", Damage = 8, Scaling = ScalingStat.Strength, StatScaling = .75f, Description = "A lighter adjacent physical attack." });
        }
        public static BattleActionOption CreatePrototypeSignature() => new BattleActionOption
        {
            Name = "Signature Strike", Description = "Prototype cinematic attack. Press when prompted to strengthen the impact.",
            Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Single, Reach = 1,
            Damage = 30, SignatureQte = true, QteDuration = 1.5f, QteBonusDamage = 15
        };
        public static BattleActionOption CreatePrototypeSequence() => new BattleActionOption
        {
            QteType = QteMode.FixedSequence, Name = "Sequence Strike", Description = "Complete the fixed input sequence to strengthen the impact.",
            Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Single, Reach = 1,
            Damage = 30, SignatureQte = true, QteDuration = 4, QteBonusDamage = 15,
            QteSequence = new[] { QteButton.West, QteButton.North, QteButton.East, QteButton.South }
        };
        public static BattleActionOption CreatePrototypeMash() => new BattleActionOption
        {
            Name = "Burst Strike", Description = "Repeatedly tap the displayed button to charge the impact.",
            Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Single, Reach = 1,
            Damage = 30, SignatureQte = true, QteType = QteMode.RepeatedTap,
            QteDuration = 4, QteRequiredTaps = 12, QteBonusDamage = 15
        };
        public static BattleActionOption CreatePrototypeHold() => new BattleActionOption
        {
            Name = "Charged Strike", Description = "Hold the displayed button to charge the impact. Release gradually drains the charge.",
            Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Single, Reach = 1,
            Damage = 30, SignatureQte = true, QteType = QteMode.Hold,
            QteDuration = 4, QteHoldDuration = 2, QteBonusDamage = 15
        };
        private void OnValidate()
        {
            foreach (var option in Options)
                if (option != null && (option.QteType == QteMode.FixedSequence || option.QteType == QteMode.Staged || option.QteType == QteMode.DualInput))
                    option.QteSequence = PressQte.Normalize(option.QteType, option.QteSequence);
        }
        public static BattleActionOption CreatePrototypeStaged() => new BattleActionOption
        {
            Name = "Flow Strike", Description = "React to one prompt at a time. Misses still advance to the next prompt.",
            Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Single, Reach = 1,
            Damage = 30, SignatureQte = true, QteType = QteMode.Staged, QteDuration = 2, QteBonusDamage = 15,
            QteSequence = new[] { QteButton.StickRight, QteButton.East, QteButton.DpadUp, QteButton.South }
        };
        public static BattleActionOption CreatePrototypeDual() => new BattleActionOption
        {
            Name = "Twin Strike", Description = "Combine a direction with a face button. Either may be held first.",
            Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Single, Reach = 1,
            Damage = 30, SignatureQte = true, QteType = QteMode.DualInput, QteDuration = 3, QteBonusDamage = 15,
            QteSequence = new[] { QteButton.StickRight, QteButton.South }
        };
        public static BattleActionOption[] CreateStatusPrototypes() => new[]
        {
            new BattleActionOption { Name = "Ember Volley", Description = "Three separately rolled hits. Each successful burn roll adds two remaining ticks.", Category = BattleActionCategory.Magic, Pattern = BattleTargetPattern.Multiple, Reach = 4, MaxTargets = 3, Damage = 5, DamageType = DamageKind.Magical, Element = CombatElement.Fire, Scaling = ScalingStat.Magic, StatScaling = .5f, InflictedStatus = CombatStatus.Burning, StatusChance = 50, StatusStacks = 2 },
            new BattleActionOption { Name = "Renew", Description = "Three start-of-turn healing ticks. Select yourself or another character.", Category = BattleActionCategory.Magic, Reach = 4, Effect = ActionEffect.Support, Damage = 0, InflictedStatus = CombatStatus.Regeneration, StatusStacks = 3 },
            new BattleActionOption { Name = "Ward", Description = "Grant a blue shield that absorbs 30 damage before health.", Category = BattleActionCategory.Magic, Reach = 4, Effect = ActionEffect.Support, Damage = 0, BarrierGranted = 30 },
            new BattleActionOption { Name = "Paralyzing Spark", Description = "Prevents dodge, parry and Block through the target's next turn. Does not skip their turn.", Category = BattleActionCategory.Magic, Reach = 4, Damage = 0, DamageType = DamageKind.Magical, Element = CombatElement.Lightning, InflictedStatus = CombatStatus.Paralyzed },
            new BattleActionOption { Name = "Push", Description = "Push one cell forward on level, clear ground. Your Strength must exceed the target's Strength or object's push resistance. Rivers, falls and traps are not supported yet.", Category = BattleActionCategory.Attack, Effect = ActionEffect.Push, Damage = 0 }
        };
        public List<BattleActionOption> Options = CreateDefaultOptions();
        private static List<BattleActionOption> CreateDefaultOptions()
        {
            var options = new List<BattleActionOption>
        {
            new BattleActionOption { Name = "Strike", Description = "Prototype adjacent attack." },
            new BattleActionOption { Name = "Piercing Attack", Description = "Prototype line targeting.", Pattern = BattleTargetPattern.Piercing, Reach = 4, Damage = 18 },
            new BattleActionOption { Name = "Area Skill", Description = "Prototype area targeting.", Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Area, Reach = 4, Damage = 18 },
            new BattleActionOption { Name = "Multi-target Skill", Description = "Prototype independent target selection.", Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Multiple, Reach = 4, Damage = 12 },
            CreatePrototypeSignature(),
            CreatePrototypeSequence(),
            CreatePrototypeMash(),
            CreatePrototypeHold(),
            CreatePrototypeStaged(),
            CreatePrototypeDual(),
            new BattleActionOption { Name = "Blast Flask", Description = "Prototype offensive consumable.", Category = BattleActionCategory.Item, Pattern = BattleTargetPattern.Area, Reach = 3, Damage = 20, Quantity = 3 }
        };
            options.AddRange(CreateStatusPrototypes()); return options;
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
            else if (action.Pattern == BattleTargetPattern.Area)
            {
                for (int x = -action.Radius; x <= action.Radius; x++) for (int y = -action.Radius; y <= action.Radius; y++)
                {
                    var p = target + new Vector2Int(x, y);
                    if (board.Contains(p) && Clear(board, target, p, targetableObstacle?.Invoke(p) ?? false) && (sight == null || sight(target, p))) result.Add(p);
                }
            }
            else result.Add(target);
            return result;
        }
    }
}
