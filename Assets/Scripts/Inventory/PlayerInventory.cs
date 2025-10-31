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
    private StompDamage stompDamageEffect;
    private FallDamageBonus fallDamageBonusEffect;
    private DotOnHit dotOnHitEffect;

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
        stompDamageEffect?.Update(dt);
        fallDamageBonusEffect?.Update(dt);
        dotOnHitEffect?.Update(dt);
        // more runtime effects would be updated here ig
    }


    public void AddItem(ItemData itemData)
    {
        if (itemData == null) return;

        if (!items.TryGetValue(itemData, out ItemStack stack))
        {
            // then this is new item
            stack = new ItemStack(itemData);
            items.Add(itemData, stack);

            ApplyStatModifiers(itemData, stack, stacksAdded: 1);
            ApplyEffects(itemData, stack, stacksAdded: 1);

            OnItemAdded?.Invoke(itemData, stack);
            Debug.Log($"Added new item: {itemData.itemName}");
            return;
        }
        

        // stacking existing thing
        if(itemData.canStack && stack.count < itemData.maxStacks)
        {
            stack.count++;
            ApplyStatModifiers(itemData, stack, stacksAdded: 1);
            ApplyEffects(itemData, stack, stacksAdded: 1);

            OnItemStackChanged?.Invoke(itemData, stack);
            Debug.Log($"Stacked item : {itemData.itemName} : {stack.count}");
        }
        else
        {
            Debug.Log($"Max Stacks reached for item : {itemData.itemName} : {stack.count}");
        }
    }

    public void RemoveItem(ItemData itemData)
    {
        if (!items.TryGetValue(itemData, out ItemStack stack)) return;
        // remove all stat modifiers created by this item
        foreach (var modifier in stack.activeModifiers)
        {
            modifier.Dispose();
        }

        // remove contributed effect stacks
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
                    break; // nothing yet, prolly dont even need it
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

    private void RemoveEffectStacks(ItemEffectKind kind, int stacks)
    {
        if (stacks <= 0) return;

        switch (kind)
        {
            case ItemEffectKind.HealOnDamage:
                if (healOnDamageEffect != null)
                {
                    healOnDamageEffect.AddStack(-stacks);
                    // if it reaches 0 it'll dispose itself
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

        }
    }
    
    void OnDestroy()
    {
        healOnDamageEffect?.Dispose();
        stompDamageEffect?.Dispose();
        fallDamageBonusEffect?.Dispose();
        dotOnHitEffect?.Dispose();

        // any other dispose handle
    }

    

}
