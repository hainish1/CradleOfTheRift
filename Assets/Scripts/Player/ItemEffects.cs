using System;
using UnityEngine;

public class ItemEffects : MonoBehaviour
{
    [SerializeField] private bool activateHomingProjectiles = false;
    [SerializeField] private float homingEffectCooldown = 3f;

    private float timer;

    void Start()
    {
        timer = homingEffectCooldown;
    }

    void FixedUpdate()
    {
        timer -= Time.fixedDeltaTime;

        if (timer < 0f)
        {
            spawnProjectiles();
            timer = homingEffectCooldown;
        }
    }

    void spawnProjectiles()
    {
        Debug.Log("Spawning homing projectiles");
    }
}
