using System;
using UnityEngine;

[Serializable]
public class EnemyType
{
    public string name;
    public GameObject prefab;
    public int cost;
    public bool isFlying;

    [Min(1)]
    [Tooltip("Relative spawn probability. Higher = more likely to be chosen. e.g. 10 = common, 3 = rare.")]
    public int spawnWeight = 10;
}