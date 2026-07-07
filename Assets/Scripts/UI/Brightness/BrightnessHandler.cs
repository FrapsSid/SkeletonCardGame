using UnityEngine;
using UnityEngine.UI;

public class BrightnessHandler : MonoBehaviour {
    public static BrightnessHandler Instance { get; private set; }

    private const float MinimumBrightness = 0.2f;
    private const string BrightnessKey = "Brightness";

    [Header("Brightness")] [SerializeField]
    private Image brightnessOverlay;

    [Range(0f, 1f)] [SerializeField] private float maxDarknessAlpha = 0.6f;

    public float CurrentBrightness { get; private set; } = 1f;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (brightnessOverlay != null)
            brightnessOverlay.raycastTarget = false;

        float brightness = Mathf.Clamp(PlayerPrefs.GetFloat(BrightnessKey, 1f), MinimumBrightness, 1f);
        SetBrightness(brightness, false);
    }

    public void SetBrightness(float value) {
        SetBrightness(value, true);
    }

    public void SetBrightness(float value, bool save) {
        value = Mathf.Clamp(value, MinimumBrightness, 1f);

        CurrentBrightness = value;

        if (brightnessOverlay != null) {
            float darkness = 1f - value;

            Color color = brightnessOverlay.color;
            color.r = 0f;
            color.g = 0f;
            color.b = 0f;
            color.a = darkness * maxDarknessAlpha;

            brightnessOverlay.color = color;
            brightnessOverlay.raycastTarget = false;
        }

        if (save) {
            PlayerPrefs.SetFloat(BrightnessKey, value);
            PlayerPrefs.Save();
        }
    }
}