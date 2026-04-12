// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//   </para>
// </summary>

using UnityEngine;

public class PlayerHeldWeaponController : MonoBehaviour
{
    [SerializeField] private GameObject _spearModel;
    [SerializeField] private GameObject _axeModel;
    [SerializeField] private GameObject _maceModel;
    [SerializeField] public HeldWeaponType HeldWeapon;
    private PlayerShooter _playerShooter;

    void Awake()
    {
        _playerShooter = GetComponentInChildren<PlayerShooter>();
    }

    void Start()
    {
        ChangeWeapon(HeldWeapon);
    }

    /// <summary>
    ///   <para>
    ///     Makes the player hold the weapon of the given type.
    ///   </para>
    /// </summary>
    /// <param name="weaponChange"> The weapon to change to. </param>
    public void ChangeWeapon(HeldWeaponType weaponChange)
    {
        _playerShooter.SetProjectileType(weaponChange); // Change the throwable projectile type.
        switch (weaponChange) // Change held weapon model to the corresponding type.
        {
            case HeldWeaponType.None:
                _spearModel.SetActive(false);
                _axeModel.SetActive(false);
                _maceModel.SetActive(false);
                break;
            case HeldWeaponType.Spear:
                _spearModel.SetActive(true);
                _axeModel.SetActive(false);
                _maceModel.SetActive(false);
                break;
            case HeldWeaponType.Axe:
                _spearModel.SetActive(false);
                _axeModel.SetActive(true);
                _maceModel.SetActive(false);
                break;
            case HeldWeaponType.Mace:
                _spearModel.SetActive(false);
                _axeModel.SetActive(false);
                _maceModel.SetActive(true);
                break;
            default:
                break;
        }
    }
}

/// <summary>
///   <para>
///     The variety of supported weapon types.
///   </para>
/// </summary>
public enum HeldWeaponType
{
    None,
    Spear,
    Axe,
    Mace
}
