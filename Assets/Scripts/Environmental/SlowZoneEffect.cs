using UnityEngine;

public class SlowZoneEffect : MonoBehaviour
{
    public float speedReduction = 0.35f; // Percentage to reduce speed by
    private float originalSpeed;    // Store the player's original speed
    private Vector3 originalLateralVelocityVector; // Store the player's original lateral speed
    
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
            originalLateralVelocityVector = playerMovement._lateralVelocityVector;

            // reduce player speed
            var modifier = new BasicStatsModifier(StatType.MoveSpeed, -1f, v => originalSpeed * (1f - speedReduction));
            playerMovement._playerEntity.Stats.Mediator.AddModifier(modifier);      
            playerMovement.isSlowed = true;
            //Debug.Log("Player speed reduced to: " + playerMovement.MoveMaxSpeed + ". Original speed was: " + originalSpeed);
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

             // restore player speed (maybe should add a duration instead of applying another modifier)
            var modifier = new BasicStatsModifier(StatType.MoveSpeed, -1f, v => originalSpeed);
            playerMovement._playerEntity.Stats.Mediator.AddModifier(modifier);
            playerMovement.isSlowed = false;
        }
    }

    void OnDrawGizmos()
    {
        // Draw a red wireframe sphere to visualize the slow zone
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, GetComponent<BoxCollider>().size);
        //Gizmos.DrawWireSphere(transform.position, GetComponent<SphereCollider>().radius);
    }
}
