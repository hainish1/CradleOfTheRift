using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance;
    [SerializeField] private int poolSize = 10;

    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();



    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject GetObject(GameObject prefabName, Transform target)
    {
        if (poolDictionary.ContainsKey(prefabName) == false) // if not alr there
        {
            CreateInitialPool(prefabName);
        }

        if (poolDictionary[prefabName].Count == 0)
        {
            CreateNewObject(prefabName); // if all objects of this type are in use, create new ones, then
        }

        GameObject objectToGet = poolDictionary[prefabName].Dequeue();

        // object may have been destroyed by sum else, if so, create a new one
        if (objectToGet == null)
        {
            CreateNewObject(prefabName);
            objectToGet = poolDictionary[prefabName].Dequeue();
        }

        // set position of object before I actually enable it
        objectToGet.transform.position = target.position;
        objectToGet.transform.parent = null;

        objectToGet.SetActive(true);
        return objectToGet;
    }

    public void ReturnObject(GameObject objectToReturn, float delayTime = 0.001f)
    {
        if (objectToReturn == null) return;
        StartCoroutine(DelayReturn(objectToReturn, delayTime));
    }

    private IEnumerator DelayReturn(GameObject objectToReturn, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        if (objectToReturn != null)
            ReturnToPool(objectToReturn);
    }
    
    private void ReturnToPool(GameObject objectToReturn){
        if (objectToReturn == null) return;

        var pooled = objectToReturn.GetComponent<PooledObject>();
        if (pooled == null || pooled.originalPrefab == null)
        {
            // not a pooled object, just destroy it normally, someone mightve created using Instantiate, but destroyed using ReturnToPool which is valid cause ObjectPooling was not null in scene
            Destroy(objectToReturn);
            return;
        }

        GameObject originalPrefab = pooled.originalPrefab;
        objectToReturn.SetActive(false);

        if (poolDictionary.ContainsKey(originalPrefab))
            poolDictionary[originalPrefab].Enqueue(objectToReturn);

        objectToReturn.transform.parent = transform;
    }

    private void CreateInitialPool(GameObject prefab)
    {
        poolDictionary[prefab] = new Queue<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewObject(prefab);
        }
    }

    /// <summary>
    /// Helper to create a New Object that carries info about the original prefab, so it can be identified by the pool dictionary
    /// </summary>
    private void CreateNewObject(GameObject prefab)
    {
        GameObject newObject = Instantiate(prefab, transform);
        newObject.AddComponent<PooledObject>().originalPrefab = prefab; // assign value of original prefab to the prefab we are trying to create/instantiate

        newObject.SetActive(false);
        poolDictionary[prefab].Enqueue(newObject);
    }

}
