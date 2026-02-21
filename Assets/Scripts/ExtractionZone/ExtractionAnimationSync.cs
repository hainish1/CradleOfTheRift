using UnityEngine;

public class ExtractionAnimationSync : MonoBehaviour
{
    private ExtractionZone zone;
    private Animator animator;
    private RockTextureChanger rockTextureChanger;

    // Track the previous normalized progress to detect forward/backward movement
    private float lastNormalizedProgress = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        zone = GetComponentInParent<ExtractionZone>();
        rockTextureChanger = GetComponentInParent<RockTextureChanger>();

        if (zone == null)
        {
            Debug.LogError($"ExtractionAnimationSync on {gameObject.name} can't find ExtractionZone on any parent!");
        }

        if (rockTextureChanger == null)
            Debug.LogError($"ExtractionAnimationSync on {gameObject.name} can't find RockTextureChanger on any parent!");
        
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

        // Check each rock threshold and fire the appropriate method
        for (int i = 0; i < rockTextureChanger.GetCachedThresholds().Length; i++)
        {
            float threshold = rockTextureChanger.GetCachedThresholds()[i];

            // Moving FORWARD past a threshold -> change material
            if (lastNormalizedProgress < threshold && normalizedProgress >= threshold)
            {
                rockTextureChanger.ChangeRockMaterial(i);
            }
            // Moving BACKWARD past a threshold -> reset material
            else if (lastNormalizedProgress >= threshold && normalizedProgress < threshold)
            {
                rockTextureChanger.ResetRockMaterial(i);
            }
        }

        lastNormalizedProgress = normalizedProgress;


        // This forces the animation to the exact frame matching the progress bar
        animator.Play("Extraction_Process", 0, normalizedProgress);
    }
}