// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//   </para>
// </summary>

using System.Collections.Generic;
using UnityEngine;

public class TutorialObject : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Objects that are destroyed (enemies, pickups, etc.) and objects that are touched (trigger areas, pickups, etc.)")]
    private TutorialObjectType _objectType;
    [Tooltip("Specific name of the event this tutorial object will trigger.")] public string EventName;
    [SerializeField]
    [Tooltip("Whether or not the tutorial object should trigger only once.")] private bool _singleActivation = true;
    [SerializeField]
    [Tooltip("Whether any object or an exclusive target is allowed to trigger the tutorial object.")] private TutorialTargetExclusionType _allowedTargets;
    [SerializeField]
    [Tooltip("The exclisive targets that can trigger this tutorial object.")] private List<GameObject> _triggerTargets = new();

    // Collision handling is located inside the PlayerMovement script.

    void OnTriggerEnter(Collider other)
    {
        OnTriggerOrCollide(other);
    }

    void OnDestroy()
    {
        // Prevent triggering event twice if tutorial object's trigger type is by touch.
        if (_objectType == TutorialObjectType.Destroyable) TutorialManager.OnTutorialEvent.Invoke(EventName);
    }

    /// <summary>
    ///   <para>
    ///     Handles behavior for both OnTriggerEnter and OnControllerColliderHit events.
    ///     Destroys itself if a single action event.
    ///   </para>
    /// </summary>
    public void OnTriggerOrCollide(Collider other)
    {
        if (_objectType != TutorialObjectType.Touchable) return; // Return early if not a touchable type.
        if (_allowedTargets == TutorialTargetExclusionType.Exclusive && !_triggerTargets.Contains(other.gameObject)) return; // Return early if excluded.
        TutorialManager.OnTutorialEvent.Invoke(EventName);
        if (_singleActivation) Destroy(gameObject); // Destroy if single activation.
    }
}

/// <summary>
///   <para>
///     The variety of supported tutorial object types.
///   </para>
/// </summary>
public enum TutorialObjectType
{
    Touchable,
    Destroyable
}


/// <summary>
///   <para>
///     The variety of supported target exclusion types.
///   </para>
/// </summary>
public enum TutorialTargetExclusionType
{
    Any,
    Exclusive
}
