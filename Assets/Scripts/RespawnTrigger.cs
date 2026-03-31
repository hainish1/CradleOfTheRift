using System.Collections.Generic;
using UnityEngine;

public class RespawnTrigger : MonoBehaviour
{
    public float threshold;

    [Header("Respawn Settings")]
    [Tooltip("The player will respawn at whichever is closest to where they fell off")]
    public List<Transform> respawnPoints = new();

    private CharacterController characterController;
    private Vector3 lastGroundedPosition;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        // last position above threshold 
        if (transform.position.y >= threshold)
        {
            lastGroundedPosition = transform.position;
        }

        if (transform.position.y < threshold)
        {
            Transform nearest = GetNearestRespawnPoint();
            if (nearest != null)
            {
                if (characterController != null) characterController.enabled = false;

                transform.position = nearest.position;
                transform.rotation = nearest.rotation;

                if (characterController != null) characterController.enabled = true;
            }
        }
    }

    private Transform GetNearestRespawnPoint()
    {
        if (respawnPoints.Count == 0) return null;

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (Transform point in respawnPoints)
        {
            if (point == null) continue;
            float dist = Vector3.Distance(lastGroundedPosition, point.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = point;
            }
        }

        return closest;
    }
}
