using System.Collections;
using UnityEngine;

public class DelayedDamageMark : MonoBehaviour
{
    private Enemy targetEnemy;
    private float damage;
    private Entity attacker;
    private float delayTime;
    private float damageMultiplier;
    private Light markLight;
    private ParticleSystem particles;
    private float timer;

    public void Init(Enemy enemy, float damageAmount, Entity attackerEntity, float delay, float multiplier)
    {
        targetEnemy = enemy;
        damage = damageAmount;
        attacker = attackerEntity;
        delayTime = delay;
        damageMultiplier = multiplier;
        timer = 0f;

        transform.SetParent(enemy.transform);
        transform.localPosition = Vector3.zero;

        StartCoroutine(DelayedDamageCoroutine());
    }

    public void SetLight(Light light)
    {
        markLight = light;
    }

    public void SetParticles(ParticleSystem ps)
    {
        particles = ps;
    }

    void Update()
    {
        if (markLight != null)
        {
            timer += Time.deltaTime;
            float pulse = Mathf.Sin(timer * 4f) * 0.5f + 0.5f;
            markLight.intensity = 1.5f + pulse * 1.5f;
        }
    }

    private IEnumerator DelayedDamageCoroutine()
    {
        yield return new WaitForSeconds(delayTime);

        if (targetEnemy != null)
        {
            var damageable = targetEnemy.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                float delayedDamage = damage * damageMultiplier;
                damageable.TakeDamage(delayedDamage);
                CombatEvents.ReportDamage(attacker, targetEnemy, delayedDamage);
                
                var flash = targetEnemy.GetComponentInChildren<TargetFlash>();
                if (flash != null) flash.Flash();
            }
        }

        Destroy(gameObject);
    }
}

