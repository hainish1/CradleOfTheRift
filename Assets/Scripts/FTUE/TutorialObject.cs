// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah, February 2026.
//   </para>
// </summary>

using UnityEngine;

public class TutorialObject : MonoBehaviour
{
    [SerializeField] private TutorialObjectType ObjectType;
    public string EventName;
    private bool _wasTouched = false;

    // Collision handling is located inside the PlayerMovement script.

    void OnTriggerEnter(Collider other)
    {
        OnTriggerOrCollide();
    }

    void OnDestroy()
    {
        // Do not call event twice if tutorial object was triggered by touch.
        if (!_wasTouched) TutorialManager.TriggerTutorialEvent(EventName);
    }

    /// <summary>
    ///   <para>
    ///     Handles behavior for both OnTriggerEnter and OnControllerColliderHit events
    ///     and then destroys itself.
    ///   </para>
    /// </summary>
    public void OnTriggerOrCollide()
    {
        if (ObjectType != TutorialObjectType.Destroyable)
        {
            _wasTouched = true;
            TutorialManager.TriggerTutorialEvent(EventName);
            Destroy(gameObject);
        }
    }

    /// <summary>
    ///   <para>
    ///     The variety of supported tutorial object types.
    ///   </para>
    /// </summary>
    private enum TutorialObjectType
    {
        Touchable,
        Destroyable
    }
}
