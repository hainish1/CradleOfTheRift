using UnityEngine;

public class SlowZoneEffect : MonoBehaviour
{
    public float speedReduction = 0.35f; // Percentage to reduce speed by
    private float originalSpeed;    // Store the player's original speed
    
    void OnTriggerEnter(Collider collision)
    {
        // check if collided with player
        //PlayerMovement playerMovement = collision.GetComponent<Collider>().GetComponentInParent<PlayerMovement>();
        PlayerMovement playerMovement = collision.GetComponentInParent<PlayerMovement>();
        if (playerMovement != null)
        {
            Debug.Log("Player entered slow zone");
            // Check if already slowed, if not then slow the player
            if (playerMovement.isSlowed) return;

            originalSpeed = playerMovement.MoveMaxSpeed;

            // reduce player speed
            playerMovement.MoveMaxSpeed = originalSpeed * (1f - speedReduction);
            playerMovement.isSlowed = true;
            Debug.Log("Player speed reduced to: " + playerMovement.MoveMaxSpeed);
            playerMovement.MoveDecelerateImmediate(); // call decelerate to immediately apply the speed reduction
        }
        
    }

    void OnTriggerExit(Collider collision)
    {
        // check if collided with player
        //PlayerMovement playerMovement = collision.GetComponent<Collider>().GetComponentInParent<PlayerMovement>();
        PlayerMovement playerMovement = collision.GetComponentInParent<PlayerMovement>();
        if (playerMovement != null)
        {
            // Check if the player is currently slowed, if yes then restore the player's speed
            if (!playerMovement.isSlowed) return;

            originalSpeed = playerMovement.MoveMaxSpeed;
            
            // restore player speed
            playerMovement.MoveMaxSpeed = originalSpeed;
            playerMovement.isSlowed = false;
        }
    }
}
