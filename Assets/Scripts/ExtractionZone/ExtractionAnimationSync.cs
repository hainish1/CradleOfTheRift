using UnityEngine;

public class ExtractionAnimationSync : MonoBehaviour
{
    private ExtractionZone zone;
    private Animator animator;
    private RockTextureChanger rockTextureChanger;

    // Track the previous normalized progress to detect forward/backward movement
    private float lastNormalizedProgress = 0f;

    // The normalized time threshold for each rock event (0.0 - 1.0) 
    // etc 0.268 for rock 1 means at 26.8% (frame 134/500) progress, we trigger the rock 1 event
    private readonly float[] rockEventThresholds = new float[]
    {
        0.268f,  // Rock 1
        0.468f,  // Rock 2
        0.532f,  // Rock 3
        0.622f,  // Rock 4
        0.756f,  // Rock 5
        0.89f,   // Rock 6
        0.94f,   // Rock 7
        0.97f,   // Rock 8
        0.912f,  // Rock 9
        0.846f,  // Rock 10
        0.712f,  // Rock 11
        0.644f,  // Rock 12
        0.6f,    // Rock 13
        0.512f,  // Rock 14
        0.334f,  // Rock 15
    };

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
        for (int i = 0; i < rockEventThresholds.Length; i++)
        {
            float threshold = rockEventThresholds[i];

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