using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Unified tutorial highlighter.
///
/// Key design decisions:
///   • DiamondAbilityElement.BorderColor is driven DIRECTLY in C# via a
///     coroutine — USS class-based --border-color is unreliable for custom
///     painted elements and is not used here.
///   • All other elements (ChestPrompt, Gold, XP, etc.) use USS class injection.
///   • All your UI lives in ONE UIDocument (PlayerUI), so uiDocumentObjectName
///     on every TutorialHighlightTarget should be that one GameObject's name.
/// </summary>
[RequireComponent(typeof(TutorialPulseDriver))]
public class TutorialHighlighter : MonoBehaviour
{
    // ── CSS class names ──────────────────────────────────────────────────
    private const string CLASS_HIGHLIGHT    = "tutorial-highlight";
    private const string CLASS_GOLD_PULSE   = "tutorial-highlight--gold-pulse";
    private const string CLASS_GOLD_STATIC  = "tutorial-highlight--gold-static";
    private const string CLASS_BLUE_PULSE   = "tutorial-highlight--blue-pulse";
    private const string CLASS_SCALE_PULSE  = "tutorial-highlight--scale-pulse";
    private const string CLASS_FLASH        = "tutorial-highlight--flash";

    private static readonly string[] ALL_MODIFIER_CLASSES =
    {
        CLASS_GOLD_PULSE, CLASS_GOLD_STATIC, CLASS_BLUE_PULSE,
        CLASS_SCALE_PULSE, CLASS_FLASH
    };

    // ── Diamond direct-drive colors ──────────────────────────────────────
    private static readonly Color DIAMOND_DIM     = new Color(1.00f, 0.76f, 0.16f, 1f); // gold
    private static readonly Color DIAMOND_BRIGHT  = new Color(1.00f, 0.94f, 0.51f, 1f); // bright gold
    private static readonly Color DIAMOND_DEFAULT = Color.white;

    // ── Inspector ────────────────────────────────────────────────────────
    [SerializeField] private AbilityUIController abilityUIController;

    [Tooltip("Drag TutorialOverride StyleSheet asset here. Injected at runtime only.")]
    [SerializeField] private StyleSheet overrideStyleSheet;

    // ── Private state ────────────────────────────────────────────────────
    private List<AbilityUIController.AbilitySlot> _abilitySlots;
    private TutorialPulseDriver _pulseDriver;

    private readonly Dictionary<string, UIDocument> _docsByGoName    = new();
    private readonly HashSet<VisualElement>          _injectedRoots   = new();
    private readonly List<VisualElement>             _activeHighlights = new();
    private readonly List<Coroutine>                 _diamondCoroutines = new();

    // ────────────────────────────────────────────────────────────────────
    //  Unity
    // ────────────────────────────────────────────────────────────────────

    void Start()
    {
        _pulseDriver = GetComponent<TutorialPulseDriver>();

        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            _docsByGoName[doc.gameObject.name] = doc;

        if (abilityUIController == null)
            abilityUIController = FindFirstObjectByType<AbilityUIController>();

        if (abilityUIController != null)
        {
            var field = typeof(AbilityUIController).GetField(
                "abilitySlots", BindingFlags.NonPublic | BindingFlags.Instance);
            _abilitySlots = field?.GetValue(abilityUIController)
                as List<AbilityUIController.AbilitySlot>;
        }

        if (_abilitySlots == null)
            Debug.LogWarning("[TutorialHighlighter] Could not find abilitySlots.");
    }

    void OnDestroy()
    {
        ClearAllHighlights();
        RemoveAllStyleSheets();
    }

    // ────────────────────────────────────────────────────────────────────
    //  Public API
    // ────────────────────────────────────────────────────────────────────

    public void ApplyHighlights(TutorialHighlightTarget[] targets)
    {
        ClearAllHighlights();
        if (targets == null || targets.Length == 0) return;
        foreach (var target in targets)
            if (target != null) ApplySingleHighlight(target);
    }

