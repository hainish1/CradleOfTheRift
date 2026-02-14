using System;
using UnityEngine;

/// <summary>
/// Tracks Player Xp and level for the current run
/// </summary>
public class PlayerXP : MonoBehaviour
{
    public static PlayerXP Instance { get; private set; }

    [Header("Leveling up")]
    [Tooltip("How much XP is needed to reach the next upgrade level.")]
    [SerializeField] private int xpToLevelUp = 100;

    private int currentXP;
    private int currentLevel;
    private bool levelUpReady;

    // this fires whenever XP changes
    public event Action<int, int> XPChanged;

    // fires when player has enough exchange and is available to level up
    public event Action LevelUpAvailable;

    // this fires when the player selects an upgrade and actually levels up, like select one from three choices
    public event Action<int> LeveledUp;


    public int CurrentXP => currentXP;
    public int CurrentLevel => currentLevel;
    public bool IsLevelUpReady => levelUpReady;
    public int XPToLevelUp => xpToLevelUp;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Add XP to player. If threshold is reaches, automatically trigger the level-up-available event
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;
        XPChanged?.Invoke(currentXP, xpToLevelUp);

        Debug.Log($"[PlayerXP] +{amount} XP  ({currentXP}/{xpToLevelUp})");

        if (!levelUpReady && currentXP >= xpToLevelUp)
        {
            levelUpReady = true;
            LevelUpAvailable?.Invoke();
            Debug.Log("Player Level Up now ready");
        }
    }

    /// <summary>
    /// Called by upgrade system once the player has selected an upgrade from the three its provided. Reset XP, transfer overflow 
    /// and increase the level
    /// </summary>
    public void ConsumeLevelUp()
    {
        if (!levelUpReady) return;

        currentXP -= xpToLevelUp;
        if (currentXP < 0) currentXP = 0;

        currentLevel++; // LEVEL UP
        levelUpReady = true; // upgrade can be selected now

        LeveledUp?.Invoke(currentLevel);
        XPChanged?.Invoke(currentXP, xpToLevelUp);
        Debug.Log($"[PlayerXP] Leveled up! Now level {currentLevel} (overflow XP: {currentXP})");

        if(currentXP >= xpToLevelUp)
        {
            levelUpReady = true;
            LevelUpAvailable?.Invoke();
        }
    }

    /// <summary>
    /// Reset all values - XP and levels for the new run
    /// </summary>
    public void ResetForNewRun()
    {
        currentXP = 0;
        currentLevel = 0;
        levelUpReady = false;

        XPChanged?.Invoke(currentXP, xpToLevelUp);
        Debug.Log("Reset XP and Level");
    }




}
