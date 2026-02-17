using System;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightningTest : IDisposable
{
    public static bool IsProcessingChain = false;

    private Entity owner;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private float strikeInterval;
    private float nextStrikeTime;
    private float buffDuration;
    private float buffTimer;
    private bool isBuffActive;

    private float treeChainDamagePercent;
    private int maxTreeDepth;
    private int branchesPerNode;
    private float chainRange;
    private LayerMask enemyLayer;

    private GameObject buffArcContainer;
    private GameObject strikeStartObj;
    private GameObject strikeEndObj;
    private GameObject[] arcStartPool;
    private GameObject[] arcEndPool;
    private int arcPoolSize = 4;
    private GameObject strikeWarning;
    
    private float lastChainTime;
    private float nextBuffArcTime;
    private float spiralOffset;
    private const float MinChainInterval = 0.15f;
    private const float BuffArcInterval = 0.15f;
    private const int BuffArcCount = 6;
    private const float BuffArcRadius = 0.4f;
    private const float BuffArcHeight = 1.5f;

    private const float StrikeDelay = 0.3f;
    private const float StrikeHeight = 10f;
    private const float StrikeRadius = 2f;

    public ChainLightningTest(
        Entity owner,
        float strikeInterval,
        float buffDuration,
        float chainDamagePercent,
        int maxDepth,
        int branchesPerNode,
        float chainRange,
        int initialStacks = 1,
        float durationSec = -1f)
    {
        this.owner = owner;
        this.strikeInterval = strikeInterval;
        this.buffDuration = buffDuration;
        this.treeChainDamagePercent = chainDamagePercent;
        this.maxTreeDepth = maxDepth;
        this.branchesPerNode = branchesPerNode;
        this.chainRange = chainRange;
        this.stacks = initialStacks > 0 ? initialStacks : 1;
        this.duration = durationSec;
        this.timer = durationSec;
        this.buffTimer = 0f;
        this.isBuffActive = false;

        enemyLayer = LayerMask.GetMask("Enemy");
        nextStrikeTime = Time.time + strikeInterval;
        
        strikeStartObj = new GameObject("StrikeStart_Pooled");
        strikeEndObj = new GameObject("StrikeEnd_Pooled");
        strikeStartObj.SetActive(false);
        strikeEndObj.SetActive(false);
        
        buffArcContainer = new GameObject("BuffArcContainer");
        buffArcContainer.transform.SetParent(owner.transform);
        buffArcContainer.transform.localPosition = Vector3.zero;
        buffArcContainer.SetActive(false);
        
        arcStartPool = new GameObject[BuffArcCount];
        arcEndPool = new GameObject[BuffArcCount];
        for (int i = 0; i < BuffArcCount; i++)
        {
            arcStartPool[i] = new GameObject($"BuffArcStart_{i}");
            arcEndPool[i] = new GameObject($"BuffArcEnd_{i}");
            arcStartPool[i].transform.SetParent(buffArcContainer.transform);
            arcEndPool[i].transform.SetParent(buffArcContainer.transform);
            arcStartPool[i].SetActive(false);
            arcEndPool[i].SetActive(false);
        }
    }

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0) Dispose();
    }

    public void Update(float dt)
    {
        if (disposed) return;

        if (duration > 0f)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                Dispose();
                return;
            }
        }

        if (Time.time >= nextStrikeTime)
        {
            TriggerStrike();
            nextStrikeTime = Time.time + strikeInterval;
        }

        if (isBuffActive)
        {
            buffTimer -= dt;
            if (buffTimer <= 0f)
            {
                DeactivateBuff();
            }
            else if (Time.time >= nextBuffArcTime)
            {
                SpawnBuffArcs();
                nextBuffArcTime = Time.time + BuffArcInterval;
            }
        }
    }

    private void TriggerStrike()
    {
        Vector3 strikePos = owner.transform.position + UnityEngine.Random.insideUnitSphere * StrikeRadius;
        strikePos.y = owner.transform.position.y;

        CreateWarningVfx(strikePos);
        owner.GetComponent<MonoBehaviour>().StartCoroutine(DelayedStrike(strikePos));
    }

    private System.Collections.IEnumerator DelayedStrike(Vector3 position)
    {
        yield return new UnityEngine.WaitForSeconds(StrikeDelay);
        CreateStrikeVfx(position);
        ActivateBuff();
        if (strikeWarning != null)
        {
            UnityEngine.Object.Destroy(strikeWarning);
            strikeWarning = null;
        }
    }

    private void CreateWarningVfx(Vector3 groundPoint)
    {
        Vector3 startPos = groundPoint + Vector3.up * StrikeHeight;
        strikeWarning = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        strikeWarning.name = "LightningWarning";
        strikeWarning.transform.position = startPos;
        strikeWarning.transform.localScale = Vector3.one * 0.4f;

        var warningCollider = strikeWarning.GetComponent<Collider>();
        if (warningCollider != null) UnityEngine.Object.Destroy(warningCollider);

        var renderer = strikeWarning.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.6f, 0.9f, 1f, 1f);
            renderer.material = mat;
        }

        UnityEngine.Object.Destroy(strikeWarning, StrikeDelay + 0.1f);
    }

    private void CreateStrikeVfx(Vector3 position)
    {
        if (strikeStartObj == null || strikeEndObj == null) return;

        Vector3 startPos = position + Vector3.up * StrikeHeight;
        Vector3 endPos = position;

        strikeStartObj.transform.position = startPos;
        strikeEndObj.transform.position = endPos;
        strikeStartObj.SetActive(true);
        strikeEndObj.SetActive(true);

        LightningCore.CreateLightningVFX(
            strikeStartObj.transform,
            strikeEndObj.transform,
            StrikeHeight,
            0.3f,
            null,
            0f,
            0f,
            0.1f
        );

        strikeStartObj.SetActive(false);
        strikeEndObj.SetActive(false);
    }

    private void ActivateBuff()
    {
        buffTimer = buffDuration;
        
        if (!isBuffActive)
        {
            isBuffActive = true;
            nextBuffArcTime = Time.time;
            CombatEvents.DamageDealt += OnDamageDealt;
            if (buffArcContainer != null) buffArcContainer.SetActive(true);
        }
    }

    private void DeactivateBuff()
    {
        if (!isBuffActive) return;
        
        isBuffActive = false;
        buffTimer = 0f;
        CombatEvents.DamageDealt -= OnDamageDealt;
        if (buffArcContainer != null) buffArcContainer.SetActive(false);
    }

    private void SpawnBuffArcs()
    {
        if (owner == null || buffArcContainer == null) return;

        spiralOffset += Time.deltaTime * 2f;
        
        for (int i = 0; i < BuffArcCount; i++)
        {
            float angle = (i / (float)BuffArcCount) * Mathf.PI * 2f + spiralOffset;
            float heightRatio = (i / (float)(BuffArcCount - 1));
            float height = -BuffArcHeight * 0.5f + BuffArcHeight * heightRatio;
            
            float nextAngle = angle + (Mathf.PI * 2f / BuffArcCount);
            float nextHeightRatio = ((i + 1) % BuffArcCount) / (float)(BuffArcCount - 1);
            float nextHeight = -BuffArcHeight * 0.5f + BuffArcHeight * nextHeightRatio;
            
            Vector3 start = new Vector3(
                Mathf.Cos(angle) * BuffArcRadius,
                height,
                Mathf.Sin(angle) * BuffArcRadius
            );
            
            Vector3 end = new Vector3(
                Mathf.Cos(nextAngle) * BuffArcRadius,
                nextHeight,
                Mathf.Sin(nextAngle) * BuffArcRadius
            );

            arcStartPool[i].transform.localPosition = start;
            arcEndPool[i].transform.localPosition = end;
            arcStartPool[i].SetActive(true);
            arcEndPool[i].SetActive(true);

            GameObject vfx = LightningCore.CreateLightningVFX(
                arcStartPool[i].transform,
                arcEndPool[i].transform,
                BuffArcRadius * 3f,
                0.25f,
                new Color(1f, 0.9f, 0.3f, 1f),
                0f,
                0f,
                0.05f
            );

            if (vfx != null)
            {
                var lr = vfx.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.startWidth = 0.08f;
                    lr.endWidth = 0.08f;
                }
            }

            arcStartPool[i].SetActive(false);
            arcEndPool[i].SetActive(false);
        }
    }

    private void OnDamageDealt(Entity attacker, Component target, float damage, ElementType triggerElement)
    {
        if (!isBuffActive || disposed || attacker != owner) return;

        if (IsProcessingChain || ChainLightning.IsProcessingChain) return;

        if (triggerElement == ElementType.Lightning) return;

        if (Time.time - lastChainTime < MinChainInterval) return;

        Enemy enemy = target as Enemy;
        if (enemy == null) return;

        if (damage <= 0) return;

        float chainDamage = damage * treeChainDamagePercent;

        IsProcessingChain = true;
        lastChainTime = Time.time;

        LightningCore.CreateTreeChain(
            owner: owner,
            startEnemy: enemy,
            baseDamage: chainDamage,
            maxDepth: maxTreeDepth,
            branchesPerNode: branchesPerNode,
            range: chainRange,
            damageDecayPerDepth: 0.8f,
            vfxDuration: 0.35f,
            skipRootProcessing: true
        );

        IsProcessingChain = false;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        DeactivateBuff();
        
        if (buffArcContainer != null) UnityEngine.Object.Destroy(buffArcContainer);
        if (strikeStartObj != null) UnityEngine.Object.Destroy(strikeStartObj);
        if (strikeEndObj != null) UnityEngine.Object.Destroy(strikeEndObj);
        if (strikeWarning != null) UnityEngine.Object.Destroy(strikeWarning);
    }
}
