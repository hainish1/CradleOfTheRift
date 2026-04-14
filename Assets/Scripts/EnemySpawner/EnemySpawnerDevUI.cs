using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class EnemySpawnerUI : MonoBehaviour
{
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private EnemySpawner_2 spawner_2;
    [SerializeField] private DifficultyScaler difficultyScaler;

    private Label currentEnemyCountLabel;
    private Label currentCreditsLabel;
    private Label currentMaxEnemyCapLabel;
    private Label currentWaveLabel;
    private Label difficultyScaleLabel;
    private VisualElement devContainer;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        this.devContainer = root.Q<VisualElement>("SpawnerDevHUDRoot");

        this.currentEnemyCountLabel = root.Q<Label>("CurrentEnemyCount");
        this.currentCreditsLabel = root.Q<Label>("CurrentCredits");
        this.currentMaxEnemyCapLabel = root.Q<Label>("CurrentMaxEnemyCap");
        this.currentWaveLabel = root.Q<Label>("CurrentWave");
        this.difficultyScaleLabel = root.Q<Label>("DifficultyScale");

        // Logic for Original Spawner
        if (this.spawner != null)
        {
            this.spawner.CurrentEnemyCountChanged += OnCurrentEnemyCountChanged;
            this.spawner.CurrentCreditsChanged += OnCurrentCreditsChanged;
            this.spawner.CurrentMaxEnemyCapChanged += OnCurrentMaxEnemyCapChanged;
            this.spawner.CurrentWaveChanged += OnCurrentWaveChanged;
            this.spawner.DevModeChanged += OnDevModeChanged;

            OnDevModeChanged(this.spawner.IsDevModeEnabled);        
        }
        // Logic for New Node Spawner
        else if (this.spawner_2 != null)
        {
            this.spawner_2.CurrentEnemyCountChanged += OnCurrentEnemyCountChanged;
            this.spawner_2.CurrentCreditsChanged += OnCurrentCreditsChanged;
            this.spawner_2.CurrentMaxEnemyCapChanged += OnCurrentMaxEnemyCapChanged;
            this.spawner_2.CurrentWaveChanged += OnCurrentWaveChanged;
            this.spawner_2.DevModeChanged += OnDevModeChanged;

            OnDevModeChanged(this.spawner_2.IsDevModeEnabled);
        }
        else
        {
            // Debug.LogError("EnemySpawnerUI: No spawner assigned in the inspector!");
            Debug.Log("Enemy Spawner is not assigned");
        }
    }

    void Update()
    {
        if (difficultyScaler != null && difficultyScaleLabel != null)
        {
            float scale = difficultyScaler.GetDifficultyScale();
            this.difficultyScaleLabel.text = $"Difficulty Scale: {scale:F2}";
        }
    }

    private void OnCurrentEnemyCountChanged(int currentChange) {
        this.currentEnemyCountLabel.text = $"Current Enemy Count: {currentChange}";
    }

    private void OnCurrentCreditsChanged(float currentChange) {
        this.currentCreditsLabel.text =  $"Current Credits: {currentChange:F3}";
    }

    private void OnCurrentMaxEnemyCapChanged(int currentChange) {
        this.currentMaxEnemyCapLabel.text =  $"Current Max Enemy Cap: {currentChange}";
    }

    private void OnCurrentWaveChanged(int currentChange) {
        this.currentWaveLabel.text = $"Current Wave: {currentChange}";
    }

    private void OnDevModeChanged(bool devMode)
    {
        if (devContainer != null)
        {
            devContainer.style.display = devMode ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}