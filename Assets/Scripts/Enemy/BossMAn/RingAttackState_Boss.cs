using System.Collections.Generic;
using UnityEngine;

public class RingAttackState_Boss : EnemyState
{
    private EnemyBoss_SS boss;
    private float duration;
    private float elapsed;
    private float maxRadius;
    private float damage;
    private LayerMask playerMask;
    private HashSet<IDamageable> alreadyDamaged = new HashSet<IDamageable>();
    private GameObject explosionVFXObj;
    private BossRingVFX bossRingVFX;
    private GameObject shockwaveVFX;

    public RingAttackState_Boss(Enemy enemy, EnemyStateMachine stateMachine, float maxRadius, float duration, float damage, LayerMask mask) : base(enemy, stateMachine)
    {
        this.boss = enemy as EnemyBoss_SS;
        this.maxRadius = maxRadius;
        this.duration = duration;
        this.damage = damage;
        this.playerMask = mask;
    }

    public override void Enter()
    {
        base.Enter();
        elapsed = 0f;
        alreadyDamaged = new HashSet<IDamageable>();

        if(boss.explosionVFXPrefab != null)
        {
            explosionVFXObj = GameObject.Instantiate(boss.explosionVFXPrefab);
            explosionVFXObj.transform.position = boss.transform.position - new Vector3(0, 0, 0);
            explosionVFXObj.transform.rotation = Quaternion.identity;

            bossRingVFX = explosionVFXObj.GetComponent<BossRingVFX>();
        }
    }


    public override void Update()
    {
        base.Update();
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        float radius = Mathf.Lerp(0.5f, maxRadius, t);

        if (bossRingVFX != null)
        {
            bossRingVFX.SetRadius(radius);
        }

        if (elapsed >= duration)
        {
            
            Collider[] hits = Physics.OverlapSphere(boss.transform.position, radius, playerMask);
            foreach (var col in hits)
            {
                var dmg = col.GetComponentInParent<IDamageable>();
                if (dmg != null && !dmg.IsDead && !alreadyDamaged.Contains(dmg))
                {
                    var player = col.GetComponentInParent<PlayerMovement>();
                    var cameraShake = col.GetComponentInParent<PlayerGroundSlam>(); // idk I haven't foudn a better solution yet
                    if (player != null)
                    {
                        CauseDamage(dmg);
                        cameraShake?.CreateCameraShake(boss.explosionCameraShakeForce);
                    }
                }
            }


            if (explosionVFXObj != null) GameObject.Destroy(explosionVFXObj);
            if(boss.shockwaveVFXPrefab != null)
            {
                shockwaveVFX = GameObject.Instantiate(boss.shockwaveVFXPrefab);
                shockwaveVFX.transform.position = boss.transform.position - new Vector3(0, 0, 0);
                shockwaveVFX.transform.rotation = Quaternion.identity;
            }

            if(shockwaveVFX != null) GameObject.Destroy(shockwaveVFX, 2);
            stateMachine.ChangeState(boss.GetRecoveryState());
        }
    }
    

    private void CauseDamage(IDamageable dmg)
    {
        dmg.TakeDamage(damage);
        alreadyDamaged.Add(dmg);
    }
}
