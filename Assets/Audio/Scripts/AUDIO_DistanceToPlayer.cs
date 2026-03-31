using UnityEngine;

public class AUDIO_DistanceToPlayer : MonoBehaviour
{
    private Transform playerLocation;
    [SerializeField]
    private AK.Wwise.RTPC distanceController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerLocation = GameObject.FindWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Distance(transform.position, playerLocation.position);
        distanceController.SetGlobalValue(distance);
    }
}
