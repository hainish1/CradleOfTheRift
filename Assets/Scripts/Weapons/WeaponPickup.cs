// <summary>
//   <authors>
//     Samuel Rigby, Jiedi Mo
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//     Contributed to by Jiedi Mo.
//          -Wrote the IInteractable interface.
//   </para>
// </summary>

using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private HeldWeaponType _thisWeapon;
    public string InteractionPrompt { get; }
    public bool SingleActivation { get; } = true;
    private bool _interacted = false;

    /// <summary>
    ///   <para>
    ///     Changes the player's weapon to the weapon type of this pickup.
    ///   </para>
    /// </summary>
    /// <param name="interactor"> The player's interactor script. </param>
    /// <returns> True if weapon was picked up, otherwise false. </returns>
    public bool Interact(Interactor interactor)
    {
        Debug.Log("Interacted with " + gameObject.name);
        if (_interacted) return false;

        // Change held weapon to this pickup's weapon type.
        PlayerHeldWeaponController heldWeaponController = interactor.gameObject.GetComponent<PlayerHeldWeaponController>();
        heldWeaponController.ChangeWeapon(_thisWeapon);

        // Schedule pickup for destruction and prevent multiple interactions.
        _interacted = true;
        Destroy(gameObject, 1.0f);
        return true;
    }
}
