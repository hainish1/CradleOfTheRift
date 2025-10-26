using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public class ItemStack
    {
        public ItemData itemData;
        public int count;
        public List<StatModifier> activeModifiers = new List<StatModifier>();
        public IDisposable activeEffect; // for special effects like HealOnDamage, StompDamage

        public ItemStack(ItemData data)
        {
            itemData = data;
            count = 1;
        }
    }

    private Dictionary<ItemData, ItemStack> items = new Dictionary<ItemData, ItemStack>();
    private Entity playerEntity;
    
    private HealOnDamage healOnDamageEffect;
    private StompDamage stompDamageEffect;
    private FallDamageBonus fallDamageBonusEffect;

    public event Action<ItemData, ItemStack> OnItemAdded;
    public event Action<ItemData, ItemStack> OnItemStackChanged;
    public event Action<ItemData> OnItemRemoved;

    public IReadOnlyDictionary<ItemData, ItemStack> Items => items;

    void Awake()
    {
        playerEntity = GetComponent<Entity>();
        if (playerEntity == null)
        {
            Debug.Log("Player requires a Entity script");
        }
    }

    public void AddItem(ItemData itemData)
    {
        if (itemData == null) return;

        if (items.TryGetValue(itemData, out ItemStack existingStack))
        {
            // Item already exists, try to stack on eixs
            if (itemData.canStack && existingStack.count < itemData.maxStacks)
            {
                existingStack.count++;
                ApplyStatModifier(itemData, existingStack);
                ApplySpecialEffect(itemData, existingStack); // Update special effects with new stack count
                OnItemStackChanged?.Invoke(itemData, existingStack);


                Debug.Log($"Stacked {itemData.itemName} x{existingStack.count}");

            }
            else
            {
                Debug.Log($"Cannot stack {itemData.itemName} - max stacks reached or item doesn't stack");
            }
        }
        else
        {
            // New item
            ItemStack newStack = new ItemStack(itemData);
            items.Add(itemData, newStack);
            ApplyStatModifier(itemData, newStack);
            ApplySpecialEffect(itemData, newStack); // Apply special effects
            OnItemAdded?.Invoke(itemData, newStack);


            Debug.Log($"Added new item: {itemData.itemName}");

        }

    }


    private void ApplyStatModifier(ItemData itemData, ItemStack stack)
    {
        if (playerEntity == null || playerEntity.Stats == null) return; // cant do shit ma man

        float baseValue = playerEntity.Stats.BaseValueForStat(itemData.statType);

        // we can use this if we want to stack the items and multiple their values but number of stack counts
        // float actualValue = CalculateStackedValue(itemData, stack.count);     < -----------
        float incrementalValue = itemData.value; // for now we use basic add on add
        

        StatModifier modifier = itemData.operatorType switch
        {
            OperatorType.Add => new BasicStatsModifier(itemData.statType, itemData.duration, v => v + incrementalValue),
            OperatorType.Multiply => new BasicStatsModifier(itemData.statType, itemData.duration, v => v * incrementalValue),
            OperatorType.Percentage => new BasicStatsModifier(itemData.statType, itemData.duration, v => v + baseValue * incrementalValue),
            _ => throw new ArgumentOutOfRangeException()
        };

        // Track the modifier for removal later
        stack.activeModifiers.Add(modifier);
        playerEntity.Stats.Mediator.AddModifier(modifier);


        Debug.Log($"Applied {itemData.statType} modifier: {incrementalValue} ({itemData.operatorType})");

    }

    private float CalculateStackedValue(ItemData itemData, int stackCount)
    {
        if (stackCount <= 1) return itemData.value;

        return itemData.stackingType switch
        {
            StackingType.Linear => itemData.value * stackCount,
            _ => itemData.value * stackCount
        };
    }

    public void RemoveItem(ItemData itemData)
    {
        if (items.TryGetValue(itemData, out ItemStack stack))
        {
            // Remove all stat modifiers
            foreach (var modifier in stack.activeModifiers)
            {
                modifier.Dispose();
            }

            items.Remove(itemData);
            OnItemRemoved?.Invoke(itemData);
            Debug.Log($"Removed item: {itemData.itemName}");
        }
    }
    
    public int GetItemCount(ItemData itemData)
    {
        return items.TryGetValue(itemData, out ItemStack stack) ? stack.count : 0;
    }
    
    public bool HasItem(ItemData itemData)
    {
        return items.ContainsKey(itemData);
    }
    
    private void ApplySpecialEffect(ItemData itemData, ItemStack stack)
    {
        if (playerEntity == null) return;
        if (itemData.effectKind == ItemEffectKind.None) return;

        switch (itemData.effectKind)
        {
            case ItemEffectKind.HealOnDamage:
                if (healOnDamageEffect == null)
                {
                    healOnDamageEffect = new HealOnDamage(
                        playerEntity,
                        itemData.healOnDamagePercentPerStack,
                        stack.count,
                        itemData.effectDuration
                    );
                    stack.activeEffect = healOnDamageEffect;
                    Debug.Log($"[Effect] Activated Heal On Damage ({stack.count} stacks)");
                }
                else
                {
                    healOnDamageEffect.AddStack(1);
                    Debug.Log($"[Effect] Heal On Damage stacked ({stack.count} stacks)");
                }
                break;

            case ItemEffectKind.StompDamage:
                if (stompDamageEffect == null)
                {
                    stompDamageEffect = new StompDamage(
                        playerEntity,
                        itemData.stompDamagePerStack,
                        itemData.stompBounceForce,
                        stack.count,
                        itemData.effectDuration
                    );
                    stack.activeEffect = stompDamageEffect;
                    Debug.Log($"[Effect] Activated Stomp Damage ({stack.count} stacks) - {itemData.stompDamagePerStack} damage per stack");
                }
                else
                {
                    stompDamageEffect.AddStack(1);
                    Debug.Log($"[Effect] Stomp Damage stacked ({stack.count} stacks)");
                }
                break;

            case ItemEffectKind.FallDamageBonus:
                if (fallDamageBonusEffect == null)
                {
                    fallDamageBonusEffect = new FallDamageBonus(
                        playerEntity,
                        itemData.fallDamageBonusPerMeter * itemData.fallDamageBonusPerStack,
                        stack.count,
                        itemData.effectDuration
                    );
                    stack.activeEffect = fallDamageBonusEffect;
                    Debug.Log($"[Effect] Activated Fall Damage Bonus ({stack.count} stacks) - {itemData.fallDamageBonusPerMeter} damage per meter");
                }
                else
                {
                    fallDamageBonusEffect.AddStack(1);
                    Debug.Log($"[Effect] Fall Damage Bonus stacked ({stack.count} stacks)");
                }
                break;
        }
    }
    
    void Update()
    {
        float dt = Time.deltaTime;
        healOnDamageEffect?.Update(dt);
        stompDamageEffect?.Update(dt);
        fallDamageBonusEffect?.Update(dt);
    }
    
    void OnDestroy()
    {
        healOnDamageEffect?.Dispose();
        stompDamageEffect?.Dispose();
        fallDamageBonusEffect?.Dispose();
    }
}
