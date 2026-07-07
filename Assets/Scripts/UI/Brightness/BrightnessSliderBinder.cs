using UnityEngine;
using UnityEngine.UI;

public class BrightnessSliderBinder : MonoBehaviour {
    [SerializeField] private Slider brightnessSlider;

    private void Start() {
        if (brightnessSlider == null)
            brightnessSlider = GetComponent<Slider>();

        if (brightnessSlider == null)
            return;

        if (BrightnessHandler.Instance != null) {
            brightnessSlider.SetValueWithoutNotify(BrightnessHandler.Instance.CurrentBrightness);
            brightnessSlider.onValueChanged.AddListener(BrightnessHandler.Instance.SetBrightness);
        }
    }

    private void OnDestroy() {
        if (brightnessSlider != null && BrightnessHandler.Instance != null) {
            brightnessSlider.onValueChanged.RemoveListener(BrightnessHandler.Instance.SetBrightness);
        }
    }
}
