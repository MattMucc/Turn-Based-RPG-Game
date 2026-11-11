using Game.GridPrototype;
// Test-only examples exercise targeting and QTE infrastructure without populating playable kits.
static class DemoActionFixtures
{
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
            new BattleActionOption { Name = "Ember Volley", Description = "Hurl a volley of embers at selected targets.", Category = BattleActionCategory.Magic, Pattern = BattleTargetPattern.Multiple, Reach = 4, MaxTargets = 3, Damage = 5, DamageType = DamageKind.Magical, Element = CombatElement.Fire, Scaling = ScalingStat.Magic, StatScaling = .5f, InflictedStatus = CombatStatus.Burning, StatusChance = 50, StatusStacks = 2 },
            new BattleActionOption { Name = "Renew", Description = "Restore vitality over time.", Category = BattleActionCategory.Magic, Reach = 4, Effect = ActionEffect.Support, Damage = 0, InflictedStatus = CombatStatus.Regeneration, StatusStacks = 3 },
            new BattleActionOption { Name = "Ward", Description = "Conjure a protective shield.", Category = BattleActionCategory.Magic, Reach = 4, Effect = ActionEffect.Support, Damage = 0, BarrierGranted = 30 },
            new BattleActionOption { Name = "Paralyzing Spark", Description = "Disrupt the target with a crackling spark.", Category = BattleActionCategory.Magic, Reach = 4, Damage = 0, DamageType = DamageKind.Magical, Element = CombatElement.Lightning, InflictedStatus = CombatStatus.Paralyzed },
            new BattleActionOption { Name = "Push", Description = "Push one cell forward on level, clear ground. Your Strength must exceed the target's Strength or object's push resistance. Rivers, falls and traps are not supported yet.", Category = BattleActionCategory.Attack, Effect = ActionEffect.Push, Damage = 0 }
        };
    public static void Populate(BattleActionLoadout loadout)
    {
        loadout.PersonalOptions.AddRange(System.Array.FindAll(CreateStatusPrototypes(), x => x.Category == BattleActionCategory.Magic));
        loadout.PersonalOptions.Insert(2, new BattleActionOption { Name="Area Skill", Category=BattleActionCategory.Skill,Pattern=BattleTargetPattern.Area,Reach=4,Damage=18 });
        loadout.PersonalOptions.Insert(3, new BattleActionOption { Name="Multi-target Skill",Category=BattleActionCategory.Skill,Pattern=BattleTargetPattern.Multiple,Reach=4,Damage=12 });
        loadout.PersonalOptions.AddRange(new[]{DemoActionFixtures.CreatePrototypeSignature(),DemoActionFixtures.CreatePrototypeSequence(),DemoActionFixtures.CreatePrototypeMash(),DemoActionFixtures.CreatePrototypeHold(),DemoActionFixtures.CreatePrototypeStaged(),DemoActionFixtures.CreatePrototypeDual()});
        loadout.EquipmentChanged(false);
    }
}
