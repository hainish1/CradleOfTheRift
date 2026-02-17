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
        public List<StatModifier> activeModifiers = new List<StatModifier>(); // all modifiers created for this item's stacks

        public Dictionary<ItemEffectKind, int> contributedEffectStacks = new(); // how many stacks this item contributed into each runtime effect
        public List<(ElementType, ElementType)> contributedElementFusions = new(); // element fusion rules added by this item
        // public IDisposable activeEffect; 
        public IDisposable runtimeEffect;

        public ItemStack(ItemData data)
        {
            itemData = data;
            count = 1;
        }
    }

    private Dictionary<ItemData, ItemStack> items = new();
    private Entity playerEntity;

    // once instance per effect kind on the player
    private HealOnDamage healOnDamageEffect;
    private HealOnPoison healOnPoisonEffect;
    private StompDamage stompDamageEffect;
    private FallDamageBonus fallDamageBonusEffect;
    private DotOnHit dotOnHitEffect;
    private PoisonCore poisonCore;
    private PoisonPoolOnDash poisonPoolOnDashEffect;
    private HomingProjectileEffect homingProjectilesEffect;
    private ExplosiveProjectiles explosiveProjectilesEffect;
    private ChainLightning chainLightningEffect;
    private BounceProjectiles bounceProjectilesEffect;
    private DelayedProjectiles delayedProjectilesEffect;
    private DashDamage dashDamageEffect;
    private BurnAura burnAuraEffect;
    private ArcStrike arcStrikeEffect;
    private PlayerLightningStrike playerLightningStrikeEffect;
    private FlyingFire flyingFireSprayEffect;
    private ElementReactionExplosion elementReactionExplosionEffect;
    private OrbitingFireballs orbitingFireballsEffect;
    private LightningDash lightningDashEffect;
    private OrbitingFireballsTest orbitingFireballsTestEffect;
    private ChainLightningTest chainLightningTestEffect;
    private PassThroughSpear passThroughSpearEffect;

    // if I have time limited effects that need use of Updates, I will keep em here
    private readonly List<IDisposable> tickingEffects = new();
    // private List<HealOnDamage> healEffects = new();

    // for UI updates
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
        healOnPoisonEffect?.Update(dt);
        stompDamageEffect?.Update(dt);
        fallDamageBonusEffect?.Update(dt);
        dotOnHitEffect?.Update(dt);
        poisonPoolOnDashEffect?.Update(dt);
        explosiveProjectilesEffect?.Update(dt);
        chainLightningEffect?.Update(dt);
        bounceProjectilesEffect?.Update(dt);
        delayedProjectilesEffect?.Update(dt);
        dashDamageEffect?.Update(dt);
        burnAuraEffect?.Update(dt);
        arcStrikeEffect?.Update(dt);
        playerLightningStrikeEffect?.Update(dt);
        flyingFireSprayEffect?.Update(dt);
        elementReactionExplosionEffect?.Update(dt);
        orbitingFireballsEffect?.Update(dt);
        lightningDashEffect?.Update(dt);
        orbitingFireballsTestEffect?.Update(dt);
        chainLightningTestEffect?.Update(dt);
        passThroughSpearEffect?.Update(dt);
        //homingProjectilesEffect?.Update(dt);

        // more runtime effects would be updated here ig
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


        // stacking existing thing
        if (itemData.canStack && stack.count < itemData.maxStacks)
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

    public void PauseOrbitingFireballs()
    {
        orbitingFireballsEffect?.Pause();
        orbitingFireballsTestEffect?.Pause();
    }

    public void ResumeOrbitingFireballs()
    {
        orbitingFireballsEffect?.Resume();
        orbitingFireballsTestEffect?.Resume();
    }

    public void RemoveItem(ItemData itemData)
    {
        if (!items.TryGetValue(itemData, out ItemStack stack)) return;
        // remove all stat modifiers created by this item
        foreach (var modifier in stack.activeModifiers)
        {
            modifier.Dispose();
        }

        RemovePoisonContribution(itemData);

        // remove contributed effect stacks
        foreach (var kv in stack.contributedEffectStacks)
        {
            var kind = kv.Key;
            int stacks = kv.Value;
            RemoveEffectStacks(kind, stacks);
        }

        // remove element fusion rules
        foreach (var fusionKey in stack.contributedElementFusions)
        {
            ElementSystem.RemoveTempRule(fusionKey.Item1, fusionKey.Item2);
        }

        items.Remove(itemData);
        OnItemRemoved?.Invoke(itemData);
        Debug.Log($"Removed item : {itemData.itemName}");
    }

    // some getters
    public int GetItemCount(ItemData itemData) => items.TryGetValue(itemData, out var stck) ? stck.count : 0;
    public bool HasItem(ItemData itemData) => items.ContainsKey(itemData);

    // --------------------------------------STATS--------------------------------------

    private void ApplyStatModifiers(ItemData data, ItemStack stack, int stacksAdded)
    {
        if (playerEntity == null || playerEntity.Stats == null) return;

        // decide which specs
        if (data.useMultipleStats == true)
        {
            // if effect items only, no stats to apply
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

        // Legacy single stat
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
        // how much to apply per new stack
        float baseValue = playerEntity.Stats.BaseValueForStat(spec.statType);
        float inc = spec.value; // linear stacking

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


    // ------------------ EFFECTS ------------------------

    private void ApplyEffects(ItemData data, ItemStack stack, int stacksAdded)
    {
        if (playerEntity == null) return;
        if (data.effects == null || data.effects.Count == 0) return;

        bool poisonCoreAdded = false;

        foreach (var effect in data.effects)
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
                case ItemEffectKind.HealOnPoison:
                    EnsureHealOnPoison(effect);
                    break;
                case ItemEffectKind.StompDamage:
                    EnsureStomp(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.FallDamageBonus:
                    EnsureFallBonus(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.DotOnHit:
                    if (!poisonCoreAdded)
                    {
                        EnsurePoisonCore(data, stacksAdded);
                        poisonCoreAdded = true;
                    }
                    EnsureDot(data, effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.PoisonPoolOnDash:
                    if (!poisonCoreAdded)
                    {
                        EnsurePoisonCore(data, stacksAdded);
                        poisonCoreAdded = true;
                    }
                    EnsurePoisonPoolOnDash(data, effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.BurnOnDamage:
                    EnsureBurnAura(effect, initialStacks: stacksAdded);
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
                case ItemEffectKind.ElementFusion:
                    EnsureElementFusion(effect, stack, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.ArcStrike:
                    EnsureArcStrike(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.LightningStrike:
                    EnsurePlayerLightningStrike(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.FlyFire:
                    EnsureFlyingFireSpray(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.ElementReactionExplosion:
                    EnsureElementReactionExplosion(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.OrbitingFireballs:
                    EnsureOrbitingFireballs(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.LightningDash:
                    EnsureLightningDash(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.OrbitingFireballsTest:
                    EnsureOrbitingFireballsTest(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.ChainLightningTest:
                    EnsureChainLightningTest(effect, initialStacks: stacksAdded);
                    break;
                case ItemEffectKind.PassThroughSpear:
                    EnsurePassThroughSpear(effect, initialStacks: stacksAdded);
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

    private void EnsureHealOnPoison(EffectSpec effect)
    {
        if (healOnPoisonEffect == null || healOnPoisonEffect.IsDisposed)
        {
            healOnPoisonEffect = new HealOnPoison(
                owner: playerEntity,
                range: effect.healOnPoisonRange,
                healPerPoisonStackPerSecond: effect.healPerPoisonStackPerSecond,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(healOnPoisonEffect);
            Debug.Log($"[Effect] HealOnPoison created");
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
            damagePerMeter: effect.fallDamageBonusPerMeter + (effect.fallDamageBonusPerStack * (initialStacks - 1)),
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
    private void EnsureDot(ItemData data, EffectSpec effect, int initialStacks)
    {
        if (dotOnHitEffect == null)
        {
            dotOnHitEffect = new DotOnHit(
                owner: playerEntity,
                durationSec: effect.duration
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

    private void EnsurePoisonPoolOnDash(ItemData data, EffectSpec effect, int initialStacks)
    {
        if (poisonPoolOnDashEffect == null)
        {
            poisonPoolOnDashEffect = new PoisonPoolOnDash(
                owner: playerEntity,
                radius: effect.poisonPoolRadius,
                poolLifetime: effect.poisonPoolLifetime,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(poisonPoolOnDashEffect);
            Debug.Log($"[Effect] Poison Pool On Dash created : Stacks {initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                poisonPoolOnDashEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Poison Pool On Dash : Stacks {initialStacks}");
        }
    }

    private void EnsurePoisonCore(ItemData data, int initialStacks)
    {
        if (poisonCore == null)
        {
            poisonCore = new PoisonCore();
        }
        poisonCore.AddContribution(data, initialStacks);
    }

    private void EnsureHomingProjectiles(EffectSpec effect, int initialStacks)
    {
        // Do nothing idk
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
    
    private void EnsureBurnAura(EffectSpec effect, int initialStacks)
    {
        if (burnAuraEffect == null)
        {
            burnAuraEffect = new BurnAura(
                owner: playerEntity,
                damagePerSecond: effect.burnAuraDamagePerSecond,
                range: effect.burnAuraRange,
                initialStacks: initialStacks,
                durationSec: effect.duration,
                tickInterval: effect.burnAuraTickInterval
            );
            if (effect.duration > 0f) tickingEffects.Add(burnAuraEffect);
            Debug.Log($"[Effect] Burn Aura created, stacks {initialStacks}");
        }
        else
        {
            Debug.LogWarning("Burn Aura already exists, stacking.");
            for (int i = 0; i < initialStacks; i++)
            {
                burnAuraEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Burn Aura : Stacks {initialStacks}");
        }
    }
    
    private void EnsureArcStrike(EffectSpec effect, int initialStacks)
    {
        if (arcStrikeEffect == null)
        {
            arcStrikeEffect = new ArcStrike(
                owner: playerEntity,
                damage: effect.arcStrikeDamage,
                range: effect.arcStrikeRange,
                poissonLambda: effect.arcStrikePoissonLambda,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(arcStrikeEffect);
            Debug.Log($"[Effect] Arc Strike created : {effect.arcStrikeDamage} damage, {effect.arcStrikeRange}m range, {effect.arcStrikePoissonLambda} lambda, Stacks{initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                arcStrikeEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Arc Strike : Stacks {initialStacks}");
        }
    }

    private void EnsurePlayerLightningStrike(EffectSpec effect, int initialStacks)
    {
        if (playerLightningStrikeEffect == null)
        {
            playerLightningStrikeEffect = new PlayerLightningStrike(
                owner: playerEntity,
                damage: effect.playerLightningStrikeDamage,
                radius: effect.playerLightningStrikeRadius,
                interval: effect.playerLightningStrikeInterval,
                electrifyDamage: effect.playerLightningStrikeElectrifyDamage,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(playerLightningStrikeEffect);
            Debug.Log($"[Effect] Player Lightning Strike created : Stacks {initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                playerLightningStrikeEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Player Lightning Strike : Stacks {initialStacks}");
        }
    }

    private void EnsureFlyingFireSpray(EffectSpec effect, int initialStacks)
    {
        if (flyingFireSprayEffect == null)
        {
            flyingFireSprayEffect = new FlyingFire(
                owner: playerEntity,
                damage: effect.flyingFireDamage,
                radius: effect.flyingFireRadius,
                tickInterval: effect.flyingFireTickInterval,
                offset: effect.flyingFireOffset,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(flyingFireSprayEffect);
            Debug.Log($"[Effect] Flying Fire Spray created : Stacks {initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                flyingFireSprayEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Flying Fire Spray : Stacks {initialStacks}");
        }
    }

    private void EnsureElementFusion(EffectSpec effect, ItemStack stack, int initialStacks)
    {
        if (effect.fusionTriggerElement == ElementType.None || effect.fusionEffectElement == ElementType.None)
        {
            Debug.LogWarning("[Effect] ElementFusion: Invalid element types");
            return;
        }

        var key = (effect.fusionTriggerElement, effect.fusionEffectElement);
        
        for (int i = 0; i < initialStacks; i++)
        {
            ElementSystem.AddTempRule(effect.fusionTriggerElement, effect.fusionEffectElement);
            stack.contributedElementFusions.Add(key);
        }
    }

    private void EnsureElementReactionExplosion(EffectSpec effect, int initialStacks)
    {
        if (elementReactionExplosionEffect == null)
        {
            elementReactionExplosionEffect = new ElementReactionExplosion(
                owner: playerEntity,
                explosionDamage: effect.elementReactionExplosionDamage,
                explosionRadius: effect.elementReactionExplosionRadius,
                initialStacks: initialStacks,
                durationSec: effect.duration,
                explosionVFX: effect.elementReactionExplosionVFX
            );
            if (effect.duration > 0f) tickingEffects.Add(elementReactionExplosionEffect);
            Debug.Log($"[Effect] Element Reaction Explosion created : {effect.elementReactionExplosionDamage} damage, {effect.elementReactionExplosionRadius}m radius, Stacks{initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                elementReactionExplosionEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Element Reaction Explosion : Stacks {initialStacks}");
        }
    }

    private void EnsureOrbitingFireballs(EffectSpec effect, int initialStacks)
    {
        if (orbitingFireballsEffect == null)
        {
            orbitingFireballsEffect = new OrbitingFireballs(
                owner: playerEntity,
                damage: effect.orbitingFireballDamage,
                orbitRadius: effect.orbitingFireballRadius,
                rotationSpeed: effect.orbitingFireballRotationSpeed,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(orbitingFireballsEffect);
            Debug.Log($"[Effect] Orbiting Fireballs created : {effect.orbitingFireballDamage} damage, {effect.orbitingFireballRadius}m radius, Stacks {initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                orbitingFireballsEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Orbiting Fireballs : Stacks {initialStacks}");
        }
    }

    private void EnsureLightningDash(EffectSpec effect, int initialStacks)
    {
        if (lightningDashEffect == null)
        {
            lightningDashEffect = new LightningDash(
                owner: playerEntity,
                damage: effect.lightningDashDamage,
                chainCount: effect.lightningDashChainCount,
                range: effect.lightningDashChainRange,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(lightningDashEffect);
            Debug.Log($"[Effect] Lightning Dash created : {effect.lightningDashDamage} damage, {effect.lightningDashChainCount} chains, Stacks {initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                lightningDashEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Lightning Dash : Stacks {initialStacks}");
        }
    }

    private void EnsureOrbitingFireballsTest(EffectSpec effect, int initialStacks)
    {
        if (orbitingFireballsTestEffect == null)
        {
            orbitingFireballsTestEffect = new OrbitingFireballsTest(
                owner: playerEntity,
                damage: effect.orbitingFireballTestDamage,
                orbitRadius: effect.orbitingFireballRadius,
                rotationSpeed: effect.orbitingFireballRotationSpeed,
                damageThreshold: effect.orbitingFireballTestDamageThreshold,
                maxCount: effect.orbitingFireballTestMaxCount,
                ballLifetime: effect.orbitingFireballTestLifetime,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(orbitingFireballsTestEffect);
            Debug.Log($"[Effect] Orbiting Fireballs Test created : {effect.orbitingFireballTestDamage} dmg/ball, {effect.orbitingFireballTestDamageThreshold} dmg threshold, Max {effect.orbitingFireballTestMaxCount} balls, {effect.orbitingFireballTestLifetime}s lifetime, Stacks {initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                orbitingFireballsTestEffect.AddStack(1);
            }
            Debug.Log($"[Effect] Orbiting Fireballs Test : Stacks {initialStacks}");
        }
    }

    private void EnsureChainLightningTest(EffectSpec effect, int initialStacks)
    {
        if (chainLightningTestEffect == null)
        {
            chainLightningTestEffect = new ChainLightningTest(
                owner: playerEntity,
                strikeInterval: effect.chainLightningTestStrikeInterval,
                buffDuration: effect.chainLightningTestBuffDuration,
                chainDamagePercent: effect.chainLightningTestChainDamagePercent,
                maxDepth: effect.chainLightningTestMaxDepth,
                branchesPerNode: effect.chainLightningTestBranchesPerNode,
                chainRange: effect.chainLightningTestChainRange,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );
            if (effect.duration > 0f) tickingEffects.Add(chainLightningTestEffect);
            Debug.Log($"[Effect] ChainLightningTest created : Stacks {initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
            {
                chainLightningTestEffect.AddStack(1);
            }
            Debug.Log($"[Effect] ChainLightningTest : Stacks {initialStacks}");
        }
    }

    private void EnsurePassThroughSpear(EffectSpec effect, int initialStacks)
    {
        if (passThroughSpearEffect == null)
        {
            passThroughSpearEffect = new PassThroughSpear(
                owner: playerEntity,
                passThroughEnemyCount: effect.passThroughEnemyCount,
                initialStacks: initialStacks,
                durationSec: effect.duration
            );

            if (effect.duration > 0f) tickingEffects.Add(passThroughSpearEffect);
            Debug.Log($"[Effect] Pass Through Spear created : passThrough={effect.passThroughEnemyCount}, Stacks={initialStacks}");
        }
        else
        {
            for (int i = 0; i < initialStacks; i++)
                passThroughSpearEffect.AddStack(1);

            // Update the count in case the new item stack has a different value
            passThroughSpearEffect.SetPassThroughEnemyCount(effect.passThroughEnemyCount);

            Debug.Log($"[Effect] Pass Through Spear : passThrough={effect.passThroughEnemyCount}, Stacks={initialStacks}");
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
            case ItemEffectKind.HealOnPoison:
                if (healOnPoisonEffect != null && !healOnPoisonEffect.IsDisposed)
                {
                    healOnPoisonEffect.Dispose();
                    healOnPoisonEffect = null;
                }
                break;
            case ItemEffectKind.StompDamage:
                if (stompDamageEffect != null)
                {
                    stompDamageEffect.AddStack(-stacks);
                    // if it reaches 0 it'll dispose itself
                }
                break;
            case ItemEffectKind.FallDamageBonus:
                if (fallDamageBonusEffect != null)
                {
                    fallDamageBonusEffect.AddStack(-stacks);
                    // if it reaches 0 it'll dispose itself
                }
                break;
            case ItemEffectKind.DotOnHit:
                if (dotOnHitEffect != null)
                {
                    dotOnHitEffect.AddStack(-stacks);
                    // if it reaches 0 it'll dispose itself
                }
                break;
            case ItemEffectKind.PoisonPoolOnDash:
                if (poisonPoolOnDashEffect != null)
                {
                    poisonPoolOnDashEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.ExplosiveProjectiles:
                if (explosiveProjectilesEffect != null)
                {
                    explosiveProjectilesEffect.AddStack(-stacks);
                    // if it reaches 0 it'll dispose itself
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
            case ItemEffectKind.BurnOnDamage:
                if (burnAuraEffect != null)
                {
                    burnAuraEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.ArcStrike:
                if (arcStrikeEffect != null)
                {
                    arcStrikeEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.LightningStrike:
                if (playerLightningStrikeEffect != null)
                {
                    playerLightningStrikeEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.FlyFire:
                if (flyingFireSprayEffect != null)
                {
                    flyingFireSprayEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.ElementReactionExplosion:
                if (elementReactionExplosionEffect != null)
                {
                    elementReactionExplosionEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.OrbitingFireballs:
                if (orbitingFireballsEffect != null)
                {
                    orbitingFireballsEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.LightningDash:
                if (lightningDashEffect != null)
                {
                    lightningDashEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.OrbitingFireballsTest:
                if (orbitingFireballsTestEffect != null)
                {
                    orbitingFireballsTestEffect.AddStack(-stacks);
                }
                break;
            case ItemEffectKind.PassThroughSpear:
                if (passThroughSpearEffect != null)
                {
                    passThroughSpearEffect.AddStack(-stacks);
                }
                break;

        }
    }

    private void RemovePoisonContribution(ItemData data)
    {
        if (poisonCore == null || data == null || data.effects == null) return;

        bool hasPoisonEffect = false;
        foreach (var effect in data.effects)
        {
            if (effect.kind == ItemEffectKind.DotOnHit || effect.kind == ItemEffectKind.PoisonPoolOnDash)
            {
                hasPoisonEffect = true;
                break;
            }
        }

        if (!hasPoisonEffect) return;

        poisonCore.RemoveContribution(data);
        if (poisonCore.IsEmpty)
        {
            poisonCore.Dispose();
            poisonCore = null;
        }
    }
    
    public bool TryRemoveStacks(ItemData itemData, int stacksToRemove)
    {
        if (itemData == null) return false;
        if (stacksToRemove <= 0) return true;

        if (!items.TryGetValue(itemData, out ItemStack stack)) return false;

        int removable = Mathf.Min(stacksToRemove, stack.count);

        // Remove stat modifiers contrinuted by these stacks (1 modifier per stack)
        for (int i = 0; i < removable; i++)
        {
            int last = stack.activeModifiers.Count - 1;
            if (last >= 0)
            {
                stack.activeModifiers[last].Dispose();
                stack.activeModifiers.RemoveAt(last);
            }
        }

        // remove effect stacks as well
        if (stack.contributedEffectStacks != null)
        {
            var keys = new List<ItemEffectKind>(stack.contributedEffectStacks.Keys);
            foreach (var kind in keys)
            {
                int contributed = stack.contributedEffectStacks[kind];
                int toRemoveForKind = Mathf.Min(removable, contributed);
                if (toRemoveForKind <= 0) continue;

                RemoveEffectStacks(kind, toRemoveForKind);
                stack.contributedEffectStacks[kind] = contributed - toRemoveForKind;
            }
        }

        stack.count -= removable;

        if (stack.count <= 0)
        {
            items.Remove(itemData);
            OnItemRemoved?.Invoke(itemData);
        }
        else
        {
            OnItemStackChanged?.Invoke(itemData, stack);
        }

        return true;
    }


    // Set Item Count - Helper for new Inventory changes
    public void SetItemCount(ItemData itemData, int newCount)
    {
        if (itemData == null) return;
        newCount = Mathf.Max(0, newCount);

        int current = GetItemCount(itemData);
        if (newCount == current) return;

        if (newCount > current)
        {
            int add = newCount - current;
            for (int i = 0; i < add; i++)
            {
                AddItem(itemData);
            }
        }
        else
        {
            int remove = current - newCount;
            TryRemoveStacks(itemData, remove);
        }
    }

    public void Clear()
    {
        // remove modifiers + effect stacks by items
        foreach (var pair in items)
        {
            var stack = pair.Value;

            foreach (var mod in stack.activeModifiers)
            {
                mod.Dispose();
            }
            stack.activeModifiers.Clear();

            foreach (var kv in stack.contributedEffectStacks)
            {
                RemoveEffectStacks(kv.Key, kv.Value);
            }
            stack.contributedEffectStacks.Clear();

            OnItemRemoved?.Invoke(pair.Key);
        }

        items.Clear();

        // stop any remaining runtime effects and reset 
        healOnDamageEffect?.Dispose(); healOnDamageEffect = null;
        stompDamageEffect?.Dispose(); stompDamageEffect = null;
        fallDamageBonusEffect?.Dispose(); fallDamageBonusEffect = null;
        dotOnHitEffect?.Dispose(); dotOnHitEffect = null;
        explosiveProjectilesEffect?.Dispose(); explosiveProjectilesEffect = null;
        chainLightningEffect?.Dispose(); chainLightningEffect = null;
        bounceProjectilesEffect?.Dispose(); bounceProjectilesEffect = null;
        delayedProjectilesEffect?.Dispose(); delayedProjectilesEffect = null;
        dashDamageEffect?.Dispose(); dashDamageEffect = null;
        passThroughSpearEffect?.Dispose(); passThroughSpearEffect = null;

        tickingEffects.Clear();
    }    void OnDestroy()
    {
        tickingEffects.Clear();

        healOnDamageEffect?.Dispose();
        healOnPoisonEffect?.Dispose();
        stompDamageEffect?.Dispose();
        fallDamageBonusEffect?.Dispose();
        dotOnHitEffect?.Dispose();
        poisonPoolOnDashEffect?.Dispose();
        poisonCore?.Dispose();
        explosiveProjectilesEffect?.Dispose();
        chainLightningEffect?.Dispose();
        bounceProjectilesEffect?.Dispose();
        delayedProjectilesEffect?.Dispose();
        dashDamageEffect?.Dispose();
        burnAuraEffect?.Dispose();
        arcStrikeEffect?.Dispose();
        playerLightningStrikeEffect?.Dispose();
        flyingFireSprayEffect?.Dispose();
        elementReactionExplosionEffect?.Dispose();
        orbitingFireballsEffect?.Dispose();
        lightningDashEffect?.Dispose();
        orbitingFireballsTestEffect?.Dispose();
        passThroughSpearEffect?.Dispose();
        //homingProjectilesEffect?.Dispose();
        ElementSystem.ClearTempRules();

        // any other dispose handle
    }



}
