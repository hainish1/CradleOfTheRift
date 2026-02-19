using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DifficultyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DifficultyScaler difficultyScaler;
    
    [Header("UI Settings")]
    [SerializeField] private float segmentWidth = 250f;

    [Header("Tier Colors")]
    [SerializeField] private List<Color> tierColors = new List<Color> {
        Color.green,              // EASY
        Color.yellow,             // NORMAL
        new Color(1f, 0.5f, 0f),  // HARD
        Color.red,                // VERY HARD
        new Color(0.5f, 0f, 0f),  // INSANE
        new Color(0.2f, 0f, 0f)   // HAHAHA
    };

    private VisualElement difficultyStrip;
    private int segmentsCreated = 0;

void Start()
{
    VisualElement root = GetComponent<UIDocument>().rootVisualElement;
    difficultyStrip = root.Q<VisualElement>("DifficultyStrip");
    difficultyStrip.Clear();

    for (int i = 0; i < tierColors.Count; i++)
    {
        string name = (i < difficultyScaler.difficultyNames.Length)
            ? difficultyScaler.difficultyNames[i]
            : "???";
        AddColoredSegment(tierColors[i], name);
    }

    difficultyStrip.style.left = 0f;

    if (difficultyScaler != null)
        difficultyScaler.OnDifficultyUIUpdate += HandleUIUpdate;
}

private void HandleUIUpdate(float progressToNextTier, string currentTierName)
{
    float timePerTier = difficultyScaler.timePerDifficultyTier;
    float totalProgress = difficultyScaler.elapsedTime / timePerTier;

    // Calculate the marker's offset. 
    // Since the marker is at (middle), it is at exactly half the segmentWidth.
    float markerOffset = segmentWidth / 2f; 

    // Add the offset to the translation math.
    // At t=0, translation is +125px (Start of segment aligns with marker).
    // At t=30s, translation is -125px (End of first segment aligns with marker).
    float translation = markerOffset - (totalProgress * segmentWidth);
    difficultyStrip.style.translate = new Translate(translation, 0, 0);

    // Generate new segments infinitely as you progress
    if (totalProgress + 2 > segmentsCreated)
    {
        string lastName = difficultyScaler.difficultyNames[difficultyScaler.difficultyNames.Length - 1];
        AddColoredSegment(tierColors[tierColors.Count - 1], lastName);
    }
}
    private void AddColoredSegment(Color color, string tierName)
    {
        VisualElement segment = new VisualElement();
        segment.AddToClassList("difficulty-segment");
        segment.style.backgroundColor = color;

        Label label = new Label(tierName);
        label.AddToClassList("difficulty-text");
        segment.Add(label);

        difficultyStrip.Add(segment);
        segmentsCreated++;
    }
}