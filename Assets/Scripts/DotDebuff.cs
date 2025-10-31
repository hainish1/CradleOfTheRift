using UnityEngine;
using System.Collections.Generic;

// Handles all active DOT effects on an enemy
// Gets added to enemy gameobjects when they get hit with DOT items
public class DotDebuff : MonoBehaviour
{
    // flag to prevent DOT recursion 
    public static bool IsProcessingDotDamage { get; private set; }
    
    // tracks a single DOT instance 
    private class DotEffect
    {
        public float damagePerTick;
        public float tickInterval;  // how often to tick
        public float duration;  // how long total
        public float nextTickTime;  
        public float endTime;  
        public Entity source;  // who applied this (for credit)
        public bool canStack;
        public string id; 

        public DotEffect(float damage, float interval, float duration, Entity source, bool stack = true, string id = "", bool applyImmediately = false)
        {
            this.damagePerTick = damage;
            this.tickInterval = interval;
            this.duration = duration;
            this.nextTickTime = applyImmediately ? Time.time : Time.time + interval;
            this.endTime = Time.time + duration;
            this.source = source;
            this.canStack = stack;
            this.id = id;
        }

        public bool IsExpired() => Time.time >= endTime;
        public bool ShouldTick() => Time.time >= nextTickTime;
        public void ResetTickTimer() => nextTickTime = Time.time + tickInterval;
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

    // called when an enemy gets hit with a DOT attack
    public void AddDot(float damagePerTick, float tickInterval, float duration, Entity source, bool canStack = true, string id = "", int maxStacks = 5, bool applyImmediately = false)
    {
        // if DOT doesnt stack, just refresh existing one
        if (!canStack && !string.IsNullOrEmpty(id))
        {
            var existing = activeDots.Find(dot => dot.id == id);
            if (existing != null)
            {
                existing.endTime = Time.time + duration;
                existing.damagePerTick = Mathf.Max(existing.damagePerTick, damagePerTick);
                Debug.Log($"[DOT] Refreshed on {gameObject.name}");
                return;
            }
        }

        // if DOT stacks, check if we hit max
        if (canStack && !string.IsNullOrEmpty(id))
        {
            int currentStacks = activeDots.FindAll(dot => dot.id == id).Count;
            if (currentStacks >= maxStacks)
            {
                var oldest = activeDots.Find(dot => dot.id == id);
                if (oldest != null)  // shouldnt be null but just in case
                {
                    oldest.endTime = Time.time + duration;
                    oldest.nextTickTime = Time.time + tickInterval;
                    Debug.Log($"[DOT] Max stacks ({currentStacks}/{maxStacks}), refreshed oldest");
                }
                return;
            }
        }

        activeDots.Add(new DotEffect(damagePerTick, tickInterval, duration, source, canStack, id, applyImmediately));
        int newCount = activeDots.FindAll(dot => dot.id == id).Count;
        string immediateText = applyImmediately ? " [instant]" : "";
        Debug.Log($"[DOT] Added: {damagePerTick}dmg every {tickInterval}s for {duration}s (Stack: {newCount}/{maxStacks}){immediateText}");
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
        
        damageable.TakeDamage(dot.damagePerTick);
        
        var enemy = GetComponent<Enemy>();
        if (enemy && dot.source)  // report for stats tracking
            CombatEvents.ReportDamage(dot.source, enemy, dot.damagePerTick);

        if (showDotNumbers)
            ShowDotNumber(dot.damagePerTick);

        Debug.Log($"[DOT] {gameObject.name} took {dot.damagePerTick} damage");
        
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

