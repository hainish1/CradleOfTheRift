using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class BossHealthUI : MonoBehaviour
{
    [SerializeField] private float damageHoldDuration = 0.5f;
    [SerializeField] private float damageDrainDuration = 0.4f;
    [SerializeField] private Texture2D slimeBossIcon;
    [SerializeField] private Texture2D revenantBossIcon;
    [SerializeField] private Texture2D rockBossIcon;

    private readonly List<BossSpawner> subscribedSpawners = new();

    private EnemyHealth currentBoss;
    private VisualElement bossContainer;
    private VisualElement bossIcon;
    private ProgressBar healthBar;
    private ProgressBar damageBar;
    private Coroutine drainCoroutine;

    private void Awake()
    {
        CacheUi();
    }

    private void OnEnable()
    {
        SubscribeToSpawners();
    }

    private void Start()
    {
        CacheUi();
        SubscribeToSpawners();
    }

    private void OnDisable()
    {
        UnsubscribeFromSpawners();
        UnbindCurrentBoss();
    }

    private void SubscribeToSpawners()
    {
        foreach (var spawner in FindObjectsByType<BossSpawner>(FindObjectsSortMode.None))
        {
            if (subscribedSpawners.Contains(spawner))
                continue;

            spawner.BossSpawned += OnBossSpawned;
            subscribedSpawners.Add(spawner);

            if (spawner.ActiveBoss != null)
                BindToBoss(spawner.ActiveBoss);
        }
    }

    private void UnsubscribeFromSpawners()
    {
        foreach (var spawner in subscribedSpawners)
        {
            if (spawner != null)
                spawner.BossSpawned -= OnBossSpawned;
        }

        subscribedSpawners.Clear();
    }

    private void OnBossSpawned(EnemyHealth boss)
    {
        BindToBoss(boss);
    }

    public void BindToBoss(EnemyHealth boss)
    {
        CacheUi();
        UnbindCurrentBoss();

        currentBoss = boss;
        if (currentBoss == null || bossContainer == null || healthBar == null || damageBar == null)
            return;

        currentBoss.HealthChanged += OnBossHealthChanged;
        currentBoss.EnemyDied += OnBossDied;

        ApplyBossIcon(currentBoss);
        healthBar.lowValue = 0f;
        damageBar.lowValue = 0f;
        healthBar.highValue = currentBoss.GetMaxHealth();
        damageBar.highValue = currentBoss.GetMaxHealth();

        OnBossHealthChanged(currentBoss.GetCurrentHealth(), currentBoss.GetMaxHealth());
        Show();
    }

    private void UnbindCurrentBoss()
    {
        if (currentBoss != null)
        {
            currentBoss.HealthChanged -= OnBossHealthChanged;
            currentBoss.EnemyDied -= OnBossDied;
            currentBoss = null;
        }

        if (drainCoroutine != null)
        {
            StopCoroutine(drainCoroutine);
            drainCoroutine = null;
        }
    }

    private void OnBossHealthChanged(float currentHealth, float maxHealth)
    {
        if (healthBar == null || damageBar == null)
            return;

        float previousHealth = healthBar.value;

        healthBar.highValue = maxHealth;
        damageBar.highValue = maxHealth;
        healthBar.value = currentHealth;
        healthBar.title = $"BOSS  {(int)currentHealth} / {(int)maxHealth}";

        if (currentHealth < previousHealth)
        {
            damageBar.value = previousHealth;

            if (drainCoroutine != null)
                StopCoroutine(drainCoroutine);

            drainCoroutine = StartCoroutine(DrainDamageBar(previousHealth, currentHealth));
        }
        else
        {
            damageBar.value = currentHealth;
        }
    }

    private IEnumerator DrainDamageBar(float fromValue, float toValue)
    {
        yield return new WaitForSeconds(damageHoldDuration);

        float elapsed = 0f;
        while (elapsed < damageDrainDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / damageDrainDuration);
            t = 1f - (1f - t) * (1f - t);
            damageBar.value = Mathf.Lerp(fromValue, toValue, t);
            yield return null;
        }

        damageBar.value = toValue;
        drainCoroutine = null;
    }

    private void OnBossDied(EnemyHealth deadBoss)
    {
        Hide();
        UnbindCurrentBoss();
    }

    private void Show()
    {
        if (bossContainer != null)
            bossContainer.style.display = DisplayStyle.Flex;
    }

    private void Hide()
    {
        if (bossContainer != null)
            bossContainer.style.display = DisplayStyle.None;
    }

    private void CacheUi()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        bossContainer = root.Q<VisualElement>("BossHealthContainer");
        bossIcon = root.Q<VisualElement>("BossIcon");
        healthBar = root.Q<ProgressBar>("BossHealthBar");
        damageBar = root.Q<ProgressBar>("BossDamageBar");

        if (bossContainer == null || bossIcon == null || healthBar == null || damageBar == null)
        {
            Debug.LogWarning("[BossHealthUI] Boss health elements were not found in the active HUD document.");
            return;
        }

        Hide();
    }

    private void ApplyBossIcon(EnemyHealth boss)
    {
        if (bossIcon == null)
            return;

        var icon = ResolveBossIcon(boss);
        if (icon != null)
        {
            bossIcon.style.backgroundImage = new StyleBackground(icon);
        }
    }

    private Texture2D ResolveBossIcon(EnemyHealth boss)
    {
        if (boss == null)
            return null;

        if (boss.GetComponent<RevenantBossRange>() != null)
            return revenantBossIcon;

        if (boss.GetComponent<EnemyBoss_SS>() != null)
            return slimeBossIcon;

        if (boss.GetComponent<EnemyTitan>() != null || boss.GetComponent<EnemyGolem>() != null)
            return rockBossIcon;

        return null;
    }
}
