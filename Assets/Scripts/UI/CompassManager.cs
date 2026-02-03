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

    // Pairs world markers with their corresponding UI elements
    private Dictionary<CompassMarker, VisualElement> markerMap = new Dictionary<CompassMarker, VisualElement>();

    void Start()
    {
        // Get the UI container from UXML where markers will be spawned
        var root = uiDocument.rootVisualElement;
        iconContainer = root.Q<VisualElement>("icon-container");

        if (iconContainer == null)
        {
            Debug.LogError("Compass Manager: 'icon-container' not found in UXML. Check your element names!");
            return;
        }

        // Initialize markers already in the world
        foreach (var marker in CompassMarker.AllMarkers)
        {
            AddMarkerUI(marker);
        }
    }

    void AddMarkerUI(CompassMarker marker)
    {
        if (markerMap.ContainsKey(marker)) return;

        // Create a new UI element and apply the base "marker" styling
        var element = new VisualElement();
        element.AddToClassList("marker");
        
        // Add specific icon class based on marker type
        string className = marker.Type switch
        {
            MarkerType.Cave => "icon-cave",
            MarkerType.Extraction => "icon-extraction",
            _ => "marker"
        };
    
        element.AddToClassList(className);
        iconContainer.Add(element);
        markerMap.Add(marker, element);
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