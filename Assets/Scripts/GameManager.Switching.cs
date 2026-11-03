using UnityEngine;

public partial class GameManager
{
    [Header("Overworld character switching")]
    [SerializeField, Min(.1f)] private float characterSwitchDuration = .65f;
    public bool IsSwitchingCharacter { get; private set; }
    private PlayerMovement switchTarget;
    private Vector3 switchCameraStart;
    private float switchElapsed, previousTimeScale;
    private int switchCompletedFrame = -1;

    private void Update()
    {
        if (IsSwitchingCharacter) { if (Application.isFocused) AdvanceCharacterSwitch(Time.unscaledDeltaTime); return; }
        if (!Application.isFocused || WorldInputBlocked || !Input || (BattleManager.Instance && BattleManager.Instance.InBattle)) return;
        if (Input.PreviousCharacterPressed != Input.NextCharacterPressed)
        { if (Input.PreviousCharacterPressed) TryCycleCharacter(-1); else if (Input.NextCharacterPressed) TryCycleCharacter(1); }
    }
    public bool TryCycleCharacter(int direction)
    {
        if (direction == 0 || WorldInputBlocked || !PlayerRotation || !PossessedCharacter || (BattleManager.Instance && BattleManager.Instance.InBattle)) return false;
        var party = GetBattleParty(); if (party.Count < 2) return false;
        int current = party.FindIndex(m => m && m.transform == PossessedCharacter.transform);
        if (current < 0) return false;
        var next = party[(current + (direction > 0 ? 1 : -1) + party.Count) % party.Count];
        switchTarget = next ? next.GetComponent<PlayerMovement>() : null;
        if (!switchTarget || !switchTarget.gameObject.activeInHierarchy) return false;
        PossessedCharacter.ResetMotion(); switchElapsed = 0;
        switchCameraStart = PlayerRotation.transform.position;
        previousTimeScale = Time.timeScale; IsSwitchingCharacter = true; Time.timeScale = 0;
        return true;
    }
    public void AdvanceCharacterSwitch(float seconds)
    {
        if (!IsSwitchingCharacter) return;
        if (!switchTarget || !PlayerRotation) { CancelCharacterSwitch(); return; }
        switchElapsed += Mathf.Max(0, seconds);
        float fraction = Mathf.Clamp01(switchElapsed / Mathf.Max(.1f, characterSwitchDuration));
        PlayerRotation.BlendToCharacter(switchTarget.transform, switchCameraStart, fraction);
        if (fraction < 1) return;
        var target = switchTarget; switchTarget = null;
        Time.timeScale = previousTimeScale; IsSwitchingCharacter = false; switchCompletedFrame = Time.frameCount;
        PossessCharacter(target);
    }
    public void CancelCharacterSwitch()
    {
        if (!IsSwitchingCharacter) return;
        Time.timeScale = previousTimeScale; IsSwitchingCharacter = false; switchTarget = null; switchCompletedFrame = Time.frameCount;
        if (PlayerRotation && PossessedCharacter) PlayerRotation.SetFollowTarget(PossessedCharacter.transform);
    }
    private void OnDisable() => CancelCharacterSwitch();
    private void OnDestroy() { CancelCharacterSwitch(); if (Instance == this) Instance = null; }
}
