using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shrine: interact to replace the player's currently held weapon with a
/// specific or randomly chosen weapon. Player chooses to pay with Gold or XP
/// [E] for Gold, [Shift+E] for XP
/// </summary>
public class Shrine : MonoBehaviour, IInteractable, IPromptHeightOverride
{
    public enum WeaponSelectionMode { Specific, Random }

    [Header("Weapon")]
    [Tooltip("Specific weapon or random")]
    [SerializeField] private WeaponSelectionMode selectionMode = WeaponSelectionMode.Specific;

    [Tooltip("The weapon given when Selection Mode is specific")]
    [SerializeField] private HeldWeaponType specificWeapon = HeldWeaponType.Axe;

    [Tooltip("Random pool")]
    [SerializeField]
    private List<HeldWeaponType> randomPool = new List<HeldWeaponType>
    {
        HeldWeaponType.Spear, HeldWeaponType.Axe, HeldWeaponType.Mace
    };

    [Header("Cost")]
    [SerializeField] private int goldPrice = 25;
    [SerializeField] private int xpPrice = 50;

    [Header("Behavior")]
    [SerializeField] private bool singleActivation = true;
    [Tooltip("If true, the player cant buy the same weapon they have")]
    [SerializeField] private bool blockIfAlreadyEquipped = true;

    [Header("VFX")]
    [SerializeField] private GameObject activateVFX;
    [SerializeField] private Transform vfxAnchor;

    [Header("Prompt")]
    [SerializeField] private float promptHeightOffset = 2.5f;
    public float PromptHeightOffset => promptHeightOffset;

    private HeldWeaponType resolvedWeapon;
    private bool weaponResolved;
    private bool canInteract = true;

    public bool SingleActivation => singleActivation;

    public string InteractionPrompt
    {
        get
        {
            ResolveWeaponIfNeeded();
            string weaponName = selectionMode == WeaponSelectionMode.Random ? "?????" : resolvedWeapon.ToString();
            return $"<b>{weaponName}</b>  " +
                   $"<color=#FFD447>[E]</color> {goldPrice}G  " +
                   $"<color=#9AD5FF>[Ctrl+E]</color> {xpPrice}XP";
        }
    }

    private void Awake()
    {
        ResolveWeaponIfNeeded();
    }

    private void ResolveWeaponIfNeeded()
    {
        if (weaponResolved) return;
        if (selectionMode == WeaponSelectionMode.Specific)
        {
            resolvedWeapon = specificWeapon;
        }
        else
        {
            if (randomPool == null || randomPool.Count == 0)
            {
                resolvedWeapon = HeldWeaponType.Spear;
            }
            else
            {
                resolvedWeapon = randomPool[Random.Range(0, randomPool.Count)];
            }
        }
        weaponResolved = true;
    }

    public bool Interact(Interactor interactor)
    {
        if (!canInteract) return false;
        ResolveWeaponIfNeeded();

        var heldController = interactor.GetComponent<PlayerHeldWeaponController>();
        if (heldController == null)
        {
            Debug.LogWarning("[Shrine] Interactor has no PlayerHeldWeaponController.");
            return false;
        }

        if (blockIfAlreadyEquipped && heldController.HeldWeapon == resolvedWeapon)
        {
            Debug.Log("[Shrine] Player already has this weapon.");
            return false;
        }

        bool useXP = Keyboard.current != null &&
                     (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);

        if (!TrySpendCurrency(interactor, useXP))
        {
            return false;
        }

        heldController.ChangeWeapon(resolvedWeapon);

        PlayActivateVFX();

        if (singleActivation)
        {
            canInteract = false;
            Destroy(gameObject, 1f);
        }
        return true;
    }

    private bool TrySpendCurrency(Interactor interactor, bool useXP)
    {
        if (useXP)
        {
            var xp = PlayerXP.Instance;
            if (xp == null)
            {
                Debug.LogWarning("[Shrine] PlayerXP not available.");
                return false;
            }
            return xp.SpendXP(xpPrice);
        }

        var gold = interactor.GetComponent<PlayerGold>();
        if (gold == null)
        {
            Debug.LogWarning("[Shrine] No PlayerGold component on interactor.");
            return false;
        }
        return gold.SpendGold(goldPrice);
    }

    private void PlayActivateVFX()
    {
        if (activateVFX == null) return;
        Transform anchor = vfxAnchor != null ? vfxAnchor : transform;
        var fx = Instantiate(activateVFX, anchor.position, anchor.rotation);
        Destroy(fx, 3f);
    }
}
