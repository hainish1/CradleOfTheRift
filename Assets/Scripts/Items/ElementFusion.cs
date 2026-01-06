using System;
using UnityEngine;

public class ElementFusion : IDisposable
{
    private Entity owner;
    private ElementType triggerElement;
    private ElementType effectElement;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    public ElementFusion(
        Entity owner, 
        ElementType triggerElement, 
        ElementType effectElement,
        int initialStacks = 1,
        float durationSec = -1f)
    {
        this.owner = owner;
        this.triggerElement = triggerElement;
        this.effectElement = effectElement;
        this.stacks = Mathf.Max(1, initialStacks);
        this.duration = durationSec;
        this.timer = durationSec;

        ElementSystem.AddTempRule(triggerElement, effectElement);
        
        Debug.Log($"[ElementFusion] Activated: {triggerElement} can now trigger {effectElement}");
    }

    public void AddStack(int count = 1)
    {
        stacks += Mathf.Max(1, count);
        Debug.Log($"[ElementFusion] Stacked: {stacks} stacks");
    }

    public void Update(float dt)
    {
        if (duration < 0f || disposed) return;
        timer -= dt;
        if (timer <= 0f) Dispose();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        
        ElementSystem.RemoveTempRule(triggerElement, effectElement);
        
        Debug.Log($"[ElementFusion] Disposed: {triggerElement} can no longer trigger {effectElement}");
    }
}

