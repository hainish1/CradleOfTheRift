using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Class - Used to make the orbit like visuals for Range Enemy
/// </summary>
public class RevOrbitVisuals : MonoBehaviour
{
    [Header("Projectiles Settings")]
    public GameObject orbitVisual;
    [SerializeField] private int numOfOrbs = 4;
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitSpeed = 20;
    [SerializeField] private float orbitHeight = 1.5f;

    private int hiddenOrbs = 0;

    private Transform[] transforms;

    void Start()
    {
        transforms = new Transform[numOfOrbs];
        for (int i = 0; i < numOfOrbs; i++)
        {
            GameObject proj = Instantiate(orbitVisual, transform);
            proj.transform.localScale = Vector3.one * 0.25f;
            transforms[i] = proj.transform;
        }
    }

    /// <summary>
    /// Set the angle step for orbs, and then move and rotate then in reference to the enemy
    /// </summary>
    void Update()
    {
        float angleStep = 360f / numOfOrbs;
        if (transforms.Length > 0)
        {
            for (int i = 0; i < numOfOrbs; i++)
            {
                // angle for this orb
                float angle = (Time.time * orbitSpeed) + (i * angleStep);
                float rad = angle * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * orbitRadius;
                offset += Vector3.up * orbitHeight;

                transforms[i].localPosition = offset;
            }
        }
    }

    /// <summary>
    /// Hide the orb once it is shot,
    /// </summary>
    /// <param name="index"></param>
    public void HideOrb(int index)
    {
        if (index < 0 || index >= transforms.Length || transforms[index] == null) return;
        if (!transforms[index].gameObject.activeSelf) return; // alr hidden
        transforms[index].gameObject.SetActive(false);
        hiddenOrbs++;

        if (hiddenOrbs == transforms.Length) // all are used, time to reenable again
        {
            StartCoroutine(ReEnableOrbsAfterDelay());
        }
    }

    /// <summary>
    /// After all orbs are shot, re-enable them
    /// </summary>
    /// <returns></returns>
    private IEnumerator ReEnableOrbsAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].gameObject.SetActive(true);
        }
        hiddenOrbs = 0; // set fresh
    }

    /// <summary>
    /// Get the index of next orb that needs disabling/enabling. If none are available, then ReEnable all
    /// </summary>
    /// <returns></returns>
    public int GetNextVisibleOrbIndex()
    {
        for (int i = 0; i < transforms.Length; i++)
            if (transforms[i] != null && transforms[i].gameObject.activeSelf)
                return i;
        return -1; // None available
    }

}
