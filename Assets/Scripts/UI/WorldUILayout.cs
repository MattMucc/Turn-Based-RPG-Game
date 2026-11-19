using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/World Prompts and Notices")]
public sealed class WorldUILayout:ScriptableObject {
 public WorldPromptView Merchant,Blacksmith,Checkpoint;
 public SaveIndicatorView SaveIndicator;
 public EncounterNoticeView EncounterNotice;
 public static WorldUILayout Default=>Resources.Load<WorldUILayout>("UI/World/World");
}
