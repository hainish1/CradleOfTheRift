using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Drives the Controls settings page.
///
/// Only shows Keyboard & Mouse bindings from the Player action map.
/// Rows are generated dynamically from the asset — no actions are hardcoded.
/// Composite actions (e.g. WASD Move) are expanded into one row per part.
///
/// To exclude an action entirely, add its name to ExcludedActions.
/// To exclude a specific binding within an action, add its path to ExcludedBindingPaths.
/// </summary>
public class ControlsPageController
{
    // ── Filtering ─────────────────────────────────────────────────────────────

    // Only bindings belonging to this control scheme will be shown.
    private const string TargetScheme  = "Keyboard&Mouse";

    // Action maps to show, in the order they appear on screen.
    private static readonly string[] ShownMapNames = { "Player", "UI" };

    // Actions to hide entirely. Add action names here as needed.
    private static readonly HashSet<string> ExcludedActions = new HashSet<string>
    {
        // Player map
        "Look",
        "Previous",
        "Next",

        // UI map
        "Navigate",
        "Submit",
        "Cancel",
        "Point",
        "Click",
        "RightClick",
        "MiddleClick",
        "ScrollWheel",
        "TrackedDevicePosition",
        "TrackedDeviceOrientation",
    };

    // Specific binding paths to hide within an action. Add paths here as needed.
    // Example: "<Gamepad>/leftStick" — though those are already filtered by scheme.
    // Useful for hiding individual keyboard/mouse bindings you don't want exposed.
    private static readonly HashSet<string> ExcludedBindingPaths = new HashSet<string>
    {
        "<Keyboard>/upArrow",
        "<Keyboard>/downArrow",
        "<Keyboard>/leftArrow",
        "<Keyboard>/rightArrow",

        "<Keyboard>/enter",
        "<Keyboard>/leftCtrl",
    };

    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly SettingsService service;
    public InputActionAsset InputActions { get; set; }

    // ── UI ───────────────────────────────────────────────────────────────────
    private VisualElement pageRoot;
    private ScrollView    scrollView;
    private VisualElement rebindModal;
    private Label         rebindModalTitle;
    private Label         rebindModalBody;
    private Button        rebindCancelButton;

    // ── Row cache ────────────────────────────────────────────────────────────
    private readonly List<RowEntry> rows = new List<RowEntry>();

    private struct RowEntry
    {
        public Button      button;
        public InputAction action;
        public int         bindingIndex;
        public string      displayLabel;
    }

    // ── Rebind state ─────────────────────────────────────────────────────────
    private InputActionRebindingExtensions.RebindingOperation activeRebind;

    // ── Constructor ──────────────────────────────────────────────────────────
    public ControlsPageController(SettingsService service)
    {
        this.service = service;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public void Initialize(VisualElement root)
    {
        pageRoot = root;

        scrollView         = root.Q<ScrollView>("ControlsScrollView");
        rebindModal        = root.Q<VisualElement>("RebindModal");
        rebindModalTitle   = root.Q<Label>("RebindModalTitle");
        rebindModalBody    = root.Q<Label>("RebindModalBody");
        rebindCancelButton = root.Q<Button>("RebindCancelButton");

        rebindCancelButton?.RegisterCallback<ClickEvent>(_ => CancelRebind());
    }

    public void Refresh(SettingsData data)
    {
        if (InputActions == null)
        {
            Debug.LogWarning("ControlsPageController: InputActions is null. " +
                             "Assign the InputActionAsset in SettingsMenuController.");
            return;
        }

        ApplyAllOverrides(data);

        if (rows.Count == 0)
            BuildRows();

        RefreshButtonLabels();
    }

    // ── Row generation ───────────────────────────────────────────────────────

    private void BuildRows()
    {
        rows.Clear();
        scrollView.Clear();

        foreach (string mapName in ShownMapNames)
        {
            var map = InputActions.FindActionMap(mapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogWarning($"ControlsPageController: Action map '{mapName}' not found.");
                continue;
            }

            // Section header for this map
            var header = new Label(map.name);
            header.AddToClassList("controls-section-header");
            scrollView.Add(header);

            bool anyRowAdded = false;

            foreach (var action in map.actions)
            {
                if (ExcludedActions.Contains(action.name))
                    continue;

                var bindings = action.bindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];

                    if (binding.isComposite)
                        continue;

                    if (!binding.groups.Contains(TargetScheme))
                        continue;

                    if (ExcludedBindingPaths.Contains(binding.effectivePath))
                        continue;

                    string label = binding.isPartOfComposite
                        ? $"{action.name} \u2014 {CapitalizeFirst(binding.name)}"
                        : action.name;

                    int capturedIndex = i;

                    var button = new Button();
                    button.AddToClassList("button");
                    button.AddToClassList("controls-bind-button");
                    button.AddToClassList("unityButtonHover");
                    button.RegisterCallback<ClickEvent>(_ => OnBindButtonClicked(action, capturedIndex, button));

                    var row = new VisualElement();
                    row.AddToClassList("settings-row");
                    row.AddToClassList("controls-row");

                    var rowLabel = new Label(label);
                    rowLabel.AddToClassList("settings-label");

                    row.Add(rowLabel);
                    row.Add(button);
                    scrollView.Add(row);

                    rows.Add(new RowEntry
                    {
                        button       = button,
                        action       = action,
                        bindingIndex = capturedIndex,
                        displayLabel = label,
                    });

                    anyRowAdded = true;
                }
            }

            // If all actions in this map were excluded, remove the orphaned header
            if (!anyRowAdded)
                scrollView.Remove(header);
        }
    }

    // ── Rebind flow ──────────────────────────────────────────────────────────

    private void OnBindButtonClicked(InputAction action, int bindingIndex, Button button)
    {
        ShowRebindModal(action, bindingIndex);
        SetButtonListening(button, listening: true);
        BeginRebind(action, bindingIndex, button);
    }

    private void BeginRebind(InputAction action, int bindingIndex, Button button)
    {
        action.Disable();

        activeRebind = action
            .PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                action.Enable();
                activeRebind?.Dispose();
                activeRebind = null;

                string bindingId    = action.bindings[bindingIndex].id.ToString();
                string overridePath = action.bindings[bindingIndex].effectivePath;
                service.Current.SetBindingOverride(bindingId, overridePath);
                service.Save();

                SetButtonListening(button, listening: false);
                HideRebindModal();
                RefreshButtonLabels();
            })
            .OnCancel(op =>
            {
                action.Enable();
                activeRebind?.Dispose();
                activeRebind = null;

                SetButtonListening(button, listening: false);
                HideRebindModal();
            })
            .Start();
    }

    private void CancelRebind()
    {
        if (activeRebind != null)
            activeRebind.Cancel();
        else
            HideRebindModal();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ApplyAllOverrides(SettingsData data)
    {
        if (data.bindingOverrides == null) return;

        foreach (var map in InputActions.actionMaps)
            foreach (var action in map.actions)
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    string id = action.bindings[i].id.ToString();
                    if (data.bindingOverrides.TryGetValue(id, out string path))
                        action.ApplyBindingOverride(i, path);
                }
    }

    private void RefreshButtonLabels()
    {
        foreach (var row in rows)
            row.button.text = GetDisplayString(row.action, row.bindingIndex);
    }

    private static string GetDisplayString(InputAction action, int bindingIndex)
    {
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return "\u2014";

        return action.GetBindingDisplayString(
            bindingIndex,
            InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
    }

    private void ShowRebindModal(InputAction action, int bindingIndex)
    {
        if (rebindModal == null) return;

        var binding = action.bindings[bindingIndex];
        string label = binding.isPartOfComposite
            ? $"{action.name} \u2014 {CapitalizeFirst(binding.name)}"
            : action.name;

        rebindModalTitle.text     = $"Rebinding: {label}";
        rebindModalBody.text      = "Press any key...";
        rebindModal.style.display = DisplayStyle.Flex;
    }

    private void HideRebindModal()
    {
        if (rebindModal != null)
            rebindModal.style.display = DisplayStyle.None;
    }

    private void SetButtonListening(Button button, bool listening)
    {
        if (listening)
        {
            button.AddToClassList("controls-bind-button--listening");
            button.text = "...";
        }
        else
        {
            button.RemoveFromClassList("controls-bind-button--listening");
        }
    }

    private static string CapitalizeFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
}