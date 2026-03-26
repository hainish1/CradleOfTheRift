using System;
using UnityEngine;

public class LightningStrikeChainBuff : IDisposable
{
    private readonly Entity owner;
    private readonly float buffDuration;
    private readonly float treeChainDamagePercent;
    private readonly int maxTreeDepth;
    private readonly int branchesPerNode;
    private readonly float chainRange;

    private float buffTimer;
    private bool isBuffActive;
    private bool disposed;

    private float lastChainTime;
    private float nextBuffArcTime;
    private float spiralOffset;

    private const float MinChainInterval = 0.15f;
    private const float BuffArcInterval = 0.15f;
    private const int BuffArcCount = 6;
    private const float BuffArcRadius = 0.4f;
    private const float BuffArcHeight = 1.5f;

    private GameObject buffArcContainer;
    private GameObject[] arcStartPool;
    private GameObject[] arcEndPool;

    public LightningStrikeChainBuff(
        Entity owner,
        float buffDuration,
        float chainDamagePercent,
        int maxDepth,
        int branchesPerNode,
        float chainRange)
    {
        this.owner = owner;
        this.buffDuration = buffDuration;
        this.treeChainDamagePercent = chainDamagePercent;
        this.maxTreeDepth = maxDepth;
        this.branchesPerNode = branchesPerNode;
        this.chainRange = chainRange;

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

        LightningStrikeEvents.StrikeLanded += OnStrikeLanded;
    }

    public bool IsDisposed => disposed;

    public void Update(float dt)
    {
        if (disposed || !isBuffActive) return;

        buffTimer -= dt;
        if (buffTimer <= 0f)
        {
            DeactivateBuff();
            return;
        }

        if (Time.time >= nextBuffArcTime)
        {
            SpawnBuffArcs();
            nextBuffArcTime = Time.time + BuffArcInterval;
        }
    }

    private void OnStrikeLanded(Entity strikeOwner, Vector3 pos, float dmg)
    {
        if (disposed || strikeOwner != owner) return;
        ActivateBuff();
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

    private void OnDamageDealt(Entity attacker, Component target, float damage, ElementType triggerElement)
    {
        if (!isBuffActive || disposed || attacker != owner) return;
        if (ChainLightningTest.IsProcessingChain || ChainLightning.IsProcessingChain) return;
        if (triggerElement == ElementType.Lightning) return;
        if (Time.time - lastChainTime < MinChainInterval) return;

        Enemy enemy = target as Enemy;
        if (enemy == null || damage <= 0) return;

        float chainDamage = damage * treeChainDamagePercent;
        ChainLightningTest.IsProcessingChain = true;
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

        ChainLightningTest.IsProcessingChain = false;
    }

    private void SpawnBuffArcs()
    {
        if (owner == null || buffArcContainer == null) return;

        spiralOffset += Time.deltaTime * 2f;

        for (int i = 0; i < BuffArcCount; i++)
        {
            float angle = (i / (float)BuffArcCount) * Mathf.PI * 2f + spiralOffset;
            float heightRatio = i / (float)(BuffArcCount - 1);
            float height = -BuffArcHeight * 0.5f + BuffArcHeight * heightRatio;

            float nextAngle = angle + (Mathf.PI * 2f / BuffArcCount);
            float nextHeightRatio = ((i + 1) % BuffArcCount) / (float)(BuffArcCount - 1);
            float nextHeight = -BuffArcHeight * 0.5f + BuffArcHeight * nextHeightRatio;

            Vector3 start = new Vector3(Mathf.Cos(angle) * BuffArcRadius, height, Mathf.Sin(angle) * BuffArcRadius);
            Vector3 end = new Vector3(Mathf.Cos(nextAngle) * BuffArcRadius, nextHeight, Mathf.Sin(nextAngle) * BuffArcRadius);

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

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        DeactivateBuff();
        LightningStrikeEvents.StrikeLanded -= OnStrikeLanded;
        if (buffArcContainer != null) UnityEngine.Object.Destroy(buffArcContainer);
    }
}
