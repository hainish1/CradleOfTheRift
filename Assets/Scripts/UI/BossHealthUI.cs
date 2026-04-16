using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class BossHealthUI : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset bossBarTemplate;
    [SerializeField] private float damageHoldDuration = 0.5f;
    [SerializeField] private float damageDrainDuration = 0.4f;

    private EnemyHealth boss;
    private VisualElement bossContainer;
    private ProgressBar healthBar;
    private ProgressBar damageBar;
    private readonly List<BossSpawner> subscribedSpawners = new();
    private Coroutine drainCoroutine;

    private void Awake()
    {
        var document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        bossContainer = root.Q<VisualElement>("BossHealthContainer");
        healthBar = root.Q<ProgressBar>("BossHealthBar");
        damageBar = root.Q<ProgressBar>("BossDamageBar");

        if (bossContainer == null || healthBar == null || damageBar == null)
        {
            if (bossBarTemplate == null)
            {
                Debug.LogError("[BossHealthUI] Boss bar template is missing and no boss UI elements were found in the HUD document.");
                return;
            }

            var instance = bossBarTemplate.Instantiate();
            root.Add(instance);

            bossContainer = root.Q<VisualElement>("BossHealthContainer");
            healthBar = root.Q<ProgressBar>("BossHealthBar");
            damageBar = root.Q<ProgressBar>("BossDamageBar");
        }

        if (bossContainer == null || healthBar == null || damageBar == null)
        {
            Debug.LogError("[BossHealthUI] Failed to locate boss health UI elements after injecting the template.");
            return;
        }

        Hide();
    }

    private void OnEnable()
    {
        SubscribeToSpawners();
    }

    private void Start()
    {
        SubscribeToSpawners();
    }

    private void OnDisable()
    {
        UnsubscribeFromSpawners();
        Unbind();
    }

    public void BindToBoss(EnemyHealth bossHealth)
    {
        Unbind();

        boss = bossHealth;
        if (boss == null || healthBar == null || damageBar == null || bossContainer == null)
            return;

        boss.OnHealthChanged += OnHealthChanged;
        boss.EnemyDied += OnBossDied;

        healthBar.lowValue = 0f;
        damageBar.lowValue = 0f;
        healthBar.highValue = boss.GetMaxHealth();
        damageBar.highValue = boss.GetMaxHealth();

        OnHealthChanged(boss.GetCurrentHealth(), boss.GetMaxHealth());
        Show();
    }

    private void Unbind()
    {
        if (boss != null)
        {
            boss.OnHealthChanged -= OnHealthChanged;
            boss.EnemyDied -= OnBossDied;
            boss = null;
        }

        if (drainCoroutine != null)
        {
            StopCoroutine(drainCoroutine);
            drainCoroutine = null;
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (healthBar == null || damageBar == null)
            return;

        float previous = healthBar.value;

        healthBar.highValue = max;
        damageBar.highValue = max;
        healthBar.value = current;
        healthBar.title = $"BOSS  {(int)current} / {(int)max}";

        if (current < previous)
        {
            damageBar.value = previous;

            if (drainCoroutine != null)
                StopCoroutine(drainCoroutine);

            drainCoroutine = StartCoroutine(DrainDamageBar(previous, current));
        }
        else
        {
            damageBar.value = current;
        }
    }

    private IEnumerator DrainDamageBar(float from, float to)
    {
        yield return new WaitForSeconds(damageHoldDuration);

        float elapsed = 0f;
        while (elapsed < damageDrainDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / damageDrainDuration);
            t = 1f - (1f - t) * (1f - t);

            damageBar.value = Mathf.Lerp(from, to, t);
            yield return null;
        }

        damageBar.value = to;
        drainCoroutine = null;
    }

    private void OnBossSpawned(EnemyHealth bossHealth)
    {
        BindToBoss(bossHealth);
    }

    private void OnBossDied(EnemyHealth deadBoss)
    {
        Hide();
        Unbind();
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
}
