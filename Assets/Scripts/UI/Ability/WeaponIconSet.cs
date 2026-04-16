using UnityEngine;

[System.Serializable]
public class WeaponIconData
{
    public Texture2D icon;
    public float scale = 1.0f;
    public Vector2 offset = Vector2.zero;
}

[CreateAssetMenu(fileName = "WeaponIconSet", menuName = "UI/WeaponIconSet")]
public class WeaponIconSet : ScriptableObject
{
    public WeaponIconData spear;
    public WeaponIconData axe;
    public WeaponIconData mace;
    public WeaponIconData staff;
}
