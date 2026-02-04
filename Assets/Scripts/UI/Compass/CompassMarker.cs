using System;
using System.Collections.Generic;
using UnityEngine;

public enum MarkerType { Cave, Extraction, Chest }

public class CompassMarker : MonoBehaviour
{
    [SerializeField]
    private MarkerType type;
    public MarkerType Type => type;
    public static event Action<CompassMarker> OnMarkerAdded;
    public static event Action<CompassMarker> OnMarkerRemoved;
    public static List<CompassMarker> AllMarkers { get; private set; } = new List<CompassMarker>();

    private void OnEnable()
    {
        if (!AllMarkers.Contains(this))
        {
            AllMarkers.Add(this);
            OnMarkerAdded?.Invoke(this);
        }
    }

    private void OnDisable()
    {
        if (AllMarkers.Contains(this))
        {   
            AllMarkers.Remove(this);
            OnMarkerRemoved?.Invoke(this);
        }
    }
}