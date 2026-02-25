using System;
using System.Collections.Generic;
using UnityEngine;

public class PureCore : IDisposable
{
    private static readonly HashSet<ItemEffectKind> ElementalKinds = new HashSet<ItemEffectKind>
    {
        ItemEffectKind.BurnOnDamage,
        ItemEffectKind.FlyFire,
        ItemEffectKind.OrbitingFireballs,
        ItemEffectKind.OrbitingFireballsTest,
        ItemEffectKind.FlameTrail,
        ItemEffectKind.ExplosiveProjectiles,
        ItemEffectKind.ChainLightning,
        ItemEffectKind.ChainLightningTest,
        ItemEffectKind.LightningStrike,
        ItemEffectKind.LightningDash,
        ItemEffectKind.ElementReactionExplosion,
        ItemEffectKind.DotOnHit,
        ItemEffectKind.PoisonPoolOnDash,
        ItemEffectKind.ToxicAttackSpeed,
        ItemEffectKind.ArcStrike,
    };

    private readonly Entity owner;
    private readonly PlayerInventory inventory;
    private readonly float damageMultiplier;
    private readonly float healthMultiplier;
    private readonly Action<ItemData, PlayerInventory.ItemStack> onInventoryChanged;
    private readonly Action<ItemData> onItemRemoved;
    private bool disposed;

    private StatModifier healthModifier;
    private StatModifier meleeDamageModifier;
    private StatModifier projectileDamageModifier;
    private StatModifier shockwaveDamageModifier;

    public bool IsDisposed => disposed;

    public PureCore(Entity owner, PlayerInventory inventory, float damageMultiplier, float healthMultiplier)
    {
        this.owner = owner;
        this.inventory = inventory;
        this.damageMultiplier = Mathf.Max(1f, damageMultiplier);
        this.healthMultiplier = Mathf.Max(1f, healthMultiplier);
        onInventoryChanged = (_, __) => RefreshBuff();
        onItemRemoved = _ => RefreshBuff();

        if (inventory != null)
        {
            inventory.OnItemAdded += onInventoryChanged;
            inventory.OnItemStackChanged += onInventoryChanged;
            inventory.OnItemRemoved += onItemRemoved;
        }

        RefreshBuff();
    }

    private void RefreshBuff()
    {
        if (disposed || owner == null || owner.Stats == null || inventory == null) return;

        bool hasElemental = HasAnyElementalItem();
        RemoveModifiers();

        if (!hasElemental)
        {
            var stats = owner.Stats;
            if (stats != null && stats.Mediator != null)
            {
                healthModifier = new BasicStatsModifier(StatType.Health, -1f, v => v * healthMultiplier);
                meleeDamageModifier = new BasicStatsModifier(StatType.MeleeDamage, -1f, v => v * damageMultiplier);
                projectileDamageModifier = new BasicStatsModifier(StatType.ProjectileDamage, -1f, v => v * damageMultiplier);
                shockwaveDamageModifier = new BasicStatsModifier(StatType.ShockwaveDamage, -1f, v => v * damageMultiplier);

                stats.Mediator.AddModifier(healthModifier);
                stats.Mediator.AddModifier(meleeDamageModifier);
                stats.Mediator.AddModifier(projectileDamageModifier);
                stats.Mediator.AddModifier(shockwaveDamageModifier);
            }
        }
    }

    private bool HasAnyElementalItem()
    {
        if (inventory?.Items == null) return false;

        foreach (var kv in inventory.Items)
        {
            var data = kv.Key;
            if (data?.effects == null) continue;

            foreach (var effect in data.effects)
            {
                if (ElementalKinds.Contains(effect.kind))
                    return true;
            }
        }

        return false;
    }

    private void RemoveModifiers()
    {
        healthModifier?.Dispose();
        healthModifier = null;
        meleeDamageModifier?.Dispose();
        meleeDamageModifier = null;
        projectileDamageModifier?.Dispose();
        projectileDamageModifier = null;
        shockwaveDamageModifier?.Dispose();
        shockwaveDamageModifier = null;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (inventory != null)
        {
            inventory.OnItemAdded -= onInventoryChanged;
            inventory.OnItemStackChanged -= onInventoryChanged;
            inventory.OnItemRemoved -= onItemRemoved;
        }

        RemoveModifiers();
    }
}
