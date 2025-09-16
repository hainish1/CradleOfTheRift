using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class EnemySpawnerUI : MonoBehaviour
{
    [SerializeField] private EnemySpawner spawner;

    private Label maxEnemiesPerWaveLabel;
    private Label enemyWaveCapIncreaseLabel;
    private Label timeBetweenWavesLabel;
    private Label timeBetweenEnemySpawnLabel;
    private Label difficultyScaleLabel;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Grab references to the labels by their UXML name attributes
        maxEnemiesPerWaveLabel     = root.Q<Label>("MaxEnemiesPerWave");
        enemyWaveCapIncreaseLabel  = root.Q<Label>("EnemyWaveCapIncrease");
        timeBetweenWavesLabel      = root.Q<Label>("TimeBetweenWaves");
        timeBetweenEnemySpawnLabel = root.Q<Label>("TimeBetweenEnemySpawn");
        difficultyScaleLabel       = root.Q<Label>("DifficultyScale");
    }

    private void Update()
    {
        if (spawner == null) return;

        // Update UI text every frame (cheap enough for these few fields in early Development)
        maxEnemiesPerWaveLabel.text     = $"MaxEnemiesPerWave: {spawner.GetCurrentMaxEnemiesPerWave}";
        enemyWaveCapIncreaseLabel.text  = $"EnemyWaveCapIncrease: {spawner.GetEnemyWaveCapIncrease}";
        timeBetweenWavesLabel.text      = $"TimeBetweenWaves: {spawner.GetTimeBetweenWaves:F1}";
        timeBetweenEnemySpawnLabel.text = $"TimeBetweenEnemySpawn: {spawner.GetCurrentTimeBetweenEnemySpawns:F1}";
        difficultyScaleLabel.text       = $"DifficultyScale: {spawner.GetDifficultyScale:F2}";
    }
}
