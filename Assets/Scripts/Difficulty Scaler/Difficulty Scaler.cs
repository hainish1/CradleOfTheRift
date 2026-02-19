using UnityEngine;
using System;

public class DifficultyScaler : MonoBehaviour
{
    [Header("Difficulty Settings")]
    public float timePerDifficultyTier = 180f; // 3 minutes per tier
    [SerializeField] private float baseDifficulty = 1.0f;
    [SerializeField] private float difficultyGrowthRate = 0.05f;

    public float elapsedTime = 0f;
    private bool isRunning = true;

    public event Action<float, string> OnDifficultyUIUpdate;

    public readonly string[] difficultyNames = { "EASY", "NORMAL", "HARD", "VERY HARD", "INSANE", "HAHAHA" };

    void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        
        // Calculate UI values
        float currentTierFloat = elapsedTime / timePerDifficultyTier;
        int currentTierIndex = Mathf.Min(Mathf.FloorToInt(currentTierFloat), difficultyNames.Length - 1);
        
        // Calculate percentage to the next tier for the progress bar (0.0 to 1.0)
        float progressToNextTier = currentTierFloat % 1.0f; 

        // Fire event to update UI
        OnDifficultyUIUpdate?.Invoke(progressToNextTier, difficultyNames[currentTierIndex]);
    }

    public float GetDifficultyScale()
    {
        // Difficulty increases slightly every second
        return baseDifficulty + (elapsedTime * difficultyGrowthRate); 
    }

    public void SetRunning(bool run) => isRunning = run;
}