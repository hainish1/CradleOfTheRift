using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyRangeOrbitVisuals : MonoBehaviour
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
            transforms[i] = proj.transform;
        }
    }

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
    private IEnumerator ReEnableOrbsAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        // if (index > 0 || index < transforms.Length || transforms[index] != null)
        // {
        //     transforms[index].gameObject.SetActive(true);
        // }
        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].gameObject.SetActive(true);
        }
        hiddenOrbs = 0; // set fresh
    }
    public int GetNextVisibleOrbIndex()
    {
        for (int i = 0; i < transforms.Length; i++)
            if (transforms[i] != null && transforms[i].gameObject.activeSelf)
                return i;
        return -1; // None available
    }
    
}
