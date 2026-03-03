using UnityEngine.UIElements;

public class AudioPageController
{
    private readonly SettingsService service;

    private Slider sliderMaster, sliderMusic, sliderSFX, sliderAmbient;
    private Label  labelMaster,  labelMusic,  labelSFX,  labelAmbient;

    public AudioPageController(SettingsService service)
    {
        this.service = service;
    }

    public void Initialize(VisualElement pageRoot)
    {
        sliderMaster  = pageRoot.Q<Slider>("SliderMasterVolume");
        sliderMusic   = pageRoot.Q<Slider>("SliderMusicVolume");
        sliderSFX     = pageRoot.Q<Slider>("SliderSFXVolume");
        sliderAmbient = pageRoot.Q<Slider>("SliderAmbientVolume");

        labelMaster  = pageRoot.Q<Label>("LabelMasterVolume");
        labelMusic   = pageRoot.Q<Label>("LabelMusicVolume");
        labelSFX     = pageRoot.Q<Label>("LabelSFXVolume");
        labelAmbient = pageRoot.Q<Label>("LabelAmbientVolume");

        RegisterSlider(sliderMaster, labelMaster, v =>
        {
            service.Current.masterVolume = v;
            service.Apply(service.Current);
        });
        RegisterSlider(sliderMusic, labelMusic, v =>
        {
            service.Current.musicVolume = v;
            service.Apply(service.Current);
        });
        RegisterSlider(sliderSFX, labelSFX, v =>
        {
            service.Current.sfxVolume = v;
            service.Apply(service.Current);
        });
        RegisterSlider(sliderAmbient, labelAmbient, v =>
        {
            service.Current.ambientVolume = v;
            service.Apply(service.Current);
        });
    }

    // Called by SettingsMenuController after Load() or RevertToDefaults()
    public void Refresh(SettingsData data)
    {
        SetSilently(sliderMaster,  labelMaster,  data.masterVolume);
        SetSilently(sliderMusic,   labelMusic,   data.musicVolume);
        SetSilently(sliderSFX,     labelSFX,     data.sfxVolume);
        SetSilently(sliderAmbient, labelAmbient, data.ambientVolume);
    }

    private void RegisterSlider(Slider slider, Label label, System.Action<int> onChanged)
    {
        if (slider == null) return;
        slider.RegisterValueChangedCallback(evt =>
        {
            int v = (int)evt.newValue;
            slider.SetValueWithoutNotify(v);
            if (label != null) label.text = v.ToString();
            onChanged(v);
        });
    }

    private void SetSilently(Slider slider, Label label, int value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(value);
        if (label != null) label.text = value.ToString();
    }
}