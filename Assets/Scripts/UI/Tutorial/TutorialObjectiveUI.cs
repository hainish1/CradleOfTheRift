using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the tutorial objective panel.
/// Attach to the same GameObject as a UIDocument that has TutorialObjectives.uxml.
///
/// The panel is built entirely in code from the TutorialStep list,
/// so adding/removing steps in TutorialManager requires no UXML edits.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class TutorialObjectiveUI : MonoBehaviour
{
    // USS class names — keep in sync with TutorialObjectives.uss
    private const string CSS_ROW          = "objective-row";
    private const string CSS_ICON         = "objective-icon";
    private const string CSS_ICON_PENDING = "objective-icon--pending";
    private const string CSS_ICON_ACTIVE  = "objective-icon--active";
    private const string CSS_ICON_DONE    = "objective-icon--done";
    private const string CSS_LABEL        = "objective-label";
    private const string CSS_LABEL_ACTIVE = "objective-label--active";
    private const string CSS_LABEL_DONE   = "objective-label--done";
    private const string CSS_HINT         = "objective-hint";
    private const string CSS_ROW_ACTIVE   = "objective-row--active";
    private const string CSS_ROW_DONE     = "objective-row--done";

    private UIDocument   _doc;
    private VisualElement _listContainer;

    // Parallel list of row data kept for quick update
    private readonly List<ObjectiveRow> _rows = new();

    private struct ObjectiveRow
    {
        public VisualElement root;
        public Label icon;
        public Label         label;
        public Label         hint;
    }

    // ------------------------------------------------------------------ //
    //  Unity                                                               //
    // ------------------------------------------------------------------ //

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;
        _listContainer = root.Q<VisualElement>("ObjectiveList");

        if (_listContainer == null)
            Debug.LogError("[TutorialObjectiveUI] Could not find 'ObjectiveList' in UXML.");
    }

    // ------------------------------------------------------------------ //
    //  Public API (called by TutorialManager)                             //
    // ------------------------------------------------------------------ //

    /// <summary>Build one row per step. Call once at the start of the tutorial.</summary>
    public void Initialise(List<TutorialStep> steps)
    {
        if (_listContainer == null) return;
        _listContainer.Clear();
        _rows.Clear();

        foreach (var step in steps)
        {
            var row = BuildRow(step);
            _listContainer.Add(row.root);
            _rows.Add(row);
        }
    }

    /// <summary>Mark a step as the currently active one.</summary>
    public void SetActiveStep(int index)
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            bool isActive = (i == index);
            bool isDone   = IsDone(r);

            r.root.EnableInClassList(CSS_ROW_ACTIVE, isActive);

            if (!isDone)
            {
                r.icon.EnableInClassList(CSS_ICON_PENDING, !isActive);
                r.icon.EnableInClassList(CSS_ICON_ACTIVE,  isActive);
                r.label.EnableInClassList(CSS_LABEL_ACTIVE, isActive);

                // Show hint only on the active step
                if (r.hint != null)
                    r.hint.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }

    /// <summary>Visually tick off a completed step.</summary>
    public void MarkStepComplete(int index)
    {
        if (index < 0 || index >= _rows.Count) return;
        var r = _rows[index];

        r.root.EnableInClassList(CSS_ROW_ACTIVE, false);
        r.root.EnableInClassList(CSS_ROW_DONE,   true);

        r.icon.EnableInClassList(CSS_ICON_ACTIVE,  false);
        r.icon.EnableInClassList(CSS_ICON_PENDING, false);
        r.icon.EnableInClassList(CSS_ICON_DONE,    true);
        r.icon.text = "✓";

        r.label.EnableInClassList(CSS_LABEL_ACTIVE, false);
        r.label.EnableInClassList(CSS_LABEL_DONE,   true);

        if (r.hint != null)
            r.hint.style.display = DisplayStyle.None;
    }

    /// <summary>Called when the whole tutorial finishes.</summary>
    public void MarkAllComplete()
    {
        for (int i = 0; i < _rows.Count; i++)
            MarkStepComplete(i);
    }

    // ------------------------------------------------------------------ //
    //  Row builder                                                         //
    // ------------------------------------------------------------------ //

    private ObjectiveRow BuildRow(TutorialStep step)
    {
        // --- Row container ---
        var root = new VisualElement();
        root.AddToClassList(CSS_ROW);

        // --- Status icon (bullet / checkmark) ---
        var icon = new Label("○");
        icon.AddToClassList(CSS_ICON);
        icon.AddToClassList(CSS_ICON_PENDING);
        root.Add(icon);

        // --- Text column ---
        var textCol = new VisualElement();
        textCol.style.flexDirection = FlexDirection.Column;
        textCol.style.flexGrow = 1;
        root.Add(textCol);

        var label = new Label(step.objectiveText);
        label.AddToClassList(CSS_LABEL);
        textCol.Add(label);

        Label hint = null;
        if (!string.IsNullOrEmpty(step.hintText))
        {
            hint = new Label(step.hintText);
            hint.AddToClassList(CSS_HINT);
            hint.style.display = DisplayStyle.None; // hidden until step is active
            textCol.Add(hint);
        }

        return new ObjectiveRow { root = root, icon = icon, label = label, hint = hint };
    }

    private bool IsDone(ObjectiveRow r) => r.root.ClassListContains(CSS_ROW_DONE);
}
