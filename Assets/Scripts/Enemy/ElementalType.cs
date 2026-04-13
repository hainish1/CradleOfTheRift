using System.Collections;
using UnityEngine;

public enum ElementalType
{
    None,
    Poison,
    Fire,
    Earth,
    Lightning
}

/// </summary>
[System.Serializable]
public class ElementalProfile
{
    public ElementalType type = ElementalType.None;

    [Header("Visuals")]
    [Tooltip("Model for this elemental variant")]
    public GameObject modelVariant;
    [Tooltip("Aura body VFX played on this enemy")]
    public GameObject bodyVFX;
    [Tooltip("VFX when this enemy attacks (slam, shoot)")]
    public GameObject attackVFX;
    [Tooltip("VFX on player when a DOT is applied")]
    public GameObject dotHitVFX;

    [Header("Stat Modifiers")]
    [Tooltip("Flat multiplier applied on top of base damage. 1 = no change")]
    public float damageMultiplier = 1f;
    [Tooltip("Flat multiplier applied on top of base move speed. 1 = no change")]
    public float speedMultiplier = 1f;
    [Tooltip("this is stronger. Spawner use this to weight rarity")]
    public bool isStrongerVariant;

    [Header("DOT settings(Poison and Fire)")]
    [Range(0f, 1f)]
    [Tooltip("chance an attack applies a DOT")]
    public float dotChance = 0.35f;
    [Tooltip("damage per DOT tick")]
    public float dotDamagePerTick = 0.5f;
    [Tooltip("time between DOT ticks")]
    public float dotTickInterval = 1f;
    [Tooltip("tsotal duration of the DOT effect")]
    public float dotDuration = 3f;

    public bool CanInflictDOT => type == ElementalType.Poison || type == ElementalType.Fire;
}


public static class ElementalEffects
{

    public static bool TryApplyDOT(MonoBehaviour parent, ElementalProfile profile, IDamageable target)
    {
        if (parent == null || profile == null || target == null) return false;
        if (!profile.CanInflictDOT) return false;
        if (target.IsDead) return false;
        if (Random.value > profile.dotChance) return false;

        parent.StartCoroutine(DOTRoutine(profile, target));
        return true;
    }

    private static IEnumerator DOTRoutine(ElementalProfile profile, IDamageable target)
    {
        float elapsed = 0f;
        float interval = Mathf.Max(0.05f, profile.dotTickInterval);

        while (elapsed < profile.dotDuration)
        {
            yield return new WaitForSeconds(interval);
            elapsed += interval;

            if (target == null || target.IsDead) yield break;
            target.TakeDamage(profile.dotDamagePerTick);
        }
    }
}
