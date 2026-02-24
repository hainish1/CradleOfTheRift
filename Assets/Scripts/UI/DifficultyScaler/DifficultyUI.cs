using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DifficultyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DifficultyScaler difficultyScaler;

    [Header("UI Settings")]
    [Tooltip("How many pixels represent 1 second of time")]
    [SerializeField] private float pixelsPerSecond = 5f;

    [Tooltip("Minimum width in pixels for any segment, regardless of duration")]
    [SerializeField] private float minSegmentWidth = 80f;

    private VisualElement difficultyStrip;
    private VisualElement marker;

    private float totalStripWidth = 0f;

    // Tracks every segment's duration and actual rendered width so we can
    // correctly map elapsedTime -> scroll position even when widths are clamped.
    private struct SegmentInfo
    {
        public float duration;
        public float pixelWidth;
    }
    private List<SegmentInfo> segmentInfos = new List<SegmentInfo>();

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

    private void HandleUIUpdate(float tierProgress, string currentTierName)
    {
        float markerOffset = marker.layout.x;
        float scrollPos = GetScrollPositionForTime(difficultyScaler.elapsedTime);
        difficultyStrip.style.translate = new Translate(markerOffset - scrollPos, 0, 0);

        // The visible right edge of the window in strip-space
        float visibleRightEdge = scrollPos + markerOffset;

        // Keep generating last-tier segments when we're within 2 segment-widths of the end
        var tiers = difficultyScaler.GetTiers();
        var lastTier = tiers[tiers.Count - 1];
        float lastSegmentWidth = Mathf.Max(lastTier.duration * pixelsPerSecond, minSegmentWidth);

        while (visibleRightEdge + lastSegmentWidth * 2f > totalStripWidth)
        {
            AddColoredSegment(lastTier.tierColor, lastTier.tierName, lastTier.duration);
        }
    }

    /// <summary>
    /// Converts a game-time value into a pixel offset along the strip,
    /// respecting clamped segment widths.
    /// </summary>
    private float GetScrollPositionForTime(float time)
    {
        float remainingTime = time;
        float pixelPos = 0f;

        foreach (var seg in segmentInfos)
        {
            if (remainingTime <= 0f) break;

            if (remainingTime >= seg.duration)
            {
                // Fully past this segment
                pixelPos += seg.pixelWidth;
                remainingTime -= seg.duration;
            }
            else
            {
                // Partially through this segment — interpolate linearly within it
                float progress = remainingTime / seg.duration;
                pixelPos += seg.pixelWidth * progress;
                remainingTime = 0f;
            }
        }

        // If elapsedTime exceeds all defined segments (looping last tier),
        // the remaining time maps linearly using the last segment's effective rate.
        if (remainingTime > 0f && segmentInfos.Count > 0)
        {
            var last = segmentInfos[segmentInfos.Count - 1];
            float effectivePPS = last.pixelWidth / last.duration;
            pixelPos += remainingTime * effectivePPS;
        }

        return pixelPos;
    }

    private void AddColoredSegment(Color color, string tierName, float duration)
    {
        float naturalWidth = duration * pixelsPerSecond;
        float segmentWidth = Mathf.Max(naturalWidth, minSegmentWidth);

        VisualElement segment = new VisualElement();
        segment.AddToClassList("difficulty-segment");
        segment.style.width = segmentWidth;
        segment.style.backgroundColor = color;

        Label label = new Label(tierName);
        label.AddToClassList("difficulty-text");
        segment.Add(label);

        difficultyStrip.Add(segment);
        totalStripWidth += segmentWidth;
        segmentInfos.Add(new SegmentInfo { duration = duration, pixelWidth = segmentWidth });
    }
}
