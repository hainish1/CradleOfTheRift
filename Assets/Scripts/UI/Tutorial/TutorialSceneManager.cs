using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the tutorial. Place this GameObject ONLY in your tutorial scene.
///
/// Wire up:
///   steps[]      → drag your TutorialStep SOs in order
///   objectiveUI  → TutorialObjectiveUI component (auto-found if not set)
///   highlighter  → TutorialHighlighter component  (auto-found if not set)
///
/// To complete a step from gameplay code:
///   TutorialSceneManager.Instance?.CompleteCurrentStep();
/// </summary>
public class TutorialSceneManager : MonoBehaviour
{
    public static TutorialSceneManager Instance { get; private set; }

    [Header("Steps (in order)")]
    [SerializeField] private List<TutorialStep> steps = new();

    [Header("References (auto-found if blank)")]
    [SerializeField] private TutorialObjectiveUI objectiveUI;
    [SerializeField] private TutorialHighlighter  highlighter;
    private TutorialCompleter tutorialCompleter;

    public event Action<TutorialStep>       OnStepStarted;
    public event Action<TutorialStep, int>  OnStepCompleted;
    public event Action                     OnTutorialComplete;

    private int       _currentIndex    = -1;
    private bool      _stepActive      = false;
    private Coroutine _timerCoroutine  = null;

    public TutorialStep CurrentStep =>
        (_currentIndex >= 0 && _currentIndex < steps.Count) ? steps[_currentIndex] : null;

    public int  CurrentStepIndex => _currentIndex;
    public int  TotalSteps       => steps.Count;
    public bool IsComplete       => _currentIndex >= steps.Count;

    // ────────────────────────────────────────────────────────────────────
    //  Unity
    // ────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (objectiveUI == null) objectiveUI = FindFirstObjectByType<TutorialObjectiveUI>();
        if (highlighter == null) highlighter  = FindFirstObjectByType<TutorialHighlighter>();

        if (steps.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] No steps assigned.");
            return;
        }
        tutorialCompleter = GetComponent<TutorialCompleter>();

        objectiveUI?.Initialise(steps);
        AdvanceToNextStep();
    }

    void Update()
    {
        if (!_stepActive || CurrentStep == null) return;

        if (CurrentStep.completionMode == CompletionMode.KeyPress)
            if (Input.GetKeyDown(CurrentStep.keyToPress))
                CompleteCurrentStep();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Public API
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Complete the currently active step. Safe to call from any gameplay script.
    /// No-op if no step is active or TutorialManager doesn't exist.
    /// </summary>
    public void CompleteCurrentStep()
    {
        if (!_stepActive) return;
        FinishStep();
    }

    // ────────────────────────────────────────────────────────────────────
    //  Internal
    // ────────────────────────────────────────────────────────────────────

    private void AdvanceToNextStep()
    {
        _currentIndex++;

        if (_currentIndex >= steps.Count)
        {
            _stepActive = false;
            highlighter?.ClearAllHighlights();
            objectiveUI?.MarkAllComplete();
            OnTutorialComplete?.Invoke();
            Debug.Log("[TutorialManager] Tutorial complete!");
            // call the tutorial thing here
            if (ExtractionManager.Instance.IsExtractionCompleted())
            {
                tutorialCompleter.CompleteTutorial(); // go to the main scene
            }
            return;
        }

        StartStep(steps[_currentIndex]);
    }

    private void StartStep(TutorialStep step)
    {
        _stepActive = true;
        Debug.Log($"[TutorialManager] Step {_currentIndex}: {step.objectiveText}");

        objectiveUI?.SetActiveStep(_currentIndex);

        // Pass the entire targets array — highlighter handles every element type
        highlighter?.ApplyHighlights(step.highlightTargets);

        OnStepStarted?.Invoke(step);

        if (step.completionMode == CompletionMode.Timer)
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TimerCoroutine(step.timerDuration));
        }
    }

    private void FinishStep()
    {
        if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }

        _stepActive = false;
        var completed = CurrentStep;

        objectiveUI?.MarkStepComplete(_currentIndex);
        OnStepCompleted?.Invoke(completed, _currentIndex);

        StartCoroutine(DelayedAdvance(0.6f));
    }

    private IEnumerator TimerCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        CompleteCurrentStep();
    }

    private IEnumerator DelayedAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        AdvanceToNextStep();
    }
}
