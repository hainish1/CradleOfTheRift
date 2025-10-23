using UnityEngine;

public class PlayerGroundCheck : MonoBehaviour
{
    /// <summary>
    ///   <para>
    ///     Checks if the player character is touching the ground on the frame this method is called.
    ///   </para>
    /// </summary>
    /// <param name="playerBottom"> Bottom-most point of the player capsule. </param>
    /// <param name="castLength"> Length of the downward sphere cast. </param>
    /// <param name="castRadius"> Radius of the downward sphere cast </param>
    /// <param name="layerMasks"> Layers that should be treated as ground. </param>
    /// <param name="hitInfo"> The info for the sphere cast hit point. </param>
    /// <param name="timer"> Skips execution if the provided timer value is greater than 0. </param>
    /// <returns> True if the sphere cast intersects the ground and the timer is inactive, otherwise false. </returns>
    public static bool GetIsGrounded(Vector3 playerBottom, float castLength, float castRadius,
                                       int layerMasks, out RaycastHit hitInfo, float timer)
    {
        if (timer > 0)
        {
            hitInfo = new RaycastHit();
            return false;
        }

        Vector3 SphereCastOrigin = playerBottom + new Vector3(0, castRadius, 0);

        if (Physics.SphereCast(SphereCastOrigin,
                               castRadius,
                               Vector2.down,
                               out RaycastHit hit,
                               castLength + castRadius, // Compensate for the SphereCast starting higher.
                               layerMasks,
                               QueryTriggerInteraction.Ignore)
            && Vector3.Angle(Vector3.up, hit.normal) <= 30)
        {
            hitInfo = hit;
            return true;
        }

        hitInfo = new RaycastHit();
        return false;
    }

    /// <summary>
    ///   <para>
    ///     Gets the player character's height above the ground on the frame this method is called.
    ///   </para>
    /// </summary>
    /// <param name="playerBottom"> Bottom-most point of the player capsule. </param>
    /// <param name="maxHeight"> Maximum height above the ground that should be registered. </param>
    /// <param name="layerMasks"> Layers that should be treated as ground. </param>
    /// <returns> The player character's height above the ground, or -1 if too high to register. </returns>
    public static float GetHeightAboveGround(Vector3 playerBottom, float maxHeight, int layerMasks)
    {
        if (Physics.Raycast(playerBottom, Vector3.down, out RaycastHit hitInfo, maxHeight, layerMasks, QueryTriggerInteraction.Ignore))
        {
            return hitInfo.distance;
        }

        return -1;
    }
}
