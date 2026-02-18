using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

            // Handle flying enemies 
            EnemyRange[] flyers = Object.FindObjectsByType<EnemyRange>(FindObjectsSortMode.None);
            foreach (EnemyRange enemy in flyers)
            {
                ProcessRecycle(enemy.gameObject, true);
            }

            // Handle ground enemies
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
            if (!isFlying && enemyObj.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
            {
                agent.Warp(bestNode.transform.position);
            }
            else
            {
                enemyObj.transform.position = bestNode.transform.position;
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
                bool typeMatch = enemyIsFlying ? node.isForFlyingEnemies : node.isForGroundEnemies;

                if (typeMatch)
                {
                    float dSqr = (node.transform.position - playerTransform.position).sqrMagnitude;
                    if (dSqr >= (minDistance * minDistance) && dSqr <= (currentMax * currentMax))
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