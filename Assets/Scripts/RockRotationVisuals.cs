using UnityEngine;

public class RockRotationVisuals : MonoBehaviour
{
    [SerializeField] private float rotationSpeedX = 180f;
    private Quaternion startingLocalRotation;

    private void Awake()
    {
        startingLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        transform.localRotation = startingLocalRotation;
    }

    private void Update()
    {
        transform.Rotate(rotationSpeedX * Time.deltaTime, 0f, 0f, Space.Self);
    }
}
