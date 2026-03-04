using System;
using System.Collections.Generic;

[Serializable]
public class SettingsData
{
    // ── Audio ────────────────────────────────────────────────────────────────
    public int masterVolume  = 100;
    public int musicVolume   = 75;
    public int sfxVolume     = 100;
    public int ambientVolume = 50;

    // ── Controls ─────────────────────────────────────────────────────────────
    // Key   = InputBinding.id (GUID string)
    // Value = override path (e.g. "<Keyboard>/space")
    // Serialized as two parallel lists because Unity's JsonUtility cannot
    // serialize Dictionary<,> directly.
    public List<string> bindingOverrideKeys   = new List<string>();
    public List<string> bindingOverrideValues = new List<string>();

    // Runtime-only dictionary built from the parallel lists above.
    // Call BuildOverrideDictionary() after deserializing.
    [NonSerialized]
    public Dictionary<string, string> bindingOverrides;

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the runtime dictionary from the serialized parallel lists.
    /// Call this once after JsonUtility.FromJson.
    /// </summary>
    public void BuildOverrideDictionary()
    {
        bindingOverrides = new Dictionary<string, string>();
        int count = Math.Min(bindingOverrideKeys.Count, bindingOverrideValues.Count);
        for (int i = 0; i < count; i++)
            bindingOverrides[bindingOverrideKeys[i]] = bindingOverrideValues[i];
    }

    /// <summary>
    /// Adds or updates a binding override and keeps the serialized lists in sync.
    /// </summary>
    public void SetBindingOverride(string bindingId, string overridePath)
    {
        if (bindingOverrides == null) bindingOverrides = new Dictionary<string, string>();

        bindingOverrides[bindingId] = overridePath;

        // Keep parallel lists in sync for serialization
        int idx = bindingOverrideKeys.IndexOf(bindingId);
        if (idx >= 0)
        {
            bindingOverrideValues[idx] = overridePath;
        }
        else
        {
            bindingOverrideKeys.Add(bindingId);
            bindingOverrideValues.Add(overridePath);
        }
    }

    /// <summary>
    /// Removes all binding overrides (reset to defaults).
    /// </summary>
    public void ClearAllBindingOverrides()
    {
        bindingOverrides = new Dictionary<string, string>();
        bindingOverrideKeys.Clear();
        bindingOverrideValues.Clear();
    }

    // ── Equality ─────────────────────────────────────────────────────────────

    public bool Equals(SettingsData other)
    {
        if (other == null) return false;
        return masterVolume  == other.masterVolume  &&
               musicVolume   == other.musicVolume   &&
               sfxVolume     == other.sfxVolume     &&
               ambientVolume == other.ambientVolume;
        // Note: binding override equality intentionally omitted;
        // controls changes are saved immediately on rebind.
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(masterVolume, musicVolume, sfxVolume, ambientVolume);
    }
}
