using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandSlotUI : MonoBehaviour {
    public Button selectButton;
    public Image iconImage;
    public TMP_Text countText;
    public GameObject selectedHighlight;
    public Color fallbackItemColor = new Color(0.82f, 0.86f, 0.92f, 1f);

    private PlayerHandUI _owner;
    private int _slotIndex;
    private Vector2 _defaultIconSize;
    private bool _hasDefaultIconSize;

    public void Initialize(PlayerHandUI owner, int slotIndex) {
        _owner = owner;
        _slotIndex = slotIndex;
        CacheDefaultIconSize();

        if (selectButton != null) {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _owner.SelectSlot(_slotIndex));
        }
    }

    public void SetSlot(InventorySlot slot, Sprite resolvedSprite, bool isSelected) {
        bool hasItem = slot != null && !slot.IsEmpty && slot.quantity > 0;

        if (iconImage != null) {
            iconImage.enabled = true;
            iconImage.sprite = hasItem ? resolvedSprite : null;
            iconImage.preserveAspect = true;
            iconImage.color = !hasItem ? new Color(1f, 1f, 1f, 0f) : resolvedSprite != null ? Color.white : fallbackItemColor;
            ApplySpriteSize(hasItem ? resolvedSprite : null);
        }

        if (countText != null) {
            bool showCount = hasItem && slot.quantity > 1 && slot.cardData == null;
            countText.text = showCount ? slot.quantity.ToString() : string.Empty;
        }

        if (selectedHighlight != null) {
            selectedHighlight.SetActive(isSelected);
        }
    }

    private void CacheDefaultIconSize() {
        if (_hasDefaultIconSize || iconImage == null) {
            return;
        }

        _defaultIconSize = iconImage.rectTransform.sizeDelta;
        if (_defaultIconSize.x <= 0f || _defaultIconSize.y <= 0f) {
            _defaultIconSize = new Vector2(180f, 180f);
        }

        _hasDefaultIconSize = true;
    }

    private void ApplySpriteSize(Sprite sprite) {
        if (iconImage == null) {
            return;
        }

        CacheDefaultIconSize();
        RectTransform rectTransform = iconImage.rectTransform;
        if (rectTransform == null || !_hasDefaultIconSize) {
            return;
        }

        if (sprite == null) {
            rectTransform.sizeDelta = _defaultIconSize;
            return;
        }

        float spriteWidth = Mathf.Max(1f, sprite.rect.width);
        float spriteHeight = Mathf.Max(1f, sprite.rect.height);
        float scale = Mathf.Min(_defaultIconSize.x / spriteWidth, _defaultIconSize.y / spriteHeight);
        rectTransform.sizeDelta = new Vector2(spriteWidth * scale, spriteHeight * scale);
    }
}


