using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsSettings : MonoBehaviour {
    private const float MinimumBrightness = 0.2f;

    [Header("Audio Mixer")] [SerializeField]
    private AudioMixer audioMixer;

    [Header("Sliders")] [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider brightnessSlider;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string BrightnessKey = "Brightness";

    private void Start() {
        float musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.35f));
        float sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.7f));
        float brightness = Mathf.Clamp(PlayerPrefs.GetFloat(BrightnessKey, 1f), MinimumBrightness, 1f);

        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;

        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;

        brightnessSlider.minValue = MinimumBrightness;
        brightnessSlider.maxValue = 1f;

        musicSlider.SetValueWithoutNotify(musicVolume);
        sfxSlider.SetValueWithoutNotify(sfxVolume);
        brightnessSlider.SetValueWithoutNotify(brightness);

        ApplyMusicVolume(musicVolume);
        ApplySfxVolume(sfxVolume);
        ApplyBrightness(brightness);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        brightnessSlider.onValueChanged.AddListener(SetBrightness);
    }

    private void OnDestroy() {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(SetBrightness);
    }

    public void SetMusicVolume(float value) {
        value = Mathf.Clamp01(value);

        ApplyMusicVolume(value);

        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value) {
        value = Mathf.Clamp01(value);

        ApplySfxVolume(value);

        PlayerPrefs.SetFloat(SfxVolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetBrightness(float value) {
        value = Mathf.Clamp(value, MinimumBrightness, 1f);

        ApplyBrightness(value);

        PlayerPrefs.SetFloat(BrightnessKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume(float value) {
        if (audioMixer == null)
            return;

        audioMixer.SetFloat("MusicVolume", LinearToDecibel(value));
    }

    private void ApplySfxVolume(float value) {
        if (audioMixer == null)
            return;

        audioMixer.SetFloat("SFXVolume", LinearToDecibel(value));
    }

    private void ApplyBrightness(float value) {
        value = Mathf.Clamp(value, MinimumBrightness, 1f);

        if (BrightnessHandler.Instance == null) {
            Debug.LogWarning("BrightnessManager is not found in the scene.");
            return;
        }

        BrightnessHandler.Instance.SetBrightness(value);
    }

    private float LinearToDecibel(float value) {
        if (value <= 0.0001f)
            return -80f;

        return Mathf.Log10(value) * 20f;
    }
}