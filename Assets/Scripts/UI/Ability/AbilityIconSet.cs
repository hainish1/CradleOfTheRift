using UnityEngine;

[CreateAssetMenu(fileName = "AbilityIconSet", menuName = "UI/Ability Icon Set")]
public class AbilityIconSet : ScriptableObject
{
    [Header("Core Abilities")]
    public Texture2D dashIcon;
    public Texture2D flyIcon;
    public Texture2D shockwaveIcon;

    [Header("Weapon Defaults")]
    public Texture2D defaultRangedIcon;
}