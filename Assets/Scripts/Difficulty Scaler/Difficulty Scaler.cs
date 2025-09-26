using UnityEngine;

public class DifficultyScaler : MonoBehaviour
{
    [Header("Difficulty Scaler")]
    // I did this so you can have customizable difficulty names.
    // This should make it easy to update the UI!
    public string[] difficulties;
    // How many seconds it takes to transition to the next difficulty.
    public float timePerDifficulty = 5;
    // How many times the scale should be updated over the previously mentioned interval.
    // This doesn't really matter and I guess will only ever be used to make the UI possibly smoother.
    public int updatesPerDifficulty = 20;
    // The actual scale.
    // This can be used for boosting loot
    // Or boosting enemy spawn rates!
    private float difficultyScale = 1f;
    private float difficultyPerUpdate;
    private float updateRate;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        difficultyPerUpdate = 1f / updatesPerDifficulty;
        updateRate = timePerDifficulty / updatesPerDifficulty;
        InvokeRepeating(nameof(IncreaseDifficultyScale), updateRate, updateRate);
    }

    private void IncreaseDifficultyScale()
    {
        difficultyScale += difficultyPerUpdate;
    }

    public string GetCurrentDifficultyName()
    {
        int difficultyIndex = (int) (difficultyScale - 1);
        // If we are over max difficulty, clamp to the max difficulty.
        if (difficultyIndex > difficulties.Length - 1)
        {
            difficultyIndex = difficulties.Length - 1;
        }
        return difficulties[difficultyIndex];
    }

    public float GetDifficultyScale()
    {
        return difficultyScale;
    }
}
