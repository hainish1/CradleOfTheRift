using System;
using System.Collections;
using UnityEngine;
    using UnityEngine.SceneManagement;

public class PlayerHealth : HealthController
{
    // note to self - THIS is player MANAGER that INHERITS from entity
    private Entity playerEntity;
    public event Action LoseScreen;
    public static bool GameIsOver = false; // true when win/lose screen is up
    public event Action<float, float> healthChanged;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    [Header("Health REGEN Settings")]
    [SerializeField] private float regenStartDelay = 2f; // delay after damage
    [SerializeField] private float regenTickRate = .1f; // how often does regen tick
    [SerializeField] private float regenMinRate = 0.5f; // per second hp 
    [SerializeField] private float regenMaxRate = 8f; // per second hp at max
    [SerializeField] private float regenRampupTime = 5f; // time needed to reach max speed

    [Space]
    [Header("Health VFX")]
    [SerializeField] private GameObject healthRegenVFXPrefab;
    [SerializeField] private Transform regenVfxAttachPoint;
    [SerializeField] private float normalHealVFXDuration = 4f;
    private GameObject activeRegenVFX;

    private float regenTimer = 0f;
    private float timeSinceLastDamage = 0f;
    private bool isRegenerating = false;
    private Coroutine regenCoroutine;



    private bool canTakeDamage = true;

    public static PlayerHealth instance;

    

    protected override void Awake()
    {
        base.Awake();
        instance = this;
        GameIsOver = false;

    }

    void Start()
    {
        playerEntity = GetComponent<Entity>();
        GameIsOver = false;
        if (playerEntity != null)
        {
            // maxHealth = Mathf.RoundToInt(playerEntity.Stats.Health);
            maxHealth = Mathf.Max(1f, playerEntity.Stats.Health);

            currentHealth = maxHealth;

            Debug.Log($"Player health initialized with heatlh-statsL {maxHealth}");
            healthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    void Update()
    {
        if (playerEntity == null || playerEntity.Stats == null)
        {
            return;
        }
        // int newMaxHealth = Mathf.RoundToInt(playerEntity.Stats.Health);
        float newMaxHealth = playerEntity.Stats.Health;

        // If max health changed (due to item pickup), adjust current health proportionally
        if (newMaxHealth != maxHealth)
        {
            float healthRatio = (float)currentHealth / maxHealth;
            maxHealth = newMaxHealth;
            // currentHealth = Mathf.RoundToInt(healthRatio * maxHealth);
            currentHealth = maxHealth;

            Debug.Log($"Max health updated to: {maxHealth}, Current: {currentHealth}");
            healthChanged?.Invoke(currentHealth, maxHealth);

            PlayInstantHealVFX();
        }

        if(!IsDead && currentHealth < maxHealth)
        {
            timeSinceLastDamage += Time.deltaTime;

            if(!isRegenerating && timeSinceLastDamage >= regenStartDelay)
            {
                isRegenerating = true;
                // do the vfx
                StartRegenVFX();
                regenCoroutine = StartCoroutine(RegenRoutine());
            }
        }
    }

    private IEnumerator RegenRoutine()
    {
        regenTimer = 0f;
        while(currentHealth < maxHealth)
        {
            regenTimer += regenTickRate;

            float t = Mathf.Clamp01(regenTimer / regenRampupTime);
            float regenPerSecond = Mathf.Lerp(regenMinRate, regenMaxRate, t);

            float amount = regenPerSecond * regenTickRate;
            Heal(amount);
            healthChanged?.Invoke(currentHealth, maxHealth);

            yield return new WaitForSeconds(regenTickRate);
        }

        isRegenerating = false;
        regenCoroutine = null;
        StopRegenVFX();
    }



    protected override void Die()
    {
        Debug.Log("[PLAYER HEALTH] Player is DEADDD lmao");
        this.LoseScreen?.Invoke();
        GameIsOver = true;


        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // end movement or change scene here if we want
    }

    // maybe useful later idk
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    public override void TakeDamage(float damage)
    {
        if (canTakeDamage == false || IsDead) return;
        
        base.TakeDamage(damage);
        healthChanged?.Invoke(currentHealth, maxHealth);
        
        Debug.Log($"[PLAYER HEALTH] Player took {damage} damage, current health: {currentHealth}/{maxHealth}");
    
        // stop regen on damage
        timeSinceLastDamage = 0;
        StopRegenVFX();
        if(regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            
            regenCoroutine = null;
        }
        isRegenerating = false;

    }

    public virtual void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        healthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log("Health Fully Resotored");

        
    }

    public virtual void SetCanTakeDamage(bool enable)
    {
        this.canTakeDamage = enable;
    }

    public override void Heal(float amount)
    {


        base.Heal(amount);
        healthChanged?.Invoke(currentHealth, maxHealth); // notify UI

    }




    private void StartRegenVFX()
    {
        if(healthRegenVFXPrefab == null)return;
        if(activeRegenVFX != null) return;

        Transform parent = regenVfxAttachPoint != null ? regenVfxAttachPoint : transform;
        activeRegenVFX = Instantiate(healthRegenVFXPrefab, parent.position, parent.rotation, parent);
    }


    private void StopRegenVFX()
    {
        if(activeRegenVFX != null)
        {
            Destroy(activeRegenVFX);
            activeRegenVFX = null;
        }
    }


    private void PlayInstantHealVFX()
    {
        if(healthRegenVFXPrefab == null)return;
        Transform parent = regenVfxAttachPoint != null ? regenVfxAttachPoint : transform;
        GameObject vfx = Instantiate(healthRegenVFXPrefab, parent.position, parent.rotation, parent);

        Destroy(vfx, normalHealVFXDuration);
    }
}
