using UnityEngine;

/// <summary>
/// A single tutorial step. Create via Assets > Create > Tutorial > Tutorial Step.
/// </summary>
[CreateAssetMenu(fileName = "TutorialStep_", menuName = "Tutorial/Tutorial Step")]
public class TutorialStep : ScriptableObject
{
    [Header("Objective Panel")]
    [Tooltip("Short text shown in the objective list, e.g. \"Press X to Shockwave\"")]
    public string objectiveText = "Complete the objective";

    [Tooltip("Optional sub-text shown beneath the main label (tip, hint, etc.)")]
    public string hintText = "";

    [Header("UI Highlights")]
    [Tooltip("Any number of UI elements to highlight while this step is active. " +
             "Leave empty for steps with no highlight (e.g. a timer wait step).")]
    public TutorialHighlightTarget[] highlightTargets = new TutorialHighlightTarget[0];

    // ── Backwards-compat helper used by TutorialHooks ──────────────────
    // Returns true if any highlight target is the ability slot at the given index.
    // Slot order: 0=Dash, 1=Fly, 2=Shockwave, 3=Ranged
    public bool HighlightsAbilitySlot(int slotIndex)
    {
        // Ability slots are highlighted via TutorialHighlightTarget with
        // elementName = "AbilitySlot_0" … "AbilitySlot_3" (set by TutorialHighlighter).
        // Also support the legacy direct-index convention used in TutorialHooks.
        string expected = $"AbilitySlot_{slotIndex}";
        foreach (var t in highlightTargets)
            if (t != null && t.elementName == expected) return true;
        return false;
    }

    [Header("Completion")]
    [Tooltip("How this step is completed. Event-based steps complete via TutorialManager.CompleteCurrentStep().")]
    public CompletionMode completionMode = CompletionMode.Event;

    [Tooltip("Required key press (only used when completionMode = KeyPress).")]
    public KeyCode keyToPress = KeyCode.None;

    [Tooltip("Seconds to wait before auto-completing (only used when completionMode = Timer).")]
    public float timerDuration = 3f;

    [Tooltip("If true, only a TutorialTriggerZone can complete this step — ability events " +
             "in TutorialHooks are ignored even if this step highlights an ability slot. " +
             "Use this when an ability is highlighted as a hint (e.g. 'use dash to reach here') " +
             "but the real completion condition is reaching a physical location.")]
    public bool requiresTriggerZone = false;
}

public enum CompletionMode
{
    /// Completed by external code calling TutorialManager.CompleteCurrentStep()
    Event,
    /// Completed automatically when the player presses a specific key
    KeyPress,
    /// Completed automatically after a set number of seconds
    Timer,
}