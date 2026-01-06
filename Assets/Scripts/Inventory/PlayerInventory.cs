using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    public class ItemStack
    {
        public ItemData itemData;
        public int count;
        public List<StatModifier> activeModifiers = new List<StatModifier>();
        public Dictionary<ItemEffectKind, int> contributedEffectStacks = new();
        public IDisposable runtimeEffect;

        public ItemStack(ItemData data)
        {
            itemData = data;
            count = 1;
        }
    }

    private Dictionary<ItemData, ItemStack> items = new();
    private Entity playerEntity;
    
    private HealOnDamage healOnDamageEffect;
    private StompDamage stompDamageEffect;
    private FallDamageBonus fallDamageBonusEffect;
    private DotOnHit dotOnHitEffect;
    private HomingProjectileEffect homingProjectilesEffect;
    private ExplosiveProjectiles explosiveProjectilesEffect;
    private ChainLightning chainLightningEffect;
    private BounceProjectiles bounceProjectilesEffect;
    private DelayedProjectiles delayedProjectilesEffect;
    private DashDamage dashDamageEffect;

    private readonly List<IDisposable> tickingEffects = new();

    public event Action<ItemData, ItemStack> OnItemAdded;
    public event Action<ItemData, ItemStack> OnItemStackChanged;
    public event Action<ItemData> OnItemRemoved;

    public IReadOnlyDictionary<ItemData, ItemStack> Items => items;

    void Awake()
    {
        playerEntity = GetComponent<Entity>();
        if (playerEntity == null)
        {
            Debug.Log("PlayerInventory requires Entity component");
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        healOnDamageEffect?.Update(dt);
        stompDamageEffect?.Update(dt);
        fallDamageBonusEffect?.Update(dt);
        dotOnHitEffect?.Update(dt);
        explosiveProjectilesEffect?.Update(dt);
        chainLightningEffect?.Update(dt);
        bounceProjectilesEffect?.Update(dt);
        delayedProjectilesEffect?.Update(dt);
        dashDamageEffect?.Update(dt);
    }

    public void AddItem(ItemData itemData)
    {
        if (itemData == null) return;

        if (!items.TryGetValue(itemData, out ItemStack stack))
        {
            stack = new ItemStack(itemData);
            items.Add(itemData, stack);
            ApplyStatModifiers(itemData, stack, stacksAdded: 1);
            ApplyEffects(itemData, stack, stacksAdded: 1);
            OnItemAdded?.Invoke(itemData, stack);
            Debug.Log($"Added new item: {itemData.itemName}");
            return;
        }
        
        if(itemData.canStack && stack.count < itemData.maxStacks)
        {
            stack.count++;
            ApplyStatModifiers(itemData, stack, stacksAdded: 1);
            ApplyEffects(itemData, stack, stacksAdded: 1);
            OnItemStackChanged?.Invoke(itemData, stack);
            Debug.Log($"Stacked item : {itemData.itemName} : Count: {stack.count}");
        }
        else
        {
            Debug.Log($"Max Stacks reached for item : {itemData.itemName} : {stack.count}");
        }
    }

    public void RemoveItem(ItemData itemData)
    {
        if (!items.TryGetValue(itemData, out ItemStack stack)) return;

        foreach (var modifier in stack.activeModifiers)
        {
            modifier.Dispose();
        }

        foreach (var kv in stack.contributedEffectStacks)
        {
            var kind = kv.Key;
            int stacks = kv.Value;
            RemoveEffectStacks(kind, stacks);
        }

        items.Remove(itemData);
        OnItemRemoved?.Invoke(itemData);
        Debug.Log($"Removed item : {itemData.itemName}");
    }
    
    public int GetItemCount(ItemData itemData) => items.TryGetValue(itemData, out var stck) ? stck.count : 0;
    public bool HasItem(ItemData itemData) => items.ContainsKey(itemData);

    private void ApplyStatModifiers(ItemData data, ItemStack stack, int stacksAdded)
    {
        if (playerEntity == null || playerEntity.Stats == null) return;

        if (data.useMultipleStats == true)
        {
            if (data.statMods == null || data.statMods.Count == 0) 
            {
                return;
            }
            foreach (var s in data.statMods)
            {
                AddOneModifier(s, stack, stacksAdded);
            }
            return;
        }

        var legacy = new StatModSpec
        {
            statType = data.statType,
            operatorType = data.operatorType,
            value = data.value,
            duration = data.duration
        };
        AddOneModifier(legacy, stack, stacksAdded);
    }

    private void AddOneModifier(StatModSpec spec, ItemStack stack, int stacksAdded)
    {
        float baseValue = playerEntity.Stats.BaseValueForStat(spec.statType);
        float inc = spec.value;

        for (int i = 0; i < stacksAdded; i++)
        {
            StatModifier modifier = spec.operatorType switch
            {
                OperatorType.Add => new BasicStatsModifier(spec.statType, spec.duration, v => v + inc),
                OperatorType.Multiply => new BasicStatsModifier(spec.statType, spec.duration, v => v * inc),
                OperatorType.Percentage => new BasicStatsModifier(spec.statType, spec.duration, v => v + baseValue * inc),
                _ => throw new ArgumentOutOfRangeException()
            };

            stack.activeModifiers.Add(modifier);
            playerEntity.Stats.Mediator.AddModifier(modifier);
            Debug.Log($"Applied {spec.statType} modifier: {inc} ({spec.operatorType})");
        }
    }
    
    private void ApplyEffects(ItemData data, ItemStack stack, int stacksAdded)
    {
        if (playerEntity == null) return;
        if (data.effects == null || data.effects.Count == 0) return;

        foreach(var effect in data.effects)
        {
            if (effect.kind == ItemEffectKind.None) continue;

            if (!stack.contributedEffectStacks.ContainsKey(effect.kind))
                stack.contributedEffectStacks[effect.kind] = 0;
            stack.contributedEffectStacks[effect.kind] += stacksAdded;

            switch (effect.kind)
            {
                case ItemEffectKind.HealOnDamage:
                    EnsureHealOnDamage(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.StompDamage:
                    EnsureStomp(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.FallDamageBonus:
                    EnsureFallBonus(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.DotOnHit:
                    EnsureDot(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.BurnOnDamage:
                    break;
                case ItemEffectKind.HomingProjectiles:
                    EnsureHomingProjectiles(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.ExplosiveProjectiles:
                    EnsureExplosiveProjectiles(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.ChainLightning:
                    EnsureChainLightning(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.BounceProjectiles:
                    EnsureBounceProjectiles(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.DelayedProjectiles:
                    EnsureDelayedProjectiles(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.DashDamage:
                    EnsureDashDamage(effect, initialStacks: stacksAdded);
                    break;
            }
        }
    }

    private void EnsureHealOnDamage(EffectSpec effect, int initialStacks)
    {
        if (healOnDamageEffect == null)
        {
            healOnDamageEffect = new HealOnDamage(
            owner: playerEntity,
            percentPerStack: Mathf.Max(0f, effect.healOnDamagePercentPerStack),
            initialStacks: initialStacks,
            durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(healOnDamageEffect);
            Debug.Log($"[Effect] Heal on Damage created : Stacks{initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                healOnDamageEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Heal on Damage : Stacks {initialStacks}");
        }
    }

    private void EnsureStomp(EffectSpec effect, int initialStacks)
    {
        if (stompDamageEffect == null)
        {
            stompDamageEffect = new StompDamage(
            owner: playerEntity,
            damagePerStack: effect.stompDamagePerStack,
            bounceForce: effect.stompBounceForce,
            initialStacks: initialStacks,
            durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(stompDamageEffect);
            Debug.Log($"[Effect] Stomp created : Stacks{initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                stompDamageEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Stomp : Stacks {initialStacks}");
        }
    }

    private void EnsureFallBonus(EffectSpec effect, int initialStacks)
    {
        if (fallDamageBonusEffect == null)
        {
            fallDamageBonusEffect = new FallDamageBonus(
            owner: playerEntity,
            damagePerMeter: effect.fallDamageBonusPerMeter * effect.fallDamageBonusPerStack,
            initialStacks: initialStacks,
            durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(fallDamageBonusEffect);
            Debug.Log($"[Effect] FallBonus created : Stacks {initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                fallDamageBonusEffect.AddStack(1);
            }
            Debug.Log($"[Effect] FallBonus : Stacks {initialStacks}");
        }
    }

    private void EnsureDot(EffectSpec effect, int initialStacks)
    {
        if (dotOnHitEffect == null)
        {
            dotOnHitEffect = new DotOnHit(
                owner: playerEntity,
                dotDamagePerTick: effect.dotDamagePerTick,
                dotTickInterval: effect.dotTickInterval,
                dotDuration: effect.dotDuration,
                dotDamagePerStack: effect.dotDamagePerStack,
                initialStacks: initialStacks,
                durationSec: effect.duration,
                dotCanStack: effect.dotCanStack,
                dotMaxStacks: effect.dotMaxStacks,
                dotApplyImmediately: effect.dotApplyImmediately
            );
            if (effect.duration > 0f) tickingEffects.Add(dotOnHitEffect);
            Debug.Log($"[Effect] DOT created : Stacks {initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                dotOnHitEffect.AddStack(1);
            }
            Debug.Log($"[Effect] DOT : Stacks {initialStacks}");
        }
    }

    private void EnsureHomingProjectiles(EffectSpec effect, int initialStacks)
    {
    }

    private void EnsureExplosiveProjectiles(EffectSpec effect, int initialStacks)
    {
        if (explosiveProjectilesEffect == null)
        {
            explosiveProjectilesEffect = new ExplosiveProjectiles(
                playerEntity,
                effect.explosiveAoeRadius,
                effect.explosiveAoeDamageMultiplier,
                effect.explosiveMaxRange,
                initialStacks,
                effect.duration,
                effect.explosiveVFX
            );
            if (effect.duration > 0f) tickingEffects.Add(explosiveProjectilesEffect);
            Debug.Log($"[Effect] Explosive Projectiles created : Stacks{initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                explosiveProjectilesEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Explosive Projectiles : Stacks {initialStacks}");
        }
    }

    private void EnsureChainLightning(EffectSpec effect, int initialStacks)
    {
        if (chainLightningEffect == null)
        {
            chainLightningEffect = new ChainLightning(
                owner: playerEntity,
                chainDamagePercent: effect.chainDamagePercent,
                maxChainCount: effect.maxChainCount,
                chainRange: effect.chainRange,
                initialStacks: initialStacks,
                durationSec: effect.duration,
                lightningVFX: effect.chainLightningVFX
            );
            if (effect.duration > 0f) tickingEffects.Add(chainLightningEffect);
            Debug.Log($"[Effect] Chain Lightning created : Stacks{initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                chainLightningEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Chain Lightning : Stacks {initialStacks}");
        }
    }

    private void EnsureBounceProjectiles(EffectSpec effect, int initialStacks)
    {
        if (bounceProjectilesEffect == null)
        {
            bounceProjectilesEffect = new BounceProjectiles(
                owner: playerEntity,
                bounceRange: effect.bounceRange,
                maxBounceCount: effect.maxBounceCount,
                damageMultiplierPerBounce: effect.damageMultiplierPerBounce,
                initialStacks: initialStacks,
                durationSec: effect.duration,
                bounceVFX: effect.bounceVFX
            );
            if (effect.duration > 0f) tickingEffects.Add(bounceProjectilesEffect);
            Debug.Log($"[Effect] Bounce Projectiles created : Stacks{initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                bounceProjectilesEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Bounce Projectiles : Stacks {initialStacks}");
        }
    }

    private void EnsureDelayedProjectiles(EffectSpec effect, int initialStacks)
    {
        if (delayedProjectilesEffect == null)
        {
            delayedProjectilesEffect = new DelayedProjectiles(
                owner: playerEntity,
                delayTime: effect.delayedDamageTime,
                damageMultiplier: effect.delayedDamageMultiplier,
                initialStacks: initialStacks,
                durationSec: effect.duration,
                markVFX: effect.delayedMarkVFX
            );
            if (effect.duration > 0f) tickingEffects.Add(delayedProjectilesEffect);
            Debug.Log($"[Effect] Delayed Projectiles created : Stacks{initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                delayedProjectilesEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Delayed Projectiles : Stacks {initialStacks}");
        }
    }

    private void EnsureDashDamage(EffectSpec effect, int initialStacks)
    {
        if (dashDamageEffect == null)
        {
            dashDamageEffect = new DashDamage(
                owner: playerEntity,
                dashDamage: effect.dashDamage,
                dashDamageRange: effect.dashDamageRange,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(dashDamageEffect);
            Debug.Log($"[Effect] Dash Damage created : Stacks{initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                dashDamageEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Dash Damage : Stacks {initialStacks}");
        }
    }

    private void RemoveEffectStacks(ItemEffectKind kind, int stacks)
    {
        if (stacks <= 0) return;

        switch (kind)
        {
            case ItemEffectKind.HealOnDamage:
                if (healOnDamageEffect != null)
                {
                    healOnDamageEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.StompDamage:
                if (stompDamageEffect != null)
                {
                    stompDamageEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.FallDamageBonus:
                if (fallDamageBonusEffect != null)
                {
                    fallDamageBonusEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.DotOnHit:
                if (dotOnHitEffect != null)
                {
                    dotOnHitEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.ExplosiveProjectiles:
                if (explosiveProjectilesEffect != null)
                {
                    explosiveProjectilesEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.ChainLightning:
                if (chainLightningEffect != null)
                {
                    chainLightningEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.BounceProjectiles:
                if (bounceProjectilesEffect != null)
                {
                    bounceProjectilesEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.DelayedProjectiles:
                if (delayedProjectilesEffect != null)
                {
                    delayedProjectilesEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.DashDamage:
                if (dashDamageEffect != null)
                {
                    dashDamageEffect.AddStack(-stacks);
                }
                break;
        }
    }
    
    void OnDestroy()
    {
        healOnDamageEffect?.Dispose();
        stompDamageEffect?.Dispose();
        fallDamageBonusEffect?.Dispose();
        dotOnHitEffect?.Dispose();
        explosiveProjectilesEffect?.Dispose();
        chainLightningEffect?.Dispose();
        bounceProjectilesEffect?.Dispose();
        delayedProjectilesEffect?.Dispose();
        dashDamageEffect?.Dispose();
    }
}
