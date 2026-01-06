using System;
using System.Collections.Generic;
using UnityEngine;

public enum ElementType
{
    None,
    Fire,
    Poison,
    Lightning,
    Ice,
}

public static class ElementSystem
{
    private static Dictionary<ElementType, HashSet<ElementType>> baseRules = 
        new Dictionary<ElementType, HashSet<ElementType>>();

    private static Dictionary<ElementType, HashSet<ElementType>> tempRules = 
        new Dictionary<ElementType, HashSet<ElementType>>();

    public static bool CanTrigger(ElementType triggerElement, ElementType effectElement)
    {
        if (triggerElement == ElementType.None) return true;
        if (effectElement == ElementType.None) return true;

        if (tempRules.TryGetValue(triggerElement, out var tempAllowed) 
            && tempAllowed.Contains(effectElement))
        {
            return true;
        }

        if (baseRules.TryGetValue(triggerElement, out var baseAllowed) 
            && baseAllowed.Contains(effectElement))
        {
            return true;
        }

        return false;
    }

    public static void AddTempRule(ElementType trigger, ElementType effect)
    {
        if (!tempRules.ContainsKey(trigger))
            tempRules[trigger] = new HashSet<ElementType>();
        
        tempRules[trigger].Add(effect);
        Debug.Log($"[ElementSystem] Added temp rule: {trigger} can trigger {effect}");
    }

    public static void RemoveTempRule(ElementType trigger, ElementType effect)
    {
        if (tempRules.TryGetValue(trigger, out var set))
        {
            set.Remove(effect);
            Debug.Log($"[ElementSystem] Removed temp rule: {trigger} can trigger {effect}");
            
            if (set.Count == 0)
                tempRules.Remove(trigger);
        }
    }

    public static void ClearTempRules()
    {
        tempRules.Clear();
        Debug.Log("[ElementSystem] Cleared all temp rules");
    }

    public static Dictionary<ElementType, HashSet<ElementType>> GetTempRules()
    {
        return new Dictionary<ElementType, HashSet<ElementType>>(tempRules);
    }
}

