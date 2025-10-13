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

        //while (numberOfChestsToSpawn > 0)
        //{
        //    int randomIndex = Random.Range(0, chests.Length);
        //    Debug.Log("Random Index: " + randomIndex);
        //    var chest = chests[randomIndex].gameObject;
        //    if (!chests[randomIndex].gameObject.activeSelf)
        //        chests[randomIndex].gameObject.SetActive(true);
        //        activeChests.Add(chest);
        //        numberOfChestsToSpawn--;

        //        // Doesn't work, ignore for now
        //        // This is very unoptimized and very prone to infinite while loops if the distance is too high
        //        // Checks if the chest is too close to another chest
        //        //foreach (var activeChest in activeChests)
        //        //{
        //        //    if (Vector3.Distance(activeChest.transform.position, chest.transform.position) >= minDistanceBetweenChests)
        //        //    {
        //        //        chests[randomIndex].gameObject.SetActive(true);
        //        //        activeChests.Add(chest);
        //        //        numberOfChestsToSpawn--;
        //        //    }
        //        //    else
        //        //    {
        //        //        Debug.Log("Chest too close to another chest. Retrying");
        //        //    }
        //        //}
        //}
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

            // Check if this chest's location is valid.
            bool isLocationValid = true;
            foreach (GameObject activeChest in activeChests)
            {
                // Calculate the distance between the potential chest and an already active chest.
                float distance = Vector3.Distance(potentialChest.transform.position, activeChest.transform.position);

                // If it's too close, mark the location as invalid and stop checking.
                if (distance < minDistanceBetweenChests)
                {
                    isLocationValid = false;
                    break; // No need to check against other active chests.
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
        
        // Alternative approach
        //activeChests = new List<GameObject>();
        //int attempts = 0;
        //int maxAttempts = 1000; // Prevent infinite loops
        //while (numberOfChestsToSpawn > 0 && attempts < maxAttempts)
        //{
        //    int randomIndex = Random.Range(0, chests.Length);
        //    var chest = chests[randomIndex].gameObject;
        //    if (!chest.activeSelf)
        //    {
        //        bool tooClose = false;
        //        foreach (var activeChest in activeChests)
        //        {
        //            if (Vector3.Distance(activeChest.transform.position, chest.transform.position) < minDistanceBetweenChests)
        //            {
        //                tooClose = true;
        //                break;
        //            }
        //        }
        //        if (!tooClose)
        //        {
        //            chest.SetActive(true);
        //            activeChests.Add(chest);
        //            numberOfChestsToSpawn--;
        //        }
        //    }
        //    attempts++;
        //}
        //if (attempts >= maxAttempts)
        //{
        //    Debug.LogWarning("Max attempts reached. Could not place all chests with the given distance constraints.");
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
