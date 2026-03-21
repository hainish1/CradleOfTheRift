using UnityEngine.UIElements;

public class VideoPageController
{
    private readonly SettingsService service;

    public VideoPageController(SettingsService service)
    {
        this.service = service;
    }

    public void Initialize(VisualElement pageRoot)
    {
        // Query and wire video elements here when the page is built
    }

    public void Refresh(SettingsData data)
    {
        // Sync UI to data here when the page is built
    }
}