using UnityEngine;

public class RockTextureChanger : MonoBehaviour
{
    public Renderer[] rockRenderers; 

    [Tooltip("The new material you want to apply once the rock stops.")]
    public Material finishedMaterial;

    [Tooltip("The material to reset to if the progress bar goes backwards")]
    public Material resetMaterial; 

    public void ChangeRockMaterial(int rockIndex)
    {
        // Check to make sure we don't pass a bad number
        if (rockIndex >= 0 && rockIndex < rockRenderers.Length)
        {
            rockRenderers[rockIndex].material = finishedMaterial;
        }
        else
        {
            Debug.LogWarning("Rock index out of bounds! Check your Animation Event parameter.");
        }
    }

    public void ResetRockMaterial(int rockIndex)
    {
        // Check to make sure we don't pass a bad number
        if (rockIndex >= 0 && rockIndex < rockRenderers.Length)
        {
            rockRenderers[rockIndex].material = resetMaterial;
        }
        else
        {
            Debug.LogWarning("Rock index out of bounds! Check your Animation Event parameter.");
        }
    }
}