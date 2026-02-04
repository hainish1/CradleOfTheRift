using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class CompassManager : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float maxVisibleAngle = 90f; 

    [SerializeField] private UIDocument uiDocument;
    private VisualElement iconContainer;


    [Header("Hybrid Settings")]
    [SerializeField] private float maxDistance = 100f; // Distance where marker is smallest/faded
    [SerializeField] private float minScale = 0.6f;     // Smallest icon size

    [Header("Stacking Settings")]
    [SerializeField] private float horizontalThreshold = 35f; // Pixels apart before stacking
    [SerializeField] private float verticalOffset = 25f;      // How high to jump
    [SerializeField] private float defaultTop = 9f;           // Original 'top' from USS
    [SerializeField] private float stackUpdateInterval = 0.1f; // 10 updates per second


    [Header("Data Settings")]
    [Tooltip("Assign the CompassMarkerData assets here.")]
    [SerializeField] private List<CompassMarkerData> markerDefinitions;


    private Dictionary<CompassMarker, VisualElement> markerMap = new Dictionary<CompassMarker, VisualElement>();

    // Quick lookup to map MarkerType -> USS Class name
    private Dictionary<MarkerType, string> typeToClassMap = new Dictionary<MarkerType, string>();
    private Dictionary<MarkerType, float> typeToRadiusMap = new Dictionary<MarkerType, float>();
    private float stackTimer;

    void OnEnable()
    {
        CompassMarker.OnMarkerAdded += AddMarkerUI;
        CompassMarker.OnMarkerRemoved += RemoveMarkerUI;
    }

    void OnDisable()
    {
        CompassMarker.OnMarkerAdded -= AddMarkerUI;
        CompassMarker.OnMarkerRemoved -= RemoveMarkerUI;
    }

    void Awake()
    {
        // Get the UI container from UXML where markers will be spawned
        var root = uiDocument.rootVisualElement;
        iconContainer = root.Q<VisualElement>("icon-container");

        // Initialize the dictionary for fast lookups
        foreach (var data in markerDefinitions)
        {
            if (data != null && !typeToClassMap.ContainsKey(data.markerType))
            {
                typeToClassMap.Add(data.markerType, data.ussClass);
                typeToRadiusMap.Add(data.markerType, data.detectionRadius);
            }
        }
    }

    void Start()
    {
        // Initialize markers already in the world
        foreach (var marker in CompassMarker.AllMarkers)
        {
            AddMarkerUI(marker);
        }
    }

    void AddMarkerUI(CompassMarker marker)
    {
        if (iconContainer == null || markerMap.ContainsKey(marker)) return;

        // Create a new UI element and apply the base "marker" styling
        var element = new VisualElement();
        element.AddToClassList("marker");

        // Add the Distance Label
        var distanceLabel = new Label();
        distanceLabel.AddToClassList("marker-distance-text");
        element.Add(distanceLabel);

        // Dynamic lookup using the ScriptableObject data
        if (typeToClassMap.TryGetValue(marker.Type, out string className))
        {
            element.AddToClassList(className);
        }
        else
        {
            // Default fallback if no data asset is found for this type
            element.AddToClassList("marker"); 
        }
    
        iconContainer.Add(element);
        markerMap.Add(marker, element);
    }

    void RemoveMarkerUI(CompassMarker marker)
    {
        if (markerMap.TryGetValue(marker, out VisualElement element))
        {
            // Remove from the UI hierarchy
            element.RemoveFromHierarchy();

            // Remove from our internal tracking dictionary
            markerMap.Remove(marker);
        }
    }

    void Update()
    {
        if (playerTransform == null || iconContainer == null) return;

        float containerWidth = iconContainer.resolvedStyle.width;
        float centerX = containerWidth / 2;

        foreach (var pair in markerMap)
        {
            CompassMarker marker = pair.Key;
            VisualElement uiElement = pair.Value;
            Label dLabel = uiElement.Q<Label>();

            // Calculate world-space vector and total distance to target
            Vector3 offset = marker.transform.position - playerTransform.position;
            float distance = offset.magnitude;

            // Retrieve the specific detection range for this marker type
            float maxRadius = 9999f;
            typeToRadiusMap.TryGetValue(marker.Type, out maxRadius);


            // Calculate horizontal angle between player forward and the target
            Vector3 dirToMarker = offset;
            dirToMarker.y = 0; 
            float angle = Vector3.SignedAngle(playerTransform.forward, dirToMarker, Vector3.up);

            // Hide if too far away OR if outside the FOV angle
            if (distance > maxRadius || Mathf.Abs(angle) > maxVisibleAngle)
            {
                uiElement.style.display = DisplayStyle.None;
            }
            else
            {
                uiElement.style.display = DisplayStyle.Flex;

                // Fade out as the marker approaches its maximum detection range
                float proximityAlpha = 1.0f - Mathf.Clamp01(distance / maxRadius);

                // Calculate generic distance-based fade (maxes at 50% transparency)
                float normalizedDist = Mathf.Clamp01(distance / maxDistance);
                float distanceAlpha = 1.0f - (normalizedDist * 0.5f);

                // Combine both alphas to ensure a smooth transition into visibility
                uiElement.style.opacity = distanceAlpha * proximityAlpha;

                // Shrink the icon size based on its distance from the player
                float scale = Mathf.Lerp(1.0f, minScale, normalizedDist);
                uiElement.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));

                // Update Distance Text
                if (dLabel != null) dLabel.text = $"{(int)distance}m";
                
                // Position on Compass
                float normalizedAngle = angle / maxVisibleAngle; 
                float posX = centerX + (normalizedAngle * centerX);
                uiElement.style.left = posX - (uiElement.resolvedStyle.width / 2);
            }
        }

        // STACKING & SORTING (Throttled - 10x per second)
        stackTimer += Time.deltaTime;
        if (stackTimer >= stackUpdateInterval)
        {
            UpdateStackingOrder();
            stackTimer = 0f;
        }
    }

    void UpdateStackingOrder()
    {
        // Reset vertical alignment for visible markers before re-calculating overlaps
        foreach (var pair in markerMap)
            {
                if (pair.Value.resolvedStyle.display == DisplayStyle.Flex)
                {
                    pair.Value.style.top = defaultTop;
                }
            }

            // Filter visible markers and sort by screen-space X position to check neighbors
            var visibleByPos = markerMap.Keys
                .Where(m => markerMap[m].resolvedStyle.display == DisplayStyle.Flex)
                .OrderBy(m => markerMap[m].resolvedStyle.left)
                .ToList();

        // Check adjacent markers; if they overlap horizontally, offset the one further away
        for (int i = 1; i < visibleByPos.Count; i++)
        {
            VisualElement currentUI = markerMap[visibleByPos[i]];
            VisualElement prevUI = markerMap[visibleByPos[i - 1]];

            if (Mathf.Abs(currentUI.resolvedStyle.left - prevUI.resolvedStyle.left) < horizontalThreshold)
            {
                float distA = Vector3.Distance(playerTransform.position, visibleByPos[i].transform.position);
                float distB = Vector3.Distance(playerTransform.position, visibleByPos[i-1].transform.position);

                if (distA > distB) currentUI.style.top = defaultTop - verticalOffset;
                else prevUI.style.top = defaultTop - verticalOffset;
            }
        }

        // Sort by physical distance and reorder the UI hierarchy so closest icons draw on top
        var sortedByDist = visibleByPos
            .OrderByDescending(m => Vector3.Distance(playerTransform.position, m.transform.position))
            .ToList();

        foreach (var m in sortedByDist) markerMap[m].BringToFront();
    }
}