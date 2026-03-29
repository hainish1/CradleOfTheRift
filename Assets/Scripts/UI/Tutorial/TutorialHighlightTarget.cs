using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Describes a single UI element that the tutorial can highlight.
/// Create via Assets > Create > Tutorial > Highlight Target.
///
/// Each TutorialStep holds an array of these, so one step can highlight
/// multiple elements simultaneously (e.g. the XP bar AND the level label).
/// </summary>
[CreateAssetMenu(fileName = "Highlight_", menuName = "Tutorial/Highlight Target")]
public class TutorialHighlightTarget : ScriptableObject
{
    [Header("Source")]
    [Tooltip("Which UIDocument holds this element? Match the GameObject name exactly.")]
    public string uiDocumentObjectName = "";

    [Tooltip("The #name of the VisualElement to highlight (e.g. 'GoldContainer', 'XPBar', 'ChestKeyLabel').")]
    public string elementName = "";

    [Header("Effect")]
    [Tooltip("What kind of highlight to apply.")]
    public HighlightStyle style = HighlightStyle.GoldPulse;

    [Tooltip("Optional: also highlight a parent or sibling element (e.g. add glow to the whole container while pulsing a child).")]
    public string secondaryElementName = "";
}

public enum HighlightStyle
{
    /// Pulses the element's border between white and gold (default, matches ability diamonds)
    GoldPulse,

    /// Adds a static gold border — no animation. Good for labels or containers.
    GoldStatic,

    /// Adds a blue/cyan pulse — better for XP bar or informational elements
    BluePulse,

    /// Scales the element up and down slightly (attention-grabbing for icons)
    ScalePulse,

    /// Adds a bright background flash then fades — good for gold counter or chest icon
    FlashHighlight,
}
