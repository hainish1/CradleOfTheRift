using UnityEngine;
using System.Collections.Generic;

public class SpawnChest : MonoBehaviour
{
    [Tooltip("A reference to all possible chest GameObjects in the scene.")]
    [SerializeField] private GameObject[] chests;

    [Tooltip("The total number of chests you want to enable.")]
    [SerializeField] private int numberOfChestsToSpawn = 5;

    [Tooltip("The minimum distance allowed between any two enabled chests.")]
    [SerializeField] private float minDistanceBetweenChests = 5f;   // Inclusive
    
    private List<GameObject> activeChests;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Check for null or empty array
        if (chests.Length == 0 || chests == null)
        {
            Debug.Log("No chests assigned in the inspector.");
            return;
        }

        // Deactivate all chests at the start
        foreach (GameObject chest in chests)
        {
            chest.gameObject.SetActive(false);
        }

        SpawnChestWithDistanceCheck();
    }

    void SpawnChestWithDistanceCheck()
    {
        // Create a list of potential candidates from the initial array and remove candidates after checking.
        List<GameObject> candidateChests = new List<GameObject>(chests);
        activeChests = new List<GameObject>();

        // Loop until we have spawned the desired number of chests OR we run out of candidates.
        while (activeChests.Count < numberOfChestsToSpawn && candidateChests.Count > 0)
        {
            // Pick a random chest from the list of candidates.
            int randomIndex = Random.Range(0, candidateChests.Count);
            GameObject potentialChest = candidateChests[randomIndex];
            candidateChests.RemoveAt(randomIndex);

            // Check if location is valid.
            bool isLocationValid = true;
            foreach (GameObject activeChest in activeChests)
            {
                // Calculate the distance between the potential chest and an already active chest.
                float distance = Vector3.Distance(potentialChest.transform.position, activeChest.transform.position);

                if (distance < minDistanceBetweenChests)
                {
                    isLocationValid = false;
                    break;
                }
            }

            // If the location is valid, add it to our list of active chests.
            if (isLocationValid)
            {
                activeChests.Add(potentialChest);
            }
        }

        // After the selection loop, activate all the chosen chests.
        foreach (GameObject chest in activeChests)
        {
            chest.SetActive(true);
        }

        // Log a warning if desired number of chests couldn't spawn.
        if (activeChests.Count < numberOfChestsToSpawn)
        {
            Debug.LogWarning($"Could not find valid positions for all chests. Spawned {activeChests.Count} out of {numberOfChestsToSpawn}.");
        }
    }
}
