using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class EnemySpawnerUI : MonoBehaviour
{
    [SerializeField] private EnemySpawner spawner;
    private Label currentEnemyCountLabel;
    private Label currentCreditsLabel;
    private Label currentMaxEnemyCapLabel;
    private Label currentWaveLabel;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        this.currentEnemyCountLabel = root.Q<Label>("CurrentEnemyCount");
        this.currentCreditsLabel = root.Q<Label>("CurrentCredits");
        this.currentMaxEnemyCapLabel = root.Q<Label>("CurrentMaxEnemyCap");
        this.currentWaveLabel = root.Q<Label>("CurrentWave");

        this.spawner.CurrentEnemyCountChanged += OnCurrentEnemyCountChanged;
        this.spawner.CurrentCreditsChanged += OnCurrentCreditsChanged;
        this.spawner.CurrentMaxEnemyCapChanged += OnCurrentMaxEnemyCapChanged;
        this.spawner.CurrentWaveChanged += OnCurrentWaveChanged;
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
        this.currentWaveLabel.text =  $"Current Wave: {currentChange}";
    }
}
