using UnityEngine;

public class RespawnTrigger : MonoBehaviour
{
    public float threshold;
    [Header("Respawn Settings")]
    public Transform playerSpawnPoint;

    void FixedUpdate()
    {
        if (transform.position.y < threshold)
        {
            // Move player to spawn point
            if (playerSpawnPoint != null)
            {
                transform.position = playerSpawnPoint.position;
                transform.rotation = playerSpawnPoint.rotation; // Optional: reset rotation

                
            }
        }
    }

}
