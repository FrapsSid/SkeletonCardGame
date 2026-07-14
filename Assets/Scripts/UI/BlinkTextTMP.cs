using UnityEngine;
using TMPro;

public class BlinkTextTMP : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text text;

    [Header("Settings")]
    [SerializeField] private float blinkDuration = 3f;
    [SerializeField] private float blinkInterval = 0.3f;
    [SerializeField] private Color blinkColor = Color.yellow;

    private float _timer;
    private float _blinkTimer;
    private bool _isBlinkColor;
    private Color _originalColor;

    void Start()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();

        if (text != null)
            _originalColor = text.color;

        _timer = 0f;
    }

    void Update()
    {
        if (text == null) return;

        _timer += Time.deltaTime;
        
        if (_timer >= blinkDuration)
        {
            text.color = _originalColor;
            enabled = false;
            return;
        }

        _blinkTimer += Time.deltaTime;
        
        if (_blinkTimer >= blinkInterval)
        {
            _isBlinkColor = !_isBlinkColor;
            text.color = _isBlinkColor ? blinkColor : _originalColor;
            _blinkTimer = 0f;
        }
    }
}