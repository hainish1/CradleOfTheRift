using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "New Ability", menuName = "Abilities/Ability Data")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    [TextArea(5, 10)]
    public string description;
    
    public Sprite icon;          // The thumbnail for the button
    public VideoClip previewClip; // The video file to play
    public string hotkey;        // "Q", "W", "E", or "R"
}