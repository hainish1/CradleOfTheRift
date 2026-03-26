using System;
using UnityEngine;

public static class LightningStrikeEvents
{
    public static event Action<Entity, Vector3, float> StrikeLanded;
    public static event Action<Entity, float> PlayerSelfHit;

    public static void FireStrikeLanded(Entity owner, Vector3 pos, float dmg)
        => StrikeLanded?.Invoke(owner, pos, dmg);

    public static void FirePlayerSelfHit(Entity owner, float dmg)
        => PlayerSelfHit?.Invoke(owner, dmg);
}
