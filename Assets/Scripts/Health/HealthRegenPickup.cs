using UnityEngine;

public class HealthRegenPickup : Pickup
{
    protected override void ApplyPickupEffect(Entity entity)
    {
        var healthController = entity.GetComponent<PlayerHealth>();

        if (healthController != null)
        {
            healthController.RestoreFullHealth();
        }
    }
}
