using UnityEngine;

public class BrightnessBootstrap : MonoBehaviour {
    private void Awake() {
        if (BrightnessHandler.Instance == null) {
            GameObject prefab = Resources.Load<GameObject>("BrightnessHandler");

            if (prefab != null)
                Instantiate(prefab);
        }
    }
}
