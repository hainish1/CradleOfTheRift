using System.Collections;
using UnityEngine;

/// <summary>
/// Class : Used to flash a GameObject with some color when its hit
/// </summary>
public class TargetFlash : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material original;
    [SerializeField] private Material flash;
    [SerializeField] private float flashDuration = .1f;

    private Renderer rend;
    private Coroutine flashRoutine;


    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            rend = GetComponentInChildren<Renderer>();
        }

        if (rend != null && original != null)
        {
            rend.material = original;
        }
    }

    /// <summary>
    /// Start Flash coroutine
    /// </summary>
    public void Flash()
    {
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
        if (rend != null && flash != null)
            rend.material = flash;
        yield return new WaitForSeconds(flashDuration);
        if (rend != null && original != null)
            rend.material = original;
    }

}
