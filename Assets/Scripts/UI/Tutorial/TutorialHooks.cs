using UnityEngine;

/// <summary>
/// Bridges existing gameplay static events into TutorialSceneManager.CompleteCurrentStep().
/// Attach ONLY to a GameObject in the tutorial scene.
///
/// Zero changes required to PlayerShockwave, PlayerMovement, PlayerShooter, etc.
/// Completion is only forwarded when the active step actually expects that action
/// (i.e. it highlights the matching ability slot), so ordering is always respected.
/// </summary>
public class TutorialHooks : MonoBehaviour
{
    // Guard flags so each action only fires once per step lifetime
    private bool _dashFired;
    private bool _shockwaveFired;
    private bool _rangedFired;

    // ── Slot index reference ─────────────────────────────────────────────
    // Must match the order abilities are created in AbilityUIController.Start()
    private const int SLOT_DASH      = 0;
    private const int SLOT_FLY       = 1;
    private const int SLOT_SHOCKWAVE = 2;
    private const int SLOT_RANGED    = 3;

    // ────────────────────────────────────────────────────────────────────
    //  Unity
    // ────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        PlayerShockwaveController.OnShockwaveUsed  += OnShockwaveUsed;
        PlayerMovement.OnDashChargeSpent += OnDashUsed;
        PlayerShooter.OnFireChargeSpent  += OnRangedUsed;

        // Subscribe to TutorialSceneManager step events so we can reset guard flags
        if (TutorialSceneManager.Instance != null)
            TutorialSceneManager.Instance.OnStepStarted += OnStepStarted;
    }

    void OnDisable()
    {
        PlayerShockwaveController.OnShockwaveUsed  -= OnShockwaveUsed;
        PlayerMovement.OnDashChargeSpent -= OnDashUsed;
        PlayerShooter.OnFireChargeSpent  -= OnRangedUsed;

        if (TutorialSceneManager.Instance != null)
            TutorialSceneManager.Instance.OnStepStarted -= OnStepStarted;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Step change — reset guard flags
    // ────────────────────────────────────────────────────────────────────

    private void OnStepStarted(TutorialStep _)
    {
        _dashFired      = false;
        _shockwaveFired = false;
        _rangedFired    = false;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Gameplay event handlers
    // ────────────────────────────────────────────────────────────────────

    private void OnShockwaveUsed()
    {
        if (_shockwaveFired) return;
        if (!StepWantsSlot(SLOT_SHOCKWAVE)) return;
        _shockwaveFired = true;
        TutorialSceneManager.Instance.CompleteCurrentStep();
    }

    private void OnDashUsed(int current, int max)
    {
        if (_dashFired) return;
        if (!StepWantsSlot(SLOT_DASH)) return;
        _dashFired = true;
        TutorialSceneManager.Instance.CompleteCurrentStep();
    }

    private void OnRangedUsed(int current, int max)
    {
        if (_rangedFired) return;
        if (!StepWantsSlot(SLOT_RANGED)) return;
        _rangedFired = true;
        TutorialSceneManager.Instance.CompleteCurrentStep();
    }

    // ────────────────────────────────────────────────────────────────────
    //  Helper
    // ────────────────────────────────────────────────────────────────────

    /// Returns true only if the current step explicitly highlights the given ability slot
    /// AND does not require a trigger zone for completion.
    private static bool StepWantsSlot(int slotIndex)
    {
        var step = TutorialSceneManager.Instance?.CurrentStep;
        if (step == null) return false;
        if (step.requiresTriggerZone) return false;
        return step.HighlightsAbilitySlot(slotIndex);
    }
}

/*
 * ══════════════════════════════════════════════════════════════════════════
 * SETUP GUIDE
 * ══════════════════════════════════════════════════════════════════════════
 *
 * ABILITY SLOT HIGHLIGHT TARGET NAMING CONVENTION
 * ─────────────────────────────────────────────────
 * For ability diamond highlights, create a TutorialHighlightTarget SO with:
 *   uiDocumentObjectName → name of the GameObject with AbilityUIController
 *                          (e.g. "PlayerUI")
 *   elementName          → "AbilitySlot_2"   (for Shockwave; index matches SLOT_ consts)
 *   style                → GoldPulse
 *
 * TutorialHooks.StepWantsSlot() matches on "AbilitySlot_{index}" so the
 * name must follow this convention exactly.
 *
 * FOR NON-ABILITY UI  (Gold, XP, Chest, etc.)
 * ─────────────────────────────────────────────
 * Create a TutorialHighlightTarget SO with:
 *   uiDocumentObjectName → GameObject name of the UIDocument (e.g. "GoldUI")
 *   elementName          → UXML element #name   (e.g. "GoldContainer")
 *   style                → FlashHighlight  / GoldPulse / ScalePulse
 *
 * Element name reference from the uploaded files:
 *   Gold:        GoldContainer, GoldIcon, GoldLabel
 *   XP:          XPContainer, XPBar, LevelLabel, XPRow
 *   Chest:       ChestPrompt, ChestIcon, ChestKeyLabel
 *   AbilityBar:  see AbilitySlot_0…3 convention above
 *
 * SCENE GAMEOBJECTS NEEDED
 * ─────────────────────────
 *   "TutorialSceneManager"
 *     ├── TutorialSceneManager.cs      (steps[] → drag TutorialStep SOs in order)
 *     ├── TutorialHooks.cs
 *
 *   "TutorialObjectivesUI"
 *     ├── UIDocument              (Source: TutorialObjectives.uxml, Sort Order: 10)
 *     └── TutorialObjectiveUI.cs
 *
 *   "TutorialHighlighter"
 *     ├── TutorialHighlighter.cs  (abilityUIController, overrideStyleSheet)
 *     └── TutorialPulseDriver.cs  (halfPeriod: 0.75)
 *
 * ══════════════════════════════════════════════════════════════════════════
 */