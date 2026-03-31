using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives CSS ping-pong pulse animations for any number of elements simultaneously.
///
/// Unity UI Toolkit has no looping keyframe animations, so we simulate them by
/// toggling a modifier CSS class on a timer. The USS transition-duration handles
/// the smooth interpolation between the two states.
///
/// Each element gets its own coroutine so multiple highlights can pulse
/// independently and be stopped individually.
/// </summary>
public class TutorialPulseDriver : MonoBehaviour
{
    // The modifier class that represents the "bright" phase of the pulse.
    // Each HighlightStyle has its own bright-phase class so colors don't clash.
    private static readonly Dictionary<string, string> PulsePhaseClass = new()
    {
        { "tutorial-highlight--gold-pulse",  "tutorial-highlight--gold-bright"  },
        { "tutorial-highlight--blue-pulse",  "tutorial-highlight--blue-bright"  },
        { "tutorial-highlight--scale-pulse", "tutorial-highlight--scale-big"    },
    };

    [Tooltip("Half-period of the pulse in seconds. Match this to the USS transition-duration.")]
    [SerializeField] private float halfPeriod = 0.75f;

    // element → its running coroutine
    private readonly Dictionary<VisualElement, Coroutine> _running = new();

    // ────────────────────────────────────────────────────────────────────
    //  Public API
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Start pulsing <paramref name="element"/>. Detects which pulse class is
    /// already applied and drives the matching bright-phase class.
    /// </summary>
    public void StartPulse(VisualElement element)
    {
        if (element == null) return;
        StopPulse(element); // stop any existing pulse first

        // Find which pulse modifier class is on this element
        string baseClass  = DetectPulseClass(element);
        string phaseClass = baseClass != null && PulsePhaseClass.TryGetValue(baseClass, out var p)
            ? p : "tutorial-highlight--gold-bright"; // fallback

        var co = StartCoroutine(PulseLoop(element, phaseClass));
        _running[element] = co;
    }

    /// <summary>Stop pulsing a specific element and remove the bright-phase class.</summary>
    public void StopPulse(VisualElement element)
    {
        if (element == null) return;
        if (_running.TryGetValue(element, out var co))
        {
            if (co != null) StopCoroutine(co);
            _running.Remove(element);
        }

        // Remove all possible bright-phase classes
        foreach (var phase in PulsePhaseClass.Values)
            element.RemoveFromClassList(phase);
    }

    /// <summary>Stop all active pulses.</summary>
    public void StopAllPulses()
    {
        // Copy keys to avoid modifying dict while iterating
        var keys = new List<VisualElement>(_running.Keys);
        foreach (var el in keys)
            StopPulse(el);
    }

    // ────────────────────────────────────────────────────────────────────
    //  Internal
    // ────────────────────────────────────────────────────────────────────

    private IEnumerator PulseLoop(VisualElement element, string phaseClass)
    {
        bool bright = false;
        while (true)
        {
            bright = !bright;
            if (bright) element.AddToClassList(phaseClass);
            else        element.RemoveFromClassList(phaseClass);
            yield return new WaitForSeconds(halfPeriod);
        }
    }

    private static string DetectPulseClass(VisualElement el)
    {
        foreach (var cls in PulsePhaseClass.Keys)
            if (el.ClassListContains(cls)) return cls;
        return null;
    }

    void OnDestroy() => StopAllPulses();
}
