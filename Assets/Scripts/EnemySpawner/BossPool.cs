using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BossPool", menuName = "Game/Boss Pool")]
public class BossPool : ScriptableObject
{
    [Tooltip("The master list of all possible bosses.")]
    [SerializeField] private List<BossType> allBosses; 

    private List<BossType> availablePool; // Track what is left during the current game session

    public void InitializePool()
    {
        // Create a fresh copy of the master list
        availablePool = new List<BossType>(allBosses);
    }

    public BossType GetUniqueBoss()
    {
        if (availablePool == null || availablePool.Count == 0)
        {
            InitializePool();
        }

        // Pick a random index
        int randomIndex = UnityEngine.Random.Range(0, availablePool.Count);
        BossType selectedBoss = availablePool[randomIndex];

        // Remove it so it cannot be picked again until the pool is reset
        availablePool.RemoveAt(randomIndex);

        return selectedBoss;
    }
}