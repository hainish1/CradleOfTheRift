using UnityEngine;

public class BossRingVFX : MonoBehaviour
{
    public void SetRadius(float radius)
    {
        transform.localScale = new Vector3(radius * 2f, radius * 2f, radius * 2f);
    }
}
