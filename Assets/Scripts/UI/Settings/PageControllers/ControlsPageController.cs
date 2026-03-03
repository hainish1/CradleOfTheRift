using UnityEngine.UIElements;

public class ControlsPageController
{
    private readonly SettingsService service;

    public ControlsPageController(SettingsService service)
    {
        this.service = service;
    }

    public void Initialize(VisualElement pageRoot)
    {
        // Query and wire controls elements here when the page is built
    }

    public void Refresh(SettingsData data)
    {
        // Sync UI to data here when the page is built
    }
}