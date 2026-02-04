using UnityEngine;

public class ExtractionAnimationSync : MonoBehaviour
{
    [SerializeField] private ExtractionZone zone;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        // Set speed to 0 so the animation only moves when we tell it to
        animator.speed = 0; 
    }

    private void OnEnable()
    {
        zone.ChargeChanged += UpdateAnimationPosition;
    }

    private void OnDisable()
    {
        zone.ChargeChanged -= UpdateAnimationPosition;
    }

    private void UpdateAnimationPosition(float currentCharge)
    {
        // Calculate 0.0 to 1.0 progress
        float normalizedProgress = currentCharge / zone.ChargeTime;

        // This forces the animation to the exact frame matching the progress bar
        animator.Play("Extraction_Process", 0, normalizedProgress);
    }
}