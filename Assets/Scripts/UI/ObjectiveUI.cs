using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectiveUI : MonoBehaviour
{
    private const string CLASS_ACTIVE   = "objective-active";
    private const string CLASS_COMPLETE = "objective-complete";
    private const string CLASS_INACTIVE = "objective-inactive";

    private VisualElement objectivesPanel;
    private VisualElement locateRow;
    private VisualElement chargeRow;
    private VisualElement killRow;
    private Label locateLabel;
    private Label chargeLabel;
    private Label killLabel;
    private ExtractionZone activeZone;


    private void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        objectivesPanel = root.Q<VisualElement>("ObjectivesPanel");
        locateRow = root.Q<VisualElement>("LocateRow");
        chargeRow = root.Q<VisualElement>("ChargeRow");
        killRow = root.Q<VisualElement>("KillRow");
        locateLabel = root.Q<Label>("LocateLabel");
        chargeLabel = root.Q<Label>("ChargeLabel");
        killLabel = root.Q<Label>("KillLabel");

        if (ExtractionManager.Instance != null)
        {
            ExtractionManager.Instance.ExtractionStarted += OnExtractionStarted;
            ExtractionManager.Instance.AllExtractionsFinished += OnAllZonesFinished;
        }

        ShowLocatePhase();
    }

    private void OnDestroy()
    {
        if (ExtractionManager.Instance != null)
        {
            ExtractionManager.Instance.ExtractionStarted -= OnExtractionStarted;
            ExtractionManager.Instance.AllExtractionsFinished -= OnAllZonesFinished;
        }

        UnsubscribeFromZone();
    }

    private void ShowLocatePhase()
    {
        SetRowState(locateRow, locateLabel, "Locate Extraction Site", RowState.Active);

        chargeRow.style.display = DisplayStyle.None;
        killRow.style.display = DisplayStyle.None;
        locateRow.style.display = DisplayStyle.Flex;
    }

    private void OnExtractionStarted(ExtractionZone zone)
    {
        activeZone = zone;
        SubscribeToZone(zone);

        // Cross off Locate
        SetRowState(locateRow, locateLabel, "Locate Extraction Site", RowState.Complete);

        // Reveal active objectives
        chargeRow.style.display = DisplayStyle.Flex;
        killRow.style.display   = DisplayStyle.Flex;
        SetRowState(chargeRow, chargeLabel, "Charge the Extraction Site", RowState.Active);
        SetRowState(killRow, killLabel, "Eliminate the Boss", RowState.Active);
    }

    private void OnAllZonesFinished()
    {
        StartCoroutine(CompleteAndReset());
    }

    private IEnumerator CompleteAndReset()
    {
        // Briefly show both ticked before resetting for the next zone
        SetRowState(chargeRow, chargeLabel, "Charge the Extraction Site", RowState.Complete);
        SetRowState(killRow, killLabel, "Eliminate the Boss", RowState.Complete);

        yield return new WaitForSeconds(1.5f);

        UnsubscribeFromZone();
        activeZone = null;
        ShowLocatePhase();
    }

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

    private void OnZoneChargeFinished()
    {
        SetRowState(chargeRow, chargeLabel, "Charge the Extraction Site", RowState.Complete);
    }

    private void OnBossDied()
    {
        SetRowState(killRow, killLabel, "Eliminate the Boss", RowState.Complete);
    }

    private enum RowState { Active, Complete, Inactive }

    private void SetRowState(VisualElement row, Label label, string text, RowState state)
    {
        row.RemoveFromClassList(CLASS_ACTIVE);
        row.RemoveFromClassList(CLASS_COMPLETE);
        row.RemoveFromClassList(CLASS_INACTIVE);

        label.text = state == RowState.Complete ? $"✓  {text}" : $"•  {text}";

        switch (state)
        {
            case RowState.Active: row.AddToClassList(CLASS_ACTIVE); break;
            case RowState.Complete: row.AddToClassList(CLASS_COMPLETE); break;
            case RowState.Inactive: row.AddToClassList(CLASS_INACTIVE); break;
        }
    }
}
