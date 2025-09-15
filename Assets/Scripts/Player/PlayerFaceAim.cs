using UnityEngine;

public class PlayerFaceAim : MonoBehaviour
{
    [SerializeField] private Transform aimCore;
    [SerializeField] private float turnSpeed;
    [SerializeField] private bool onlyMoving = false;
    [SerializeField] private CharacterController cc;


    void LateUpdate()
    {
        if (!aimCore) return;
        Vector3 flatDirection = aimCore.forward;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < .0001f)
        {
            return;
        }

        if (onlyMoving && cc != null && cc.velocity.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

    }
}
