using UnityEngine;

[CreateAssetMenu(fileName = "NewMarkerData", menuName = "Compass/Marker Data")]
public class CompassMarkerData : ScriptableObject
{
    public MarkerType markerType;
    public string ussClass; // e.g., "icon-cave"
    public Sprite fallbackSprite;
}