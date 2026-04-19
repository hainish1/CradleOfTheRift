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
        HeldWeaponType.Spear, HeldWeaponType.Axe, HeldWeaponType.Mace, HeldWeaponType.Staff
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
    
    [Header("Sounds")]
    [SerializeField] private AK.Wwise.Event activateSound;
    [SerializeField] private AK.Wwise.Event cantAffordSound;

    private HeldWeaponType resolvedWeapon;
    private bool weaponResolved;
    private bool canInteract = true;

    public bool SingleActivation => singleActivation;

    public string InteractionPrompt
    {
        get
        {
            if (selectionMode == WeaponSelectionMode.Specific)
            {
                ResolveWeaponIfNeeded();
            }
            string weaponName = selectionMode == WeaponSelectionMode.Random ? "?????" : resolvedWeapon.ToString();
            return $"<b>{weaponName}</b>\n" +
                   $"<color=#FFD447>[E]</color> {goldPrice}G \n" +
                   $"<color=#9AD5FF>[Ctrl+E]</color> {xpPrice}XP";
        }
    }

    private void Awake()
    {
        // Random mode resolves at interact time
        // so it can exclude the players currently held weapon
        if (selectionMode == WeaponSelectionMode.Specific)
        {
            ResolveWeaponIfNeeded();
        }
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

    private bool ResolveRandomExcluding(HeldWeaponType excluded)
    {
        if (randomPool == null || randomPool.Count == 0)
        {
            resolvedWeapon = HeldWeaponType.Spear;
            weaponResolved = true;
            return resolvedWeapon != excluded;
        }
        var filtered = new List<HeldWeaponType>(randomPool.Count);
        foreach (var weapon in randomPool)
        {
            if (weapon != excluded) filtered.Add(weapon);
        }
        if (filtered.Count == 0)
        {
            return false;
        }
        resolvedWeapon = filtered[Random.Range(0, filtered.Count)];
        weaponResolved = true;
        return true;
    }

    public bool Interact(Interactor interactor)
    {
        if (!canInteract)
        {
            PlayCantAffordSound();
            return false;
        }

        var heldController = interactor.GetComponent<PlayerHeldWeaponController>();
        if (heldController == null)
        {
            Debug.LogWarning("[Shrine] Interactor has no PlayerHeldWeaponController.");
            PlayCantAffordSound();
            return false;
        }

        // Random mode: reroll excluding the players current weapon
        if (selectionMode == WeaponSelectionMode.Random)
        {
            weaponResolved = false;
            if (!ResolveRandomExcluding(heldController.HeldWeapon))
            {
                Debug.Log("[Shrine] Random pool has no weapon the player doesnt already have");
                PlayCantAffordSound();
                return false;
            }
        }
        else
        {
            ResolveWeaponIfNeeded();
        }

        if (blockIfAlreadyEquipped && heldController.HeldWeapon == resolvedWeapon)
        {
            Debug.Log("[Shrine] Player already has this weapon.");
            PlayCantAffordSound();
            return false;
        }

        bool useXP = Keyboard.current != null &&
                     (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);

        if (!TrySpendCurrency(interactor, useXP))
        {
            PlayCantAffordSound();
            return false;
        }

        heldController.ChangeWeapon(resolvedWeapon);

        PlayActivateVFX();

        if (singleActivation)
        {
            canInteract = false;
            PlayCantAffordSound();
            Destroy(gameObject, 1f);
        }
        PlayActivateSound();
        return true;
    }

    private void PlayActivateSound()
    {
        if(activateSound != null)
            if(activateSound.IsValid())
                activateSound.Post(gameObject);
    }
    private void PlayCantAffordSound()
    {
        if(cantAffordSound != null)
            if(cantAffordSound.IsValid())
                cantAffordSound.Post(gameObject);
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
