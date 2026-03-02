using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectiveUI : MonoBehaviour
{
    // ── USS class names ───────────────────────────────────────────────────────
    private const string CLASS_ACTIVE   = "objective-active";
    private const string CLASS_COMPLETE = "objective-complete";
    private const string CLASS_FLASH    = "objective-flash";

    // ── Timing constants ──────────────────────────────────────────────────────
    private const int   FADE_MS        = 600;
    private const int   TICK_MS        = 16;
    private const float SLIDE_PX       = 14f;
    private const int   FLASH_MS       = 900;
    private const int   ROW_STAGGER_MS = 300;
    private const float HOLD_SECONDS   = 2.0f;
    private const int   PANEL_FADE_MS  = 600;

    // ── UI references ─────────────────────────────────────────────────────────
    private VisualElement objectivesPanel;
    private VisualElement locateRow;
    private VisualElement chargeRow;
    private VisualElement killRow;
    private Label         locateLabel;
    private Label         chargeLabel;
    private Label         killLabel;

    // ── State ─────────────────────────────────────────────────────────────────
    private ExtractionZone activeZone;
    private bool           isResetting;
    private bool           chargeRowComplete;
    private bool           killRowComplete;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        objectivesPanel = root.Q<VisualElement>("ObjectivesPanel");
        locateRow       = root.Q<VisualElement>("LocateRow");
        chargeRow       = root.Q<VisualElement>("ChargeRow");
        killRow         = root.Q<VisualElement>("KillRow");
        locateLabel     = root.Q<Label>("LocateLabel");
        chargeLabel     = root.Q<Label>("ChargeLabel");
        killLabel       = root.Q<Label>("KillLabel");

        if (ExtractionManager.Instance != null)
        {
            ExtractionManager.Instance.ExtractionStarted      += OnExtractionStarted;
            ExtractionManager.Instance.AllExtractionsFinished += OnAllZonesFinished;
            ExtractionManager.Instance.OnGameWon              += OnGameWon;
        }

        objectivesPanel.style.opacity = 0f;
        InitLocatePhase();
        FadePanel(0f, 1f, PANEL_FADE_MS * 2);
    }

    private void OnDestroy()
    {
        if (ExtractionManager.Instance != null)
        {
            ExtractionManager.Instance.ExtractionStarted      -= OnExtractionStarted;
            ExtractionManager.Instance.AllExtractionsFinished -= OnAllZonesFinished;
            ExtractionManager.Instance.OnGameWon              -= OnGameWon;
        }
        UnsubscribeFromZone();
    }

    // ── Phase transitions ─────────────────────────────────────────────────────

    private void InitLocatePhase()
    {
        chargeRowComplete = false;
        killRowComplete   = false;

        SetRowState(locateRow, locateLabel, "Locate Extraction Site", RowState.Active);

        locateRow.style.opacity   = 1f;
        locateRow.style.display   = DisplayStyle.Flex;
        locateRow.style.translate = new Translate(0f, 0f);

        chargeRow.style.display = DisplayStyle.None;
        chargeRow.style.opacity = 0f;

        killRow.style.display   = DisplayStyle.None;
        killRow.style.opacity   = 0f;
    }

    private void OnExtractionStarted(ExtractionZone zone)
    {
        if (isResetting) return;

        activeZone = zone;
        SubscribeToZone(zone);

        TickOffRow(locateRow, locateLabel, "Locate Extraction Site", delayMs: 0, onComplete: () =>
        {
            RevealRow(chargeRow, chargeLabel, "Charge the Extraction Site", delayMs: 0, onComplete: () =>
            {
                RevealRow(killRow, killLabel, "Eliminate the Boss", delayMs: ROW_STAGGER_MS);
            });
        });
    }

    private void OnAllZonesFinished()
    {
        if (isResetting) return;
        StartCoroutine(CompleteAndReset());
    }

    private IEnumerator CompleteAndReset()
    {
        isResetting = true;

        if (!chargeRowComplete)
            TickOffRow(chargeRow, chargeLabel, "Charge the Extraction Site");

        yield return new WaitForSeconds(ROW_STAGGER_MS / 1000f);

        if (!killRowComplete)
            TickOffRow(killRow, killLabel, "Eliminate the Boss");

        yield return new WaitForSeconds(HOLD_SECONDS);

        bool done = false;
        FadePanel(1f, 0f, PANEL_FADE_MS, () => done = true);
        yield return new WaitUntil(() => done);

        UnsubscribeFromZone();
        activeZone = null;
        InitLocatePhase();

        yield return new WaitForSeconds(0.1f);

        FadePanel(0f, 1f, PANEL_FADE_MS, () => FlashRow(locateRow));

        isResetting = false;
    }

    private void OnGameWon()
    {
        StopAllCoroutines();
        FadePanel(objectivesPanel.style.opacity.value, 0f, PANEL_FADE_MS);
    }

    // ── Individual objective events ───────────────────────────────────────────

    private void OnZoneChargeFinished()
    {
        chargeRowComplete = true;
        TickOffRow(chargeRow, chargeLabel, "Charge the Extraction Site");
    }

    private void OnBossDied()
    {
        killRowComplete = true;
        TickOffRow(killRow, killLabel, "Eliminate the Boss");
    }

    // ── Row animations ────────────────────────────────────────────────────────

    private void RevealRow(VisualElement row, Label label, string text,
                           int delayMs = 0, System.Action onComplete = null)
    {
        SetRowState(row, label, text, RowState.Active);
        row.style.display   = DisplayStyle.Flex;
        row.style.opacity   = 0f;
        row.style.translate = new Translate(SLIDE_PX, 0f);

        row.schedule.Execute(() =>
        {
            TweenFloat(row, 0f, 1f,       FADE_MS, t => row.style.opacity   = t);
            TweenFloat(row, SLIDE_PX, 0f, FADE_MS, t => row.style.translate = new Translate(t, 0f));

            row.schedule.Execute(() =>
            {
                FlashRow(row);
                onComplete?.Invoke();
            }).StartingIn(FADE_MS);

        }).StartingIn(delayMs);
    }

    private void TickOffRow(VisualElement row, Label label, string text,
                            int delayMs = 0, System.Action onComplete = null)
    {
        row.schedule.Execute(() =>
        {
            SetRowState(row, label, text, RowState.Complete);
            FlashRow(row);
            onComplete?.Invoke();
        }).StartingIn(delayMs);
    }

    /// <summary>Briefly tints the row gold then lets USS transition it back.</summary>
    private void FlashRow(VisualElement row)
    {
        row.AddToClassList(CLASS_FLASH);
        row.schedule.Execute(() =>
            row.RemoveFromClassList(CLASS_FLASH)
        ).StartingIn(FLASH_MS);
    }

    // ── Panel fade ────────────────────────────────────────────────────────────

    private void FadePanel(float from, float to, int durationMs,
                           System.Action onComplete = null)
    {
        TweenFloat(objectivesPanel, from, to, durationMs,
            t => objectivesPanel.style.opacity = t,
            onComplete);
    }

    // ── Generic UIElements float tweener ─────────────────────────────────────

    private void TweenFloat(VisualElement context, float from, float to, int durationMs,
                            System.Action<float> onTick, System.Action onComplete = null)
    {
        float elapsed  = 0f;
        float duration = Mathf.Max(durationMs, 1) / 1000f;

        IVisualElementScheduledItem handle = null;
        handle = context.schedule.Execute(() =>
        {
            elapsed += TICK_MS / 1000f;
            float t  = Mathf.Clamp01(elapsed / duration);
            onTick(Mathf.Lerp(from, to, EaseOutCubic(t)));

            if (t >= 1f)
            {
                handle?.Pause();
                onComplete?.Invoke();
            }
        }).Every(TICK_MS);
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    // ── Zone subscriptions ────────────────────────────────────────────────────

    private void SubscribeToZone(ExtractionZone zone)
    {
        zone.ExtractionFinished += OnZoneChargeFinished;

        BossSpawner spawner = zone.GetComponent<BossSpawner>();
        if (spawner != null)
            spawner.BossDied += OnBossDied;
    }

    private void UnsubscribeFromZone()
    {
        if (activeZone == null) return;

        activeZone.ExtractionFinished -= OnZoneChargeFinished;

        BossSpawner spawner = activeZone.GetComponent<BossSpawner>();
        if (spawner != null)
            spawner.BossDied -= OnBossDied;
    }

    // ── Row state helper ──────────────────────────────────────────────────────

    private enum RowState { Active, Complete }

    private void SetRowState(VisualElement row, Label label, string text, RowState state)
    {
        row.RemoveFromClassList(CLASS_ACTIVE);
        row.RemoveFromClassList(CLASS_COMPLETE);
        row.RemoveFromClassList(CLASS_FLASH);

        label.text = state == RowState.Complete ? $"✓  {text}" : $"•  {text}";

        switch (state)
        {
            case RowState.Active:   row.AddToClassList(CLASS_ACTIVE);   break;
            case RowState.Complete: row.AddToClassList(CLASS_COMPLETE); break;
        }
    }
}
