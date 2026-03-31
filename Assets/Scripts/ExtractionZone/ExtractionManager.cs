using System;
using System.Collections.Generic;
using UnityEngine;

public class ExtractionManager : MonoBehaviour
{
    public static ExtractionManager Instance;

    [SerializeField] private BossPool bossPool;
    private List<ExtractionZone> allZones = new List<ExtractionZone>();
    private int completedZones = 0;
    private bool isAnyZoneActive = false;
    public event Action<ExtractionZone> ExtractionStarted; 
    public event Action AllExtractionsFinished;

    private bool isExtractionCompleted = false;
    private ExtractionZone currentActiveZone;
    public bool CanStartExtraction() => !isAnyZoneActive;

    [SerializeField] private bool isTutorialMode = false;

    public event Action OnGameWon;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (bossPool != null) bossPool.InitializePool();
    }

    public void RegisterZone(ExtractionZone zone)
    {
        allZones.Add(zone);
        zone.ExtractionInteracted += OnZoneStarted; 
        zone.ExtractionFinished += OnZoneCompleted;
    }

    public BossType GetNextBoss()
    {
        return bossPool.GetUniqueBoss();
    }

    public void OnZoneStarted(ExtractionZone zone)
    {
        if (isAnyZoneActive) return;

        isAnyZoneActive = true;
        currentActiveZone = zone;
        
        // Pass the zone to the UI and Spawner
        ExtractionStarted?.Invoke(currentActiveZone); 
    }

    public void OnZoneCompleted()
    {
        isAnyZoneActive = false;
        currentActiveZone = null;
        completedZones++;

        Debug.Log($"Zone Finished! Completed: {completedZones} | Total in List: {allZones.Count}");
        
        AllExtractionsFinished?.Invoke();

        // ONLY trigger the win if the count matches
        if (completedZones >= allZones.Count && allZones.Count > 0) 
        {
            Debug.Log("Condition met! Triggering Global Win...");
            TriggerGlobalWin();
        }
    }
    private void TriggerGlobalWin()
    {
        isExtractionCompleted = true;

        if (isTutorialMode)
        {
            // let TutorialSceneManager handle the transition
            if (TutorialSceneManager.Instance != null && TutorialSceneManager.Instance.IsComplete)
            {
                var completer = TutorialSceneManager.Instance.GetComponent<TutorialCompleter>();
                if (completer != null) completer.CompleteTutorial();
            }
            return;
        }
        // else its just the normal stuff
        OnGameWon?.Invoke();
    }

    public bool IsExtractionCompleted()
    {
        return isExtractionCompleted;   
    }
    
}