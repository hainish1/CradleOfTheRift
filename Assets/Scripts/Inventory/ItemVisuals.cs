using UnityEngine;

public class ItemVisuals : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] float rotationSpeed = 45f;
    [SerializeField] float bobHeight = 0.5f;
    [SerializeField] float bobSpeed = 2f;
    
    Vector3 startPosition;
    
    void Start()
    {
        startPosition = transform.position;
    }
    
    void Update()
    {
        // Rotate 
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        // Bobin
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}
