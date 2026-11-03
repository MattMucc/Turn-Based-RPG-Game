using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed partial class InSceneGridBattle
    {
        public sealed class ReserveMember
        {
            public BattleCharacterInfo Info;
            public GridUnit Unit;
            public float FootOffset;
        }
        private readonly List<ReserveMember> reserves = new();
        private readonly HashSet<BattleCharacterInfo> participants = new();
        public IReadOnlyList<ReserveMember> Reserves => reserves;
        public IReadOnlyCollection<BattleCharacterInfo> Participants => participants;
        public bool TotalDefeat { get; private set; }
        public bool ReserveTakeover { get; private set; }
        public event Action PartyChanged;
        public bool CanSwap => ActionReady && ChosenAction == null && reserves.Exists(r => r.Unit.Health > 0);

        public void AddReserve(BattleCharacterInfo info, float footOffset)
        {
            if (!info || reserves.Count >= 3 || reserves.Count + allyCount >= 6 || actors.Contains(info.transform) || reserves.Exists(r => r.Info == info)) return;
            var unit = new GridUnit(info.name, Vector2Int.zero); info.Configure(unit); info.RestoreBattleHealth(unit);
            reserves.Add(new ReserveMember { Info = info, Unit = unit, FootOffset = footOffset });
            info.gameObject.SetActive(false);
        }

        public bool SwapWithReserve(int index)
        {
            if (!CanSwap || index < 0 || index >= reserves.Count || reserves[index].Unit.Health <= 0) return false;
            if (!Active.HandOffTurnTo(reserves[index].Unit)) return false;
            ReplaceSlot(actorIndex, index);
            DefenseSettings = allyDefense[actorIndex]; selected = Active.Position;
            CancelAction(); FocusActor(actorIndex); Refresh();
            PartyChanged?.Invoke(); ActionResolved?.Invoke(); return true;
        }

        private void ReplaceSlot(int slot, int reserveIndex)
        {
            var incoming = reserves[reserveIndex];
            var outgoing = new ReserveMember { Info = Info(slot), Unit = turnOrder[slot], FootOffset = offsets[slot] };
            Vector2Int cell = outgoing.Unit.Position;
            Quaternion rotation = actors[slot].rotation;
            Grid.Board.Units.Remove(outgoing.Unit); actors[slot].gameObject.SetActive(false);
            incoming.Unit.Position = cell; turnOrder[slot] = incoming.Unit; actors[slot] = incoming.Info.transform; offsets[slot] = incoming.FootOffset;
            actors[slot].SetPositionAndRotation(Grid.Point(cell) + Vector3.up * incoming.FootOffset, rotation);
            actors[slot].gameObject.SetActive(true); Grid.Board.Units.Add(incoming.Unit);
            battleTargets[slot] = new BattleTarget(actors[slot], incoming.Unit, slot, true);
            var loadout = incoming.Info.GetComponent<BattleActionLoadout>();
            if (!loadout) loadout = incoming.Info.gameObject.AddComponent<BattleActionLoadout>();
            loadout.EnsureUniversalAttacks(); allyLoadouts[slot] = loadout;
            var defense = incoming.Info.GetComponent<DefensePrototypeSettings>();
            if (!defense) defense = incoming.Info.gameObject.AddComponent<DefensePrototypeSettings>();
            allyDefense[slot] = defense;
            reserves[reserveIndex] = outgoing; participants.Add(incoming.Info);
            hitDamage.Remove(slot); hitTime.Remove(slot); TargetFocus.Dismiss();
            initiativeOrder.SetInitiative(slot, incoming.Info.EffectiveInitiative);
        }

        private bool DeployReserves()
        {
            if (!reserves.Exists(r => r.Unit.Health > 0)) return false;
            // Reuse the fallen formation where possible, selecting a clear nearby cell if an enemy occupies it.
            for (int slot = 0; slot < allyCount; slot++)
            {
                int index = reserves.FindIndex(r => r.Unit.Health > 0);
                if (index < 0) break;
                Grid.Board.Units.Remove(turnOrder[slot]);
                Vector2Int origin = turnOrder[slot].Position, best = origin;
                int distance = int.MaxValue;
                for (int x = 0; x < Grid.Board.Width; x++) for (int y = 0; y < Grid.Board.Depth; y++)
                {
                    var cell = new Vector2Int(x, y);
                    int candidate = GridTargeting.Distance(origin, cell);
                    if (candidate < distance && Grid.Board.IsLanding(cell, reserves[index].Unit)) { best = cell; distance = candidate; }
                }
                if (distance == int.MaxValue) continue;
                turnOrder[slot].EndTurn(); turnOrder[slot].Position = best;
                ReplaceSlot(slot, index);
            }
            bool deployed = false;
            for (int i = 0; i < allyCount; i++) if (turnOrder[i].Health > 0) deployed = true;
            if (!deployed) return false;
            ReserveTakeover = true; PartyChanged?.Invoke();
            message = "The deployed team is down. Reserves have entered the battle.";
            return true;
        }

        public void StorePartyHealth()
        {
            for (int i = 0; i < allyCount; i++) Info(i).StoreHealth(turnOrder[i]);
            foreach (var reserve in reserves) reserve.Info.StoreHealth(reserve.Unit);
        }
    }
}
