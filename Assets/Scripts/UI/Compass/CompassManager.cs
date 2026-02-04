using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CompassManager : MonoBehaviour
{
    [SerializeField]
    private Transform playerTransform;
    [SerializeField]
    private float maxVisibleAngle = 90f; 

    [SerializeField]
    private UIDocument uiDocument;
    private VisualElement iconContainer;

    [Header("Data Settings")]
    [Tooltip("Assign the CompassMarkerData assets here.")]
    [SerializeField] private List<CompassMarkerData> markerDefinitions;

    private Dictionary<CompassMarker, VisualElement> markerMap = new Dictionary<CompassMarker, VisualElement>();
    
    // Quick lookup to map MarkerType -> USS Class name
    private Dictionary<MarkerType, string> typeToClassMap = new Dictionary<MarkerType, string>();

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

            // Calculate horizontal angle between player forward and the target
            Vector3 dirToMarker = marker.transform.position - playerTransform.position;
            dirToMarker.y = 0; 
            
            float angle = Vector3.SignedAngle(playerTransform.forward, dirToMarker, Vector3.up);

            // Toggle visibility based on whether the marker is within the visible FOV
            if (Mathf.Abs(angle) > maxVisibleAngle)
            {
                uiElement.style.display = DisplayStyle.None;
            }
            else
            {
                uiElement.style.display = DisplayStyle.Flex;
                
                // Map the angle to a horizontal pixel position relative to the center
                float normalizedAngle = angle / maxVisibleAngle; 
                float posX = centerX + (normalizedAngle * centerX);
                
                // Apply the position, centering the icon on the calculated pixel
                uiElement.style.left = posX - (uiElement.resolvedStyle.width / 2);
            }
        }
    }
}