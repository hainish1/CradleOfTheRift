using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class EnemyRecycler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;

    [Header("Recycle Thresholds")]
    [SerializeField] private float recycleDistance = 100f; // 
    [SerializeField] private float checkInterval = 5.0f;

    [Header("Search Settings")]
    [SerializeField] private float minDistance = 15f;      
    [SerializeField] private float initialMaxDistance = 60f;
    [SerializeField] private float searchExpansionStep = 20f;
    [SerializeField] private int maxExpansionAttempts = 3;

    [Header("Advanced Settings")]
    [SerializeField] private float forceRecycleDistance = 150f; // Overrides visibility check if they are this far
    private List<SpawnNode> allNodes = new List<SpawnNode>();

    void Start()
    {
        EnsurePlayerLocation();

        // Cache all nodes in the scene
        allNodes = new List<SpawnNode>(FindObjectsByType<SpawnNode>(FindObjectsSortMode.None));

        StartCoroutine(RecycleRoutine());
    }

    private void EnsurePlayerLocation()
    {
        if (playerTransform != null) return;

        var playerGo = PlayerLocator.FindPlayerGameObject();
        if (playerGo != null)
            playerTransform = playerGo.transform;
    }

    private IEnumerator RecycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            // Iterate backwards so we can safely skip null entries if an enemy was destroyed mid-frame 
            var flyers = EnemyRegistry.Flyers;
            for (int i = flyers.Count - 1; i >= 0; i--)
            {
                if (flyers[i] != null)
                    ProcessRecycle(flyers[i].gameObject, true);
            }

            var walkers = EnemyRegistry.Walkers;
            for (int i = walkers.Count - 1; i >= 0; i--)
            {
                if (walkers[i] != null)
                    ProcessRecycle(walkers[i].gameObject, false);
            }
        }
    }

    private void ProcessRecycle(GameObject enemyObj, bool isFlying)
    {
        float dist = Vector3.Distance(enemyObj.transform.position, playerTransform.position);

        // Must be beyond the initial recycle threshold (100m)
        if (dist > recycleDistance)
        {
            // If they are EXTREMELY far (150m), just recycle them regardless of visibility
            if (dist > forceRecycleDistance)
            {
                TeleportToNode(enemyObj, isFlying);
                return;
            }

            // Check visibility (only if they aren't forced by Rule 2)
            if (!IsVisibleToPlayer(enemyObj))
            {
                TeleportToNode(enemyObj, isFlying);
            }
        }
    }

    private void TeleportToNode(GameObject enemyObj, bool isFlying)
    {
        SpawnNode bestNode = FindNodeWithExpandingSearch(isFlying);
        
        if (bestNode != null)
        {
            // Calculate random point within node radius
            float radius = 1f;
            if (bestNode.TryGetComponent(out SphereCollider sphere))
            {
                float maxScale = Mathf.Max(
                    Mathf.Abs(bestNode.transform.lossyScale.x), 
                    Mathf.Abs(bestNode.transform.lossyScale.z)
                );
                radius = sphere.radius * maxScale;
            }

            // Pick a random X/Z point within the circle
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 finalPos = bestNode.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Apply teleportation
            if (enemyObj.TryGetComponent(out NavMeshAgent agent))
            {
                if (NavMesh.SamplePosition(finalPos, out NavMeshHit hit, radius + 2f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }
        }
    }

    private SpawnNode FindNodeWithExpandingSearch(bool enemyIsFlying)
    {
        float currentMax = initialMaxDistance;

        for (int i = 0; i < maxExpansionAttempts; i++)
        {
            List<SpawnNode> validNodes = new List<SpawnNode>();

            foreach (SpawnNode node in allNodes)
            {
                // Match type based on SpawnNode flags
                bool typeMatch = enemyIsFlying ? node.isForFlyingEnemies : node.isForGroundEnemies;

                if (typeMatch)
                {
                    float dist = Vector3.Distance(playerTransform.position, node.transform.position);
                    
                    // Get node radius for edge calculation
                    float nodeRadius = 0f;
                    if (node.TryGetComponent(out SphereCollider sphere)) {
                        nodeRadius = sphere.radius * Mathf.Abs(node.transform.lossyScale.x);
                    }                
                    float distanceToNearEdge = dist - nodeRadius;

                    // Consistent "Donut" search logic
                    if (distanceToNearEdge >= minDistance && distanceToNearEdge <= currentMax)
                    {
                        validNodes.Add(node);
                    }
                }
            }

            if (validNodes.Count > 0)
                return validNodes[Random.Range(0, validNodes.Count)];

            currentMax += searchExpansionStep;
        }
        return null;
    }

    private bool IsVisibleToPlayer(GameObject enemyObj)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Renderer rend = enemyObj.GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            return GeometryUtility.TestPlanesAABB(planes, rend.bounds);
        }

        return false;
    }
}