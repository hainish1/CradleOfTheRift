using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Drives the Controls settings page.
/// 
/// Each row has a Button whose name follows the convention "Bind_<ActionName>".
/// Clicking a button starts an interactive rebind via InputActionRebindingExtensions.
/// Overrides are stored in SettingsData.bindingOverrides (a string→string dictionary
/// keyed by InputBinding.id) and are applied/removed through the Input System's
/// ApplyBindingOverride / RemoveBindingOverride APIs.
/// 
/// Assumptions:
///   • You have a single InputActionAsset referenced as a [SerializeField] on the
///     owning MonoBehaviour, or retrieved via Resources/Addressables.
///   • Action names in the asset exactly match the string keys used here
///     (e.g. "MoveUp", "Jump").  Composite part bindings (WASD) use the
///     compositePartName to find the correct binding index.
/// </summary>
public class ControlsPageController
{
    // ── Data passed in from the menu ─────────────────────────────────────────
    private readonly SettingsService service;

    // Set by SettingsMenuController before Initialize() is called.
    public InputActionAsset InputActions { get; set; }

    // ── UI references ────────────────────────────────────────────────────────
    private VisualElement pageRoot;
    private VisualElement rebindModal;
    private Label         rebindModalTitle;
    private Label         rebindModalBody;
    private Button        rebindCancelButton;

    // ── Rebind state ─────────────────────────────────────────────────────────
    private InputActionRebindingExtensions.RebindingOperation activeRebind;

    // Maps each bind-button name suffix → (actionName, compositePartName)
    // compositePartName is null for simple (non-composite) bindings.
    private static readonly List<(string buttonSuffix, string actionName, string compositePart)> ActionMap
        = new List<(string, string, string)>
    {
        // Movement – typical composite: one action "Move" with Up/Down/Left/Right parts.
        // Adjust actionName to match your asset exactly.
        ("MoveUp",    "Move", "up"),
        ("MoveDown",  "Move", "down"),
        ("MoveLeft",  "Move", "left"),
        ("MoveRight", "Move", "right"),

        // Simple (non-composite) actions
        ("Jump",      "Jump",     null),
        ("Interact",  "Interact", null),
        ("Attack",    "Attack",   null),
        ("Dodge",     "Dodge",    null),
        ("Pause",     "Pause",    null),
        ("Sprint",    "Sprint",   null),
    };

    // ── Constructor ──────────────────────────────────────────────────────────
    public ControlsPageController(SettingsService service)
    {
        this.service = service;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────
    public void Initialize(VisualElement root)
    {
        pageRoot = root;

        // Modal elements
        rebindModal       = root.Q<VisualElement>("RebindModal");
        rebindModalTitle  = root.Q<Label>("RebindModalTitle");
        rebindModalBody   = root.Q<Label>("RebindModalBody");
        rebindCancelButton = root.Q<Button>("RebindCancelButton");

        rebindCancelButton?.RegisterCallback<ClickEvent>(_ => CancelRebind());

        // Wire every bind button
        foreach (var (suffix, _, _) in ActionMap)
        {
            string capture = suffix;
            var btn = root.Q<Button>($"Bind_{capture}");
            btn?.RegisterCallback<ClickEvent>(_ => OnBindButtonClicked(capture));
        }
    }

    public void Refresh(SettingsData data)
    {
        if (InputActions == null) return;

        // Re-apply all saved overrides to the asset first
        ApplyAllOverrides(data);

        // Then update every button label to show the current binding
        foreach (var (suffix, actionName, compositePart) in ActionMap)
        {
            var btn = pageRoot?.Q<Button>($"Bind_{suffix}");
            if (btn == null) continue;
            btn.text = GetDisplayString(actionName, compositePart);
        }
    }

    // ── Private: rebind flow ─────────────────────────────────────────────────

    private void OnBindButtonClicked(string suffix)
    {
        if (InputActions == null)
        {
            Debug.LogWarning("ControlsPageController: InputActions is null. " +
                             "Assign the InputActionAsset before opening the Controls page.");
            return;
        }

        // Find action + binding index for this suffix
        var entry = ActionMap.Find(e => e.buttonSuffix == suffix);
        var action = InputActions.FindAction(entry.actionName, throwIfNotFound: false);
        if (action == null)
        {
            Debug.LogWarning($"ControlsPageController: Action '{entry.actionName}' not found in asset.");
            return;
        }

        int bindingIndex = FindBindingIndex(action, entry.compositePart);
        if (bindingIndex < 0)
        {
            Debug.LogWarning($"ControlsPageController: No binding found for '{entry.actionName}' / '{entry.compositePart}'.");
            return;
        }

        ShowRebindModal(entry.actionName, entry.compositePart);
        BeginRebind(action, bindingIndex, suffix);
    }

    private void BeginRebind(InputAction action, int bindingIndex, string suffix)
    {
        // Disable the action so it doesn't fire while we listen for a new key
        action.Disable();

        activeRebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                action.Enable();
                activeRebind?.Dispose();
                activeRebind = null;

                // Persist the override
                var bindingId = action.bindings[bindingIndex].id.ToString();
                var overridePath = action.bindings[bindingIndex].effectivePath;
                service.Current.SetBindingOverride(bindingId, overridePath);
                service.Save();

                HideRebindModal();
                Refresh(service.Current);
            })
            .OnCancel(op =>
            {
                action.Enable();
                activeRebind?.Dispose();
                activeRebind = null;
                HideRebindModal();
            })
            .Start();
    }

    private void CancelRebind()
    {
        if (activeRebind != null)
        {
            activeRebind.Cancel(); // triggers OnCancel above
        }
        else
        {
            HideRebindModal();
        }
    }

    // ── Private: helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the index of the binding to rebind.
    /// For composite parts (e.g. WASD), finds the child binding whose
    /// compositePart name matches. For simple actions, returns the first
    /// non-composite binding.
    /// </summary>
    private static int FindBindingIndex(InputAction action, string compositePart)
    {
        var bindings = action.bindings;
        if (compositePart == null)
        {
            // First binding that is NOT a composite group header
            for (int i = 0; i < bindings.Count; i++)
                if (!bindings[i].isComposite)
                    return i;
        }
        else
        {
            for (int i = 0; i < bindings.Count; i++)
                if (bindings[i].isPartOfComposite &&
                    bindings[i].name.Equals(compositePart, System.StringComparison.OrdinalIgnoreCase))
                    return i;
        }
        return -1;
    }

    /// <summary>
    /// Returns a human-readable display string for the effective binding.
    /// </summary>
    private string GetDisplayString(string actionName, string compositePart)
    {
        var action = InputActions.FindAction(actionName, throwIfNotFound: false);
        if (action == null) return "—";

        int idx = FindBindingIndex(action, compositePart);
        if (idx < 0) return "—";

        return action.GetBindingDisplayString(idx,
            InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
    }

    /// <summary>
    /// Applies saved binding overrides from SettingsData back onto the live asset.
    /// Called on Refresh so that load-from-disk is reflected in the Input System.
    /// </summary>
    private void ApplyAllOverrides(SettingsData data)
    {
        if (data.bindingOverrides == null) return;

        foreach (var map in InputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    string id = action.bindings[i].id.ToString();
                    if (data.bindingOverrides.TryGetValue(id, out string overridePath))
                        action.ApplyBindingOverride(i, overridePath);
                }
            }
        }
    }

    private void ShowRebindModal(string actionName, string compositePart)
    {
        if (rebindModal == null) return;
        string label = compositePart != null ? $"{actionName} ({compositePart})" : actionName;
        rebindModalTitle.text = $"Rebinding: {label}";
        rebindModalBody.text  = "Press any key...";
        rebindModal.style.display = DisplayStyle.Flex;
    }

    private void HideRebindModal()
    {
        if (rebindModal != null)
            rebindModal.style.display = DisplayStyle.None;
    }
}