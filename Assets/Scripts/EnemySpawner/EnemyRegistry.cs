using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A static registry that tracks all currently active enemies in the scene.
/// Enemies self-register via OnEnable/OnDisable, so this list is always up to date
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<EnemyRange> _flyers = new List<EnemyRange>();
    private static readonly List<EnemyMelee> _walkers = new List<EnemyMelee>();

    public static IReadOnlyList<EnemyRange> Flyers => _flyers;
    public static IReadOnlyList<EnemyMelee> Walkers => _walkers;

    public static void RegisterFlyer(EnemyRange enemy)
    {
        if (!_flyers.Contains(enemy))
            _flyers.Add(enemy);
    }

    public static void UnregisterFlyer(EnemyRange enemy)
    {
        _flyers.Remove(enemy);
    }

    public static void RegisterWalker(EnemyMelee enemy)
    {
        if (!_walkers.Contains(enemy))
            _walkers.Add(enemy);
    }

    public static void UnregisterWalker(EnemyMelee enemy)
    {
        _walkers.Remove(enemy);
    }
}
