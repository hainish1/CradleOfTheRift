using UnityEngine;

public class PlayerGroundCheck : MonoBehaviour
{
    public static bool IsGrounded { get; private set; }

    /// <summary>
    ///   <para>
    ///     Checks if the player character is touching the ground on the frame this method is called.
    ///   </para>
    /// </summary>
    public static void CheckIsGrounded(Vector3 playerBottom,
                                float castLength,
                                float castRadius,
                                int layerMasks,
                                out RaycastHit hitInfo,
                                float timer)
    {
        if (timer > 0)
        {
            IsGrounded = false;
            hitInfo = new RaycastHit();
            return;
        }

        Vector3 SphereCastOrigin = playerBottom + new Vector3(0, castRadius, 0);

        if (Physics.SphereCast(SphereCastOrigin,
                               castRadius,
                               Vector2.down,
                               hitInfo: out RaycastHit hit,
                               castLength + castRadius, // Compensate for the SphereCast starting higher.
                               layerMasks,
                               QueryTriggerInteraction.Ignore)
            && Vector3.Angle(Vector3.up, hit.normal) <= 30)
        {
            IsGrounded = true;
            hitInfo = hit;
            return;
        }

        IsGrounded = false;
        hitInfo = new RaycastHit();
        return;
    }



}
