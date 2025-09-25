using UnityEngine;

public class SpawnChest : MonoBehaviour
{
    [SerializeField] private GameObject chest;
    [SerializeField] private GameObject[] chests;
    [SerializeField] private int numberOfChestsToSpawn = 5;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (GameObject chest in chests)
        {
            //if(!chest.gameObject.activeSelf)
                chest.gameObject.SetActive(false);
        }

        // Add a check to make sure the chests aren't spawned to close to each other
        //for (int i = 0; i < numberOfChestsToSpawn; i++)
        //{
        //    int randomIndex = Random.Range(0, locations.Length);
        //    var chest = chests[randomIndex].gameObject;
        //    if (!chest.activeSelf)
        //        chest.SetActive(true);
        //}

        while (numberOfChestsToSpawn > 0)
        {
            int randomIndex = Random.Range(0, chests.Length);
            Debug.Log("Random Index: " + randomIndex);
            var chest = chests[randomIndex].gameObject;
            if (!chests[randomIndex].gameObject.activeSelf)
                chests[randomIndex].gameObject.SetActive(true);
                numberOfChestsToSpawn--;
                //return;
        }
        //Instantiate(Chest, transform.position, Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
