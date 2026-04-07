using System.Collections.Generic;
using UnityEngine;


public class TutorialRespawn : MonoBehaviour
{
    [Tooltip("list of respawn points, one per tutorial. Index 0 is the starting")]
    [SerializeField] private List<Transform> respawnPoints = new();

    [Tooltip("Y position below which the player is considered to fell of")]
    [SerializeField] private float fallThreshold = -90f;

    private int currentRespawnIndex = 0;
    private CharacterController characterController;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Start()
    {
        if (TutorialSceneManager.Instance != null)
            TutorialSceneManager.Instance.OnStepCompleted += OnTutorialStepCompleted;
    }

    void OnDestroy()
    {
        if (TutorialSceneManager.Instance != null)
            TutorialSceneManager.Instance.OnStepCompleted -= OnTutorialStepCompleted;
    }

    void FixedUpdate()
    {
        if (transform.position.y < fallThreshold)
        {
            RespawnAtCheckpoint();
        }
    }

    private void OnTutorialStepCompleted(TutorialStep step, int stepIndex)
    {
        // go to next respawn point if we have one for the next step
        int nextIndex = stepIndex + 1;
        if (nextIndex < respawnPoints.Count)
        {
            currentRespawnIndex = nextIndex;
        }
    }

    private void RespawnAtCheckpoint()
    {
        if (respawnPoints.Count == 0) return;

        Transform target = respawnPoints[currentRespawnIndex];

        if (characterController != null) characterController.enabled = false;

        transform.position = target.position;
        transform.rotation = target.rotation;

        if (characterController != null) characterController.enabled = true;
    }
}
