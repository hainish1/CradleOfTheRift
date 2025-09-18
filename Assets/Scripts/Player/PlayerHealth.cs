using UnityEngine;

public class PlayerHealth : HealthController
{
    protected override void Die()
    {
        Debug.Log("[PLAYER HEALTH] Player is DEADDD lmao");

        // end movement or change scene here if we want
    }
}
