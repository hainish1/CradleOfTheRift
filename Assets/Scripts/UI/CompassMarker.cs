using System.Collections.Generic;
using UnityEngine;

public enum MarkerType { Cave, Extraction }

public class CompassMarker : MonoBehaviour
{
    public MarkerType type;

    // Static list acts as a global registry that exists before any Start() calls
    public static List<CompassMarker> AllMarkers = new List<CompassMarker>();

    private void OnEnable()
    {
        if (!AllMarkers.Contains(this))
        {
            AllMarkers.Add(this);
        }
    }

    private void OnDisable()
    {
        AllMarkers.Remove(this);
    }
}