using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class EnemyRecycler : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;

    [Header("Recycle Thresholds")]
    public float recycleDistance = 100f; 
    public float checkInterval = 2.0f;

    [Header("Search Settings")]
    public float minDistance = 35f;      
    public float initialMaxDistance = 60f;
    public float searchExpansionStep = 20f;
    public int maxExpansionAttempts = 3;

    private List<SpawnNode> allNodes = new List<SpawnNode>();

    void Start()
    {
        // Cache all nodes in the scene
        allNodes = new List<SpawnNode>(FindObjectsByType<SpawnNode>(FindObjectsSortMode.None));
        
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        StartCoroutine(RecycleRoutine());
    }

    IEnumerator RecycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            // Handle flying enemies (EnemyRange)
            EnemyRange[] flyers = Object.FindObjectsByType<EnemyRange>(FindObjectsSortMode.None);
            foreach (EnemyRange enemy in flyers)
            {
                ProcessRecycle(enemy.gameObject, true);
            }

            // Handle ground enemies (EnemyMelee)
            EnemyMelee[] walkers = Object.FindObjectsByType<EnemyMelee>(FindObjectsSortMode.None);
            foreach (EnemyMelee enemy in walkers)
            {
                ProcessRecycle(enemy.gameObject, false); 
            }
        }
    }

    void ProcessRecycle(GameObject enemyObj, bool isFlying)
    {
        float distSqr = (enemyObj.transform.position - playerTransform.position).sqrMagnitude;

        // Only recycle if out of range and not visible
        if (distSqr > (recycleDistance * recycleDistance))
        {
            if (!IsVisibleToPlayer(enemyObj))
            {
                TeleportToNode(enemyObj, isFlying);
            }
        }
    }

    void TeleportToNode(GameObject enemyObj, bool isFlying)
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

    SpawnNode FindNodeWithExpandingSearch(bool enemyIsFlying)
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

    bool IsVisibleToPlayer(GameObject enemyObj)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        if (enemyObj.TryGetComponent(out Renderer rend))
        {
            return GeometryUtility.TestPlanesAABB(planes, rend.bounds);
        }
        return false;
    }
}