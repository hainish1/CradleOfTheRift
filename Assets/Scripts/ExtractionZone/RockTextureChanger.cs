using UnityEngine;
using System;

public class RockTextureChanger : MonoBehaviour
{
    [Serializable]
    public struct RockEvent
    {
        public Renderer rockRenderer;
        public int eventFrame;
    }

    public RockEvent[] rockEvents; 

    [Tooltip("The new material you want to apply once the rock stops.")]
    public Material finishedMaterial;

    [Tooltip("The material to reset to if the progress bar goes backwards")]
    public Material resetMaterial; 
    public int totalFrames = 500; 

    private float[] cachedThresholds;
    public float[] GetCachedThresholds() => cachedThresholds;

    private void Start()
    {
        cachedThresholds = new float[rockEvents.Length];
        for (int i = 0; i < cachedThresholds.Length; i++)
        {
            cachedThresholds[i] = (float)rockEvents[i].eventFrame / totalFrames;
        }
    }

    public void ChangeRockMaterial(int rockIndex)
    {
        // Check to make sure we don't pass a bad number
        if (rockIndex >= 0 && rockIndex < rockEvents.Length)
        {
            // rockRenderers[rockIndex].material = finishedMaterial;
            rockEvents[rockIndex].rockRenderer.material = finishedMaterial;
        }
        else
        {
            Debug.LogWarning("Rock index out of bounds! Check your Animation Event parameter.");
        }
    }

    public void ResetRockMaterial(int rockIndex)
    {
        // Check to make sure we don't pass a bad number
        if (rockIndex >= 0 && rockIndex < rockEvents.Length)
        {
            // rockRenderers[rockIndex].material = resetMaterial;
            rockEvents[rockIndex].rockRenderer.material = resetMaterial;
        }
        else
        {
            Debug.LogWarning("Rock index out of bounds! Check your Animation Event parameter.");
        }
    }
}