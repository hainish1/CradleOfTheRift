using UnityEngine;
using System.Collections.Generic;

// Handles all active DOT effects on an enemy
// Gets added to enemy gameobjects when they get hit with DOT items
public class DotDebuff : MonoBehaviour
{
    // flag to prevent DOT recursion 
    public static bool IsProcessingDotDamage { get; private set; }
    
    private class DotEffect
    {
        public float baseDamagePerTick;
        public float damagePerStack;
        public float tickInterval;
        public float duration;
        public float nextTickTime;
        public float endTime;
        public Entity source;
        public bool canStack;
        public string id;
        public int stackCount;

        public DotEffect(float baseDamage, float damagePerStack, float interval, float duration, Entity source, bool stack = true, string id = "", bool applyImmediately = false, int initialStacks = 1)
        {
            this.baseDamagePerTick = baseDamage;
            this.damagePerStack = damagePerStack;
            this.tickInterval = interval;
            this.duration = duration;
            this.nextTickTime = applyImmediately ? Time.time : Time.time + interval;
            this.endTime = Time.time + duration;
            this.source = source;
            this.canStack = stack;
            this.id = id;
            this.stackCount = initialStacks;
        }

        public float GetTotalDamagePerTick()
        {
            return baseDamagePerTick + (damagePerStack * (stackCount - 1));
        }

        public bool IsExpired() => Time.time >= endTime;
        public bool ShouldTick() => Time.time >= nextTickTime;
        public void ResetTickTimer() => nextTickTime = Time.time + tickInterval;
        public void RefreshDuration(float newDuration) => endTime = Time.time + newDuration;
        public void AddStack(int count = 1) => stackCount += count;
    }

    private List<DotEffect> activeDots = new List<DotEffect>();
    private IDamageable damageable;

    [Header("Visual Feedback")]
    [SerializeField] private bool showDotNumbers = true;
    [SerializeField] private Color dotColor = new Color(0.8f, 0.2f, 1f);
    
    void Awake()
    {
        damageable = GetComponent<IDamageable>();
    }

    void Update()
    {
        ProcessDots();
    }

    public void AddDot(float baseDamagePerTick, float damagePerStack, float tickInterval, float duration, Entity source, bool canStack = true, string id = "", int maxStacks = 5, bool applyImmediately = false)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[DOT] Cannot add DOT without ID");
            return;
        }

        var existing = activeDots.Find(dot => dot.id == id);
        
        if (existing != null)
        {
            if (!canStack)
            {
                existing.RefreshDuration(duration);
                existing.baseDamagePerTick = Mathf.Max(existing.baseDamagePerTick, baseDamagePerTick);
                Debug.Log($"[DOT] Refreshed (no stack) on {gameObject.name}");
                return;
            }

            if (existing.stackCount >= maxStacks)
            {
                existing.RefreshDuration(duration);
                if (applyImmediately)
                {
                    existing.nextTickTime = Time.time;
                }
                Debug.Log($"[DOT] Max stacks ({existing.stackCount}/{maxStacks}), refreshed on {gameObject.name}");
                return;
            }

            existing.AddStack(1);
            existing.RefreshDuration(duration);
            if (applyImmediately)
            {
                existing.nextTickTime = Time.time;
            }
            float totalDamage = existing.GetTotalDamagePerTick();
            Debug.Log($"[DOT] Stacked: {totalDamage}dmg/tick ({existing.stackCount}/{maxStacks} stacks) on {gameObject.name}");
            return;
        }

        activeDots.Add(new DotEffect(baseDamagePerTick, damagePerStack, tickInterval, duration, source, canStack, id, applyImmediately, initialStacks: 1));
        string immediateText = applyImmediately ? " [instant]" : "";
        Debug.Log($"[DOT] Added: {baseDamagePerTick}dmg/tick every {tickInterval}s for {duration}s (1/{maxStacks} stacks){immediateText} on {gameObject.name}");
    }

    private void ProcessDots()
    {
        if (damageable == null || damageable.IsDead)
        {
            activeDots.Clear();  // enemy dead, clear all DOTs
            return;
        }

        // iterate backwards so we can remove while looping
        for (int i = activeDots.Count - 1; i >= 0; i--)
        {
            var dot = activeDots[i];

            if (dot.IsExpired())
            {
                activeDots.RemoveAt(i);  // DOT ran out
                continue;
            }

            if (dot.ShouldTick())
            {
                ApplyDotDamage(dot);
                dot.ResetTickTimer();
            }
        }
    }

    private void ApplyDotDamage(DotEffect dot)
    {
        if (damageable == null || damageable.IsDead) return;

        IsProcessingDotDamage = true;
        
        float totalDamage = dot.GetTotalDamagePerTick();
        damageable.TakeDamage(totalDamage);
        
        var enemy = GetComponent<Enemy>();
        if (enemy && dot.source)
            CombatEvents.ReportDamage(dot.source, enemy, totalDamage);

        if (showDotNumbers)
            ShowDotNumber(totalDamage);

        Debug.Log($"[DOT] {gameObject.name} took {totalDamage} damage ({dot.stackCount} stacks)");
        
        IsProcessingDotDamage = false;
    }

    private void ShowDotNumber(float damage)
    {
        // we have the dummy so don't need this
        Debug.Log($"[DOT Visual] {gameObject.name} -{damage:F1}", gameObject);
    }

    public int GetActiveDotCount() => activeDots.Count;

    public void ClearAllDots()
    {
        activeDots.Clear();
    }

    void OnDestroy()
    {
        activeDots.Clear();
    }
}

