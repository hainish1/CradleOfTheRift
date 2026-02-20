using UnityEngine;
using System;
using System.Collections.Generic;

public class DifficultyScaler : MonoBehaviour
{
    [Serializable]
    public struct TierSettings
    {
        public string tierName;
        public float duration;
        public float tierGrowthRate; 
        public Color tierColor;
    }

    public enum ScalingMode { Linear, Exponential }

    [Header("Global Settings")]
    [SerializeField] private ScalingMode scalingMode = ScalingMode.Linear;
    [SerializeField] private float baseDifficulty = 1.0f; 
    
    [Header("Exponential Settings")]
    [SerializeField] private float exponentialRamp = 1.5f;

    [Header("Tier Configuration")]
    [SerializeField] private List<TierSettings> tiers = new List<TierSettings>();

    public float elapsedTime = 0f;
    private float totalGrowthAccumulated;
    private bool isRunning = true;

    public event Action<float, string> OnDifficultyUIUpdate;
    public List<TierSettings> GetTiers() => tiers;

    void Start()
    {
        totalGrowthAccumulated = 0f; 
    }

    void Update()
    {
        if (!isRunning || tiers.Count == 0) return;

        elapsedTime += Time.deltaTime;
        TierSettings currentTier = GetCurrentTier(out float tierProgress);
        
        totalGrowthAccumulated += currentTier.tierGrowthRate * Time.deltaTime;

        OnDifficultyUIUpdate?.Invoke(tierProgress, currentTier.tierName);
    }

    public float GetDifficultyScale()
    {
        if (scalingMode == ScalingMode.Linear)
        {
            // Formula: Base + (Growth * Time)
            return baseDifficulty + totalGrowthAccumulated;
        }
        else
        {
            // Corrected Formula: Base + ((Growth * Time) ^ Ramp)
            return baseDifficulty + Mathf.Pow(totalGrowthAccumulated, exponentialRamp);
        }
    }

    private TierSettings GetCurrentTier(out float tierProgress)
    {
        float cumulativeTime = 0f;
        foreach (var tier in tiers)
        {
            float tierEndTime = cumulativeTime + tier.duration;
            if (elapsedTime < tierEndTime)
            {
                tierProgress = (elapsedTime - cumulativeTime) / tier.duration;
                return tier;
            }
            cumulativeTime = tierEndTime;
        }
        tierProgress = 1f; 
        return tiers[tiers.Count - 1];
    }

    public void SetRunning(bool run) => isRunning = run;
}