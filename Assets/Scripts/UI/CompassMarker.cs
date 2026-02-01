using System.Collections.Generic;
using UnityEngine;

public enum MarkerType { Cave, Extraction }

public class CompassMarker : MonoBehaviour
{
    [SerializeField]
    private MarkerType type;
    public MarkerType Type => type;
    public static List<CompassMarker> AllMarkers { get; private set; } = new List<CompassMarker>();

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