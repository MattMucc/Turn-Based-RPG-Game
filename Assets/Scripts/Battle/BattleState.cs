using UnityEngine;

public enum BattleState
{
    None,
    BattleStart, // Initialize units, camera, UI
    DetermineNextActor, // Ask TurnOrderManager who goes next
    PlayerInput, // Waiting for player to pick an action
    EnemyThink, // AI selects it's action
    ActionExecution, // The chosen action plays out
    EndOfTurnProcess, // Tick statuses, passives, decays, etc.
    Victory,
    Defeat
}