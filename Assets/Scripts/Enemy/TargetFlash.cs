using System.Collections;
using UnityEngine;

/// <summary>
/// Class : Used to flash a GameObject with some color when its hit
/// </summary>
[DisallowMultipleComponent]
public class TargetFlash : MonoBehaviour
{
    [SerializeField] private Material flash;
    [SerializeField] private float flashDuration = .1f;

    [Tooltip("if false every Renderer under this transform flashes")]
    [SerializeField] private bool flashOnlyRootRenderer = false;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Coroutine flashRoutine;

    void Awake()
    {
        GetRenderers();
    }

    public void GetRenderers()
    {
        if (flashOnlyRootRenderer)
        {
            var single = GetComponent<Renderer>();
            if (single == null) single = GetComponentInChildren<Renderer>();
            renderers = single != null ? new[] { single } : System.Array.Empty<Renderer>();
        }
        else
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
        }
    }

    /// <summary>
    /// Start Flash coroutine
    /// </summary>
    public void Flash()
    {
        if (!isActiveAndEnabled) return;
        if (flash == null || renderers == null || renderers.Length == 0) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(DoFlash());
    }

    /// <summary>
    /// Set original renderer material to flash materials,
    /// then change back to original after flash duration
    /// </summary>
    /// <returns></returns>
    private IEnumerator DoFlash()
    {
        ApplyFlashMaterial();
        yield return new WaitForSeconds(flashDuration);
        RestoreOriginalMaterials();
        flashRoutine = null;
    }

    private void ApplyFlashMaterial()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            int slots = originalMaterials[i] != null ? originalMaterials[i].Length : 1;
            var flashArr = new Material[Mathf.Max(1, slots)];
            for (int s = 0; s < flashArr.Length; s++) flashArr[s] = flash;
            r.sharedMaterials = flashArr;
        }
    }

    private void RestoreOriginalMaterials()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null || originalMaterials[i] == null) continue;
            r.sharedMaterials = originalMaterials[i];
        }
    }

    void OnDisable()
    {
        // never leave enemy stuck on the flash 
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        RestoreOriginalMaterials();
    }
}
