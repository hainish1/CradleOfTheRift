using UnityEngine;
using System.Collections.Generic;

public class SpawnChest : MonoBehaviour
{
    [SerializeField] private GameObject chest;
    [SerializeField] private GameObject[] chests;
    [SerializeField] private int numberOfChestsToSpawn = 5;
    [SerializeField] private float minDistanceBetweenChests = 5f;   // Inclusive
    private List<GameObject> activeChests;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        activeChests = new List<GameObject>();
        foreach (GameObject chest in chests)
        {
            //if(!chest.gameObject.activeSelf)
                chest.gameObject.SetActive(false);
        }

        while (numberOfChestsToSpawn > 0)
        {
            int randomIndex = Random.Range(0, chests.Length);
            Debug.Log("Random Index: " + randomIndex);
            var chest = chests[randomIndex].gameObject;
            if (!chests[randomIndex].gameObject.activeSelf)
                chests[randomIndex].gameObject.SetActive(true);
                activeChests.Add(chest);
                numberOfChestsToSpawn--;

                // Doesn't work, ignore for now
                // This is very unoptimized and very prone to infinite while loops if the distance is too high
                // Checks if the chest is too close to another chest
                //foreach (var activeChest in activeChests)
                //{
                //    if (Vector3.Distance(activeChest.transform.position, chest.transform.position) >= minDistanceBetweenChests)
                //    {
                //        chests[randomIndex].gameObject.SetActive(true);
                //        activeChests.Add(chest);
                //        numberOfChestsToSpawn--;
                //    }
                //    else
                //    {
                //        Debug.Log("Chest too close to another chest. Retrying");
                //    }
                //}
        }
        //Instantiate(Chest, transform.position, Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
