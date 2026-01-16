using UnityEngine;

public class SpawnNode : MonoBehaviour
{
    public bool isForFlyingEnemies = true;
    public bool isForGroundEnemies = true;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.3f);

        Gizmos.color = new Color(0, 1, 1, 0.3f); // Semi-transparent cyan
        if (TryGetComponent(out SphereCollider sphere))
        {
            float worldRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            Gizmos.DrawSphere(transform.position, worldRadius);

            // Blue for Flying, Green for Ground
            Color edgeColor = isForFlyingEnemies ? Color.blue : Color.green;
            Gizmos.color = edgeColor;
            Gizmos.DrawWireSphere(transform.position, worldRadius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (TryGetComponent(out SphereCollider sphere))
        {
            Gizmos.color = new Color(0, 1, 1, 0.6f); // Brighter cyan
            float worldRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            Gizmos.DrawSphere(transform.position, worldRadius);
        }
    }
}