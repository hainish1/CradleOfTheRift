using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EffectSpec))]
public class EffectSpecDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float space = EditorGUIUtility.standardVerticalSpacing;
        if (!property.isExpanded)
            return line;
        int extraLines = 3 + FieldsFor(GetKind(property)).Length;
        return line + extraLines * (line + space);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float space = EditorGUIUtility.standardVerticalSpacing;
        var kindProp = property.FindPropertyRelative("kind");
        var durationProp = property.FindPropertyRelative("duration");
        var descriptionProp = property.FindPropertyRelative("description");
        

        // make list element show the selected kind
        var kind = (ItemEffectKind)kindProp.enumValueIndex;
        label = new GUIContent(kind.ToString());
        var r = new Rect(position.x, position.y, position.width, line);
        property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, label, true);

        if (!property.isExpanded) return;
        EditorGUI.indentLevel++;

        r.y += line + space;
        EditorGUI.PropertyField(r, kindProp);
        r.y += line + space;
        EditorGUI.PropertyField(r, durationProp);
        r.y += line + space;
        EditorGUI.PropertyField(r, descriptionProp);

        foreach (string fieldName in FieldsFor(kind))
        {
            var p = property.FindPropertyRelative(fieldName);
            if (p == null) continue;
            r.y += line + space;
            EditorGUI.PropertyField(r, p, includeChildren: true);
        }
        EditorGUI.indentLevel--;
    }

    static ItemEffectKind GetKind(SerializedProperty property)
    {
        var kindProp = property.FindPropertyRelative("kind");
        return (ItemEffectKind)kindProp.enumValueIndex;
    }

    static string[] FieldsFor(ItemEffectKind kind) => kind switch
    {
        ItemEffectKind.HealOnDamage => new[]
        {
            "healOnDamagePercentPerStack",
        },

        ItemEffectKind.HealOnPoison => new[]
        {
            "healOnPoisonRange",
            "healPerPoisonStackPerSecond",
        },

        ItemEffectKind.StompDamage => new[]
        {
            "stompDamagePerStack","stompBounceForce",
        },

        ItemEffectKind.FallDamageBonus => new[]
        {
            "fallDamageBonusPerMeter", "fallDamageBonusPerStack",
        },

        ItemEffectKind.DotOnHit => new[]
        {
            "dotDamagePerTick",
            "dotTickInterval",
            "dotDuration",
            "dotDamagePerStack",
            "dotCanStack",
            "dotMaxStacks",
            "dotApplyImmediately",
        },

        ItemEffectKind.HomingProjectiles => new[]
        {
            "numberOfProjectiles", "projectileDamageMultiplier",
        },

        ItemEffectKind.ExplosiveProjectiles => new[]
        {
            "explosiveAoeRadius",
            "explosiveAoeDamageMultiplier",
            "explosiveMaxRange",
            "explosiveFireballSize",
            "explosiveVFX",
        },

        ItemEffectKind.ChainLightning => new[]
        {
            "chainDamagePercent",
            "maxChainCount",
            "chainRange",
            "chainLightningVFX",
        },

        ItemEffectKind.BounceProjectiles => new[]
        {
            "bounceRange",
            "maxBounceCount",
            "damageMultiplierPerBounce",
            "bounceVFX",
        },

        ItemEffectKind.DelayedProjectiles => new[]
        {
            "delayedDamageTime",
            "delayedDamageMultiplier",
            "delayedMarkVFX",
        },

        ItemEffectKind.DashDamage => new[]
        {
            "dashDamage", "dashDamageRange",
        },
        ItemEffectKind.PoisonPoolOnDash => new[]
        {
            "poisonPoolRadius", "poisonPoolLifetime",
        },

        ItemEffectKind.PoisonPoolProjectile => new[]
        {
            "poisonPoolRadius", "poisonPoolLifetime", "poisonPoolProjectileVFX",
        },

        ItemEffectKind.BurnOnDamage => new[]
        {
            "burnAuraDamagePerSecond", "burnAuraRange", "burnAuraTickInterval",
        },

        ItemEffectKind.ElementFusion => new[]
        {
            "fusionTriggerElement", "fusionEffectElement",
        },

        ItemEffectKind.ArcStrike => new[]
        {
            "arcStrikeDamage", "arcStrikeRange", "arcStrikePoissonLambda",
        },

        ItemEffectKind.ElementReactionExplosion => new[]
        {
            "elementReactionExplosionDamage", "elementReactionExplosionRadius",
            "elementReactionExplosionCooldown", "elementReactionExplosionVFX",
        },

        ItemEffectKind.LightningStrike => new[]
        {
            "playerLightningStrikeDamage", "playerLightningStrikeRadius",
            "playerLightningStrikeInterval", "playerLightningStrikeElectrifyDamage",
            "playerLightningStrikeVFX",
        },

        ItemEffectKind.FlyFire => new[]
        {
            "flyingFireDamage", "flyingFireRadius", "flyingFireTickInterval", "flyingFireOffset",
        },

        ItemEffectKind.OrbitingFireballs => new[]
        {
            "orbitingFireballDamage", "orbitingFireballRadius", "orbitingFireballRotationSpeed",
        },

        ItemEffectKind.LightningDash => new[]
        {
            "lightningDashDamage", "lightningDashChainCount", "lightningDashChainRange",
        },

        ItemEffectKind.OrbitingFireballsTest => new[]
        {
            "orbitingFireballTestDamage", "orbitingFireballTestDamageThreshold",
            "orbitingFireballTestMaxCount", "orbitingFireballTestLifetime",
        },

        ItemEffectKind.ChainLightningTest => new[]
        {
            "chainLightningTestStrikeInterval", "chainLightningTestBuffDuration",
            "chainLightningTestChainDamagePercent", "chainLightningTestMaxDepth",
            "chainLightningTestBranchesPerNode", "chainLightningTestChainRange",
        },

        ItemEffectKind.PassThroughSpear => new[]
        {
            "passThroughEnemyCount",
        },

        ItemEffectKind.ToxicAttackSpeed => new[]
        {
            "toxicAttackSpeedRange",
            "toxicAttackSpeedPerStack",
        },

        ItemEffectKind.FlameTrail => new[]
        {
            "flameTrailDamage", "flameTrailRadius", "flameTrailLifetime", "flameTrailSpawnInterval",
        },

        ItemEffectKind.FinisherStrike => new[]
        {
            "finisherDamageMultiplier", "finisherRangeMultiplier",
        },

        ItemEffectKind.XPGrant => new[]
        {
            "xpGrantAmount",
        },

        ItemEffectKind.PureCore => new[]
        {
            "pureCoreDamageMultiplier",
            "pureCoreHealthMultiplier",
        },

        _ => Array.Empty<string>(),
    };
}
