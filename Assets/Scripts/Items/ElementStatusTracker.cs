using System.Collections.Generic;
using UnityEngine;

public class ElementStatusTracker : MonoBehaviour
{
    private readonly Dictionary<ElementType, float> hitTimes = new Dictionary<ElementType, float>();
    private float window = 0.5f;
    
    public void RecordElementHit(ElementType element)
    {
        if (element == ElementType.None) return;
        hitTimes[element] = Time.time;
        CleanExpiredStatuses();
    }
    
    public bool HasBothElements(ElementType a, ElementType b)
    {
        CleanExpiredStatuses();
        return hitTimes.ContainsKey(a) && hitTimes.ContainsKey(b);
    }
    
    private void CleanExpiredStatuses()
    {
        var expired = new List<ElementType>();
        foreach (var kvp in hitTimes)
        {
            if (Time.time - kvp.Value > window)
                expired.Add(kvp.Key);
        }
        foreach (var e in expired)
            hitTimes.Remove(e);
    }
    
    public void ClearStatuses()
    {
        hitTimes.Clear();
    }
}
