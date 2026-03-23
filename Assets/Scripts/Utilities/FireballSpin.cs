using UnityEngine;

public class FireballSpin : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] float xRotationSpeed = 0f;
    [SerializeField] float yRotationSpeed = 0f;
    [SerializeField] float zRotationSpeed = 0f;
    
    // Vector3 startPosition;
    
    // void Start()
    // {
    //     startPosition = transform.position;
    // }
    
    void Update()
    {
        // Rotate 
        transform.Rotate(xRotationSpeed * Time.deltaTime, yRotationSpeed * Time.deltaTime, zRotationSpeed * Time.deltaTime);
    
    }
}
