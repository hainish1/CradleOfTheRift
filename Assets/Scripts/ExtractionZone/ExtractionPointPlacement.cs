using UnityEngine;

public class ExtractionPointPlacement : MonoBehaviour
{
    [Tooltip("A reference to all possible extraction point placements.")]
    [SerializeField] private GameObject[] extracPoints;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Check for null or empty array
        if (extracPoints.Length == 0 || extracPoints == null)
        {
            Debug.Log("No extraction points assigned in the inspector.");
            return;
        }

        // Deactivate all chests at the start
        foreach (GameObject extracPoint in extracPoints)
        {
            extracPoint.gameObject.SetActive(false);
        }

        int randomIndex = Random.Range(0, extracPoints.Length);
        extracPoints[randomIndex].SetActive(true);
        Debug.Log("Random Extraction Point active");
    }
}
