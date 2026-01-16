using UnityEngine;

public class SpawnNode : MonoBehaviour
{
    public bool isForFlyingEnemies = true;
    public bool isForGroundEnemies = true;

    private void OnDrawGizmos()
    {
        // Draw the center point
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Draw the radius of the "Spawn Zone"
        if (TryGetComponent(out SphereCollider sphere))
        {
            // Set color based on type
            Color radiusColor = isForFlyingEnemies ? Color.blue : Color.green;
            radiusColor.a = 0.2f; // Make it semi-transparent
            Gizmos.color = radiusColor;

            float worldRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            Gizmos.DrawWireSphere(transform.position, worldRadius);
        }
    }

    // This makes the gizmo look solid when you select the node
    private void OnDrawGizmosSelected()
    {
        if (TryGetComponent(out SphereCollider sphere))
        {
            Gizmos.color = new Color(0, 1, 1, 0.4f);
            float worldRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            Gizmos.DrawSphere(transform.position, worldRadius);
        }
    }
}