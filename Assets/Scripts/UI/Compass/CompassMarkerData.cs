using UnityEngine;

[CreateAssetMenu(fileName = "NewMarkerData", menuName = "Compass/Marker Data")]
public class CompassMarkerData : ScriptableObject
{
    public MarkerType markerType;
    public string ussClass; // e.g., "icon-cave"
    public Sprite fallbackSprite;

    [Header("Visibility Settings")]
    [Tooltip("How close the player must be for this marker to appear on the compass.")]
    public float detectionRadius = 150f;
}