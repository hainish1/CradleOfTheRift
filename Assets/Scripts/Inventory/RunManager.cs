using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Scene flow")]
    [SerializeField] private string gameplaySceneName = "";
    [SerializeField] private bool resetOnGameplaySceneLoad = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Runs once on initial scene startup
        ResetRunStateAndClearInventoryIfPresent();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!resetOnGameplaySceneLoad) return;

        if (!string.IsNullOrWhiteSpace(gameplaySceneName) && scene.name != gameplaySceneName)
            return;

        ResetRunStateAndClearInventoryIfPresent();
    }

    private void ResetRunStateAndClearInventoryIfPresent()
    {
        RunLootState.Instance?.ResetForNewRun();
        UpgradeLevelManager.Instance?.ResetForNewRun();

        var inv = FindFirstObjectByType<PlayerInventory>();
        if (inv != null) inv.Clear();

        var ruleRunner = FindFirstObjectByType<InventoryRuleRunner>();
        if (ruleRunner != null) ruleRunner.ResetForNewRun();
    }

}