    public void ClearAllHighlights()
    {
        // Stop USS-driven pulses
        _pulseDriver?.StopAllPulses();

        // Remove CSS classes
        foreach (var el in _activeHighlights)
        {
            if (el == null) continue;
            el.RemoveFromClassList(CLASS_HIGHLIGHT);
            foreach (var mod in ALL_MODIFIER_CLASSES)
                el.RemoveFromClassList(mod);
        }
        _activeHighlights.Clear();

        // Stop diamond C# coroutines and restore border color
        foreach (var co in _diamondCoroutines)
            if (co != null) StopCoroutine(co);
        _diamondCoroutines.Clear();

        if (_abilitySlots != null)
            foreach (var slot in _abilitySlots)
                if (slot.diamond != null)
                    slot.diamond.BorderColor = DIAMOND_DEFAULT;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Diamond — C# direct drive (bypasses USS entirely)
    // ────────────────────────────────────────────────────────────────────

    public void HighlightAbilitySlot(int slotIndex)
    {
        if (_abilitySlots == null || slotIndex < 0 || slotIndex >= _abilitySlots.Count)
        {
            Debug.LogWarning($"[TutorialHighlighter] Slot index {slotIndex} out of range " +
                             $"(count: {_abilitySlots?.Count ?? 0}). " +
                             "Ensure AbilityUIController.Start() runs before the tutorial.");
            return;
        }

        var diamond = _abilitySlots[slotIndex].diamond;
        if (diamond == null)
        {
            Debug.LogWarning($"[TutorialHighlighter] Diamond is null on slot {slotIndex}.");
            return;
        }

        var co = StartCoroutine(DiamondPulseLoop(diamond));
        _diamondCoroutines.Add(co);
        Debug.Log($"[TutorialHighlighter] Gold pulse started on slot {slotIndex}");
    }

    private IEnumerator DiamondPulseLoop(DiamondAbilityElement diamond)
    {
        bool bright = false;
        while (true)
        {
            bright = !bright;
            diamond.BorderColor = bright ? DIAMOND_BRIGHT : DIAMOND_DIM;
            yield return new WaitForSeconds(0.75f);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    //  Generic USS-class highlight
    // ────────────────────────────────────────────────────────────────────

    private void ApplySingleHighlight(TutorialHighlightTarget target)
    {
        // Ability slots: C# direct drive, skip USS path entirely
        if (target.elementName.StartsWith("AbilitySlot_"))
        {
            if (int.TryParse(target.elementName.Replace("AbilitySlot_", ""), out int idx))
                HighlightAbilitySlot(idx);
            return;
        }

        // Everything else: find by name in the document and apply CSS classes
        var docRoot = FindDocumentRoot(target.uiDocumentObjectName);
        if (docRoot == null)
        {
            Debug.LogWarning($"[TutorialHighlighter] UIDocument GO '{target.uiDocumentObjectName}' not found. " +
                             "The uiDocumentObjectName field must match the exact GameObject name that has the UIDocument component.");
            return;
        }

        var element = docRoot.Q<VisualElement>(target.elementName);
        if (element == null)
        {
            Debug.LogWarning($"[TutorialHighlighter] Element name='{target.elementName}' not found " +
                             $"inside '{target.uiDocumentObjectName}'. Check the name= attribute in the UXML.");
            return;
        }

        // Inject the stylesheet at the document root level (not element level)
        InjectStyleSheetInto(docRoot);
        AddHighlightClasses(element, StyleToClass(target.style));

        if (!string.IsNullOrEmpty(target.secondaryElementName))
        {
            var secondary = docRoot.Q<VisualElement>(target.secondaryElementName);
            if (secondary != null)
                AddHighlightClasses(secondary, CLASS_GOLD_STATIC);
        }

        bool needsPulse = target.style == HighlightStyle.GoldPulse  ||
                          target.style == HighlightStyle.BluePulse   ||
                          target.style == HighlightStyle.ScalePulse;
        if (needsPulse)
            _pulseDriver?.StartPulse(element);
    }

    // ────────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────────

    private VisualElement FindDocumentRoot(string goName)
    {
        if (string.IsNullOrEmpty(goName)) return null;
        if (_docsByGoName.TryGetValue(goName, out var doc)) return doc.rootVisualElement;

        foreach (var d in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            if (d.gameObject.name == goName)
            {
                _docsByGoName[goName] = d;
                return d.rootVisualElement;
            }
        }
        return null;
    }

    private void AddHighlightClasses(VisualElement el, string modifierClass)
    {
        if (el == null) return;
        el.AddToClassList(CLASS_HIGHLIGHT);
        el.AddToClassList(modifierClass);
        _activeHighlights.Add(el);
    }

    private static string StyleToClass(HighlightStyle style) => style switch
    {
        HighlightStyle.GoldPulse      => CLASS_GOLD_PULSE,
        HighlightStyle.GoldStatic     => CLASS_GOLD_STATIC,
        HighlightStyle.BluePulse      => CLASS_BLUE_PULSE,
        HighlightStyle.ScalePulse     => CLASS_SCALE_PULSE,
        HighlightStyle.FlashHighlight => CLASS_FLASH,
        _                             => CLASS_GOLD_PULSE
    };

    private void InjectStyleSheetInto(VisualElement root)
    {
        if (overrideStyleSheet == null || root == null) return;
        if (_injectedRoots.Contains(root)) return;
        root.styleSheets.Add(overrideStyleSheet);
        _injectedRoots.Add(root);
        Debug.Log("[TutorialHighlighter] TutorialOverride.uss injected.");
    }

    private void RemoveAllStyleSheets()
    {
        if (overrideStyleSheet == null) return;
        foreach (var root in _injectedRoots)
            if (root != null && root.styleSheets.Contains(overrideStyleSheet))
                root.styleSheets.Remove(overrideStyleSheet);
        _injectedRoots.Clear();
    }
}