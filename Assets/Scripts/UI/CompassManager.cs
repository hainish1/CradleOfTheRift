using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CompassManager : MonoBehaviour
{
    [Header("Settings")]
    public Transform playerTransform;
    public float maxVisibleAngle = 90f; 

    [Header("UI References")]
    public UIDocument uiDocument;
    private VisualElement iconContainer;

    private Dictionary<CompassMarker, VisualElement> markerMap = new Dictionary<CompassMarker, VisualElement>();

    void Start()
    {
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

        var element = new VisualElement();
        element.AddToClassList("marker"); // [cite: 6]
        
        string className = marker.type == MarkerType.Cave ? "icon-cave" : "icon-extraction";
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

            Vector3 dirToMarker = marker.transform.position - playerTransform.position;
            dirToMarker.y = 0; 
            
            float angle = Vector3.SignedAngle(playerTransform.forward, dirToMarker, Vector3.up);

            // Visibility logic
            if (Mathf.Abs(angle) > maxVisibleAngle)
            {
                uiElement.style.display = DisplayStyle.None;
            }
            else
            {
                uiElement.style.display = DisplayStyle.Flex;
                
                // Angle to Pixel mapping
                float normalizedAngle = angle / maxVisibleAngle; 
                float posX = centerX + (normalizedAngle * centerX);
                
                uiElement.style.left = posX - (uiElement.resolvedStyle.width / 2);
            }
        }
    }
}