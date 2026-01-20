using UnityEngine;
using System.Collections.Generic;

public class ExtractionPointPlacement : MonoBehaviour
{
    [Tooltip("A reference to all possible extraction point placements.")]
    [SerializeField] private GameObject[] extracPoints;
    
    [Tooltip("The total number of extraction points you want to enable.")]
    [SerializeField] private int numberOfPointsToSpawn = 2;

    [Tooltip("The minimum distance allowed between any two enabled extraction points.")]
    [SerializeField] private float minDistanceBetweenPoints = 50f;   // Inclusive

    private List<GameObject> activePoints;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Check for null or empty array
        if (extracPoints.Length == 0 || extracPoints == null)
        {
            Debug.Log("No extraction points assigned in the inspector.");
            return;
        }

        // Deactivate all points at the start
        foreach (GameObject extracPoint in extracPoints)
        {
            extracPoint.gameObject.SetActive(false);
        }

        // int randomIndex = Random.Range(0, extracPoints.Length);
        // extracPoints[randomIndex].SetActive(true);
        // Debug.Log("Random Extraction Point active");
        SpawnPontWithDistanceCheck();
    }

    void SpawnPontWithDistanceCheck()
    {
        // Create a list of potential candidates from the initial array and remove candidates after checking.
        List<GameObject> candidatePoints = new List<GameObject>(extracPoints);
        activePoints = new List<GameObject>();

        // Loop until we have spawned the desired number of points OR we run out of candidates.
        while (activePoints.Count < numberOfPointsToSpawn && candidatePoints.Count > 0)
        {
            // Pick a random point from the list of candidates.
            int randomIndex = Random.Range(0, candidatePoints.Count);
            GameObject potentialPoint = candidatePoints[randomIndex];
            candidatePoints.RemoveAt(randomIndex);

            // Check if location is valid.
            bool isLocationValid = true;
            foreach (GameObject activePoint in activePoints)
            {
                // Calculate the distance between the potential point and an already active point.
                float distance = Vector3.Distance(potentialPoint.transform.position, activePoint.transform.position);

                if (distance < minDistanceBetweenPoints)
                {
                    isLocationValid = false;
                    break;
                }
            }

            // If the location is valid, add it to our list of active points.
            if (isLocationValid)
            {
                activePoints.Add(potentialPoint);
            }
        }

        // After the selection loop, activate all the chosen points.
        foreach (GameObject point in activePoints)
        {
            point.SetActive(true);
        }

        // Log a warning if desired number of points couldn't spawn.
        if (activePoints.Count < numberOfPointsToSpawn)
        {
            Debug.LogWarning($"Could not find valid positions for all extraction points. Spawned {activePoints.Count} out of {numberOfPointsToSpawn}.");
        }
    }
}
