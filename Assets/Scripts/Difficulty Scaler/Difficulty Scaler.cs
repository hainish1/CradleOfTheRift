using UnityEngine;
using System;

public class DifficultyScaler : MonoBehaviour
{
    public enum ScalingMode { Linear, NonLinear }

    [Header("General Settings")]
    [SerializeField] private ScalingMode scalingMode = ScalingMode.Linear;
    [SerializeField] private float baseDifficulty = 1.0f;
    [SerializeField] private float difficultyGrowthRate = 0.05f;

    [Header("Non-Linear Settings")]
    [Tooltip("Higher values make the difficulty ramp up faster toward the end of the run.")]
    [SerializeField] private float exponentialRamp = 1.5f;

    [Header("UI / Tiers")]
    public float timePerDifficultyTier = 180f;
    public readonly string[] difficultyNames = { "EASY", "NORMAL", "HARD", "VERY HARD", "INSANE", "HAHAHA" };
    
    public float elapsedTime = 0f;
    private bool isRunning = true;

    public event Action<float, string> OnDifficultyUIUpdate;

    void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        
        float currentTierFloat = elapsedTime / timePerDifficultyTier;
        int currentTierIndex = Mathf.Min(Mathf.FloorToInt(currentTierFloat), difficultyNames.Length - 1);
        float progressToNextTier = currentTierFloat % 1.0f;

        OnDifficultyUIUpdate?.Invoke(progressToNextTier, difficultyNames[currentTierIndex]);
    }

    public float GetDifficultyScale()
    {
        if (scalingMode == ScalingMode.Linear)
        {
            // Linear Formula: 1 + (time * rate)
            return baseDifficulty + (elapsedTime * difficultyGrowthRate);
        }
        else
        {
            // Non-Linear Formula: 1 + (time * rate)^ramp
            return baseDifficulty + Mathf.Pow(elapsedTime * difficultyGrowthRate, exponentialRamp);
        }
    }

    public void SetRunning(bool run) => isRunning = run;
}