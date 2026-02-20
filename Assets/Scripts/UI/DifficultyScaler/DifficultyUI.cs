using UnityEngine;
using UnityEngine.UIElements;

public class DifficultyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DifficultyScaler difficultyScaler;

    [Header("UI Settings")]
    [Tooltip("How many pixels represent 1 second of time")]
    [SerializeField] private float pixelsPerSecond = 5f;

    private VisualElement difficultyStrip;
    private VisualElement marker;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        difficultyStrip = root.Q<VisualElement>("DifficultyStrip");
        marker = root.Q<VisualElement>("DifficultyMarker");
        difficultyStrip.Clear();

        var tiers = difficultyScaler.GetTiers();
        for (int i = 0; i < tiers.Count; i++)
        {
            AddColoredSegment(tiers[i].tierColor, tiers[i].tierName, tiers[i].duration);
        }

        marker.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            if (difficultyScaler != null)
                difficultyScaler.OnDifficultyUIUpdate += HandleUIUpdate;
        });
    }

private float totalStripWidth = 0f;

private void HandleUIUpdate(float tierProgress, string currentTierName)
{
    float markerOffset = marker.layout.x;
    float translation = markerOffset - (difficultyScaler.elapsedTime * pixelsPerSecond);
    difficultyStrip.style.translate = new Translate(translation, 0, 0);

    // The visible right edge of the window in strip-space
    float visibleRightEdge = difficultyScaler.elapsedTime * pixelsPerSecond + markerOffset;

    // Keep generating last-tier segments when we're within 2 segment-widths of the end
    var tiers = difficultyScaler.GetTiers();
    var lastTier = tiers[tiers.Count - 1];
    float lastSegmentWidth = lastTier.duration * pixelsPerSecond;

    while (visibleRightEdge + lastSegmentWidth * 2f > totalStripWidth)
    {
        AddColoredSegment(lastTier.tierColor, lastTier.tierName, lastTier.duration);
    }
}

private void AddColoredSegment(Color color, string tierName, float duration)
{
    float minWidth = 125;
    float naturalWidth = duration * pixelsPerSecond;
    float segmentWidth = Mathf.Max(naturalWidth, minWidth);

    VisualElement segment = new VisualElement();
    segment.AddToClassList("difficulty-segment");
    segment.style.width = segmentWidth;
    segment.style.backgroundColor = color;

    Label label = new Label(tierName);
    label.AddToClassList("difficulty-text");
    segment.Add(label);

    difficultyStrip.Add(segment);
    totalStripWidth += segmentWidth;
}
}