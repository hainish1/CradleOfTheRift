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
    private Coroutine activeCoroutine;
    private Color cachedLightningColor;
    
    private const float StrikeHeight = 10f;
    private const float StrikeRadius = 2f;

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
        
        cachedLightningColor = LightningCore.CalculateLightningColor();
        CreateWarningVfx();
        activeCoroutine = StartCoroutine(LightningStrikeCoroutine());
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
            float pulse = Mathf.Sin(timer * 8f) * 0.5f + 0.5f;
            markLight.intensity = 2f + pulse * 2f;
            markLight.color = cachedLightningColor;
        }
    }

    private void CreateWarningVfx()
    {
        if (targetEnemy == null) return;
        
        Vector3 strikePos = targetEnemy.transform.position;
        strikePos.y += 0.1f;
        
        GameObject warningObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        warningObj.name = "LightningWarning";
        warningObj.transform.position = strikePos;
        warningObj.transform.localScale = Vector3.one * 0.5f;
        warningObj.transform.SetParent(transform);

        var warningCollider = warningObj.GetComponent<Collider>();
        if (warningCollider != null) Destroy(warningCollider);

        var renderer = warningObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = cachedLightningColor;
            renderer.material = mat;
        }
    }

    private IEnumerator LightningStrikeCoroutine()
    {
        yield return new WaitForSeconds(delayTime);

        if (targetEnemy != null)
        {
            var damageable = targetEnemy.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                float lightningDamage = damage * damageMultiplier;
                
                CreateLightningStrikeVfx(targetEnemy.transform.position);
                
                LightningCore.ApplyLightningDamage(attacker, targetEnemy, lightningDamage);
                
                var flash = targetEnemy.GetComponentInChildren<TargetFlash>();
                if (flash != null) flash.Flash();
                
                yield return new WaitForSeconds(0.35f);
                
                LightningCore.CreateTreeChain(
                    owner: attacker,
                    startEnemy: targetEnemy,
                    baseDamage: lightningDamage * 0.6f,
                    maxDepth: 2,
                    branchesPerNode: 2,
                    range: 10f,
                    damageDecayPerDepth: 0.75f,
                    vfxDuration: 0.4f,
                    skipRootProcessing: true
                );
            }
        }

        Destroy(gameObject);
    }

    private void CreateLightningStrikeVfx(Vector3 position)
    {
        Vector3 startPos = position + Vector3.up * StrikeHeight;
        Vector3 endPos = position;

        GameObject startObj = new GameObject("LightningStart");
        startObj.transform.position = startPos;

        GameObject endObj = new GameObject("LightningEnd");
        endObj.transform.position = endPos;

        LightningCore.CreateLightningVFX(
            startObj.transform,
            endObj.transform,
            StrikeHeight,
            0.3f,
            null,
            0f,
            0f,
            0.15f
        );

        Destroy(startObj, 0.4f);
        Destroy(endObj, 0.4f);
    }

    void OnDestroy()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }
}

