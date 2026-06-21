using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class GameStyledButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private bool textButton;

    private Button button;
    private Image image;
    private bool hovered;
    private bool pressed;

    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        Apply();
    }

    public void SetLabel(TMP_Text target)
    {
        label = target;
        Apply();
    }

    public void SetTextButtonMode(bool enabled)
    {
        textButton = enabled;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Apply();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
        Apply();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
        Apply();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        Apply();
    }

    private void Apply()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (image == null)
            image = GetComponent<Image>();

        bool enabled = button == null || button.interactable;
        Color background = textButton ? new Color(0f, 0f, 0f, 0f) : GameUITheme.ButtonBase;
        Color text = GameUITheme.White;

        if (!enabled)
        {
            background = textButton ? new Color(0f, 0f, 0f, 0f) : GameUITheme.ButtonDisabled;
            text = new Color(1f, 1f, 1f, 0.35f);
        }
        else if (pressed)
        {
            background = textButton ? new Color(0f, 1f, 0.68f, 0.08f) : GameUITheme.ButtonPressed;
            text = textButton ? GameUITheme.Cyan : GameUITheme.White;
        }
        else if (hovered)
        {
            background = textButton ? new Color(0f, 1f, 0.68f, 0.04f) : GameUITheme.ButtonHover;
            text = GameUITheme.Cyan;
        }

        if (image != null)
            image.color = background;
        if (label != null)
        {
            label.color = text;
            label.enableVertexGradient = enabled && hovered;
            if (label.enableVertexGradient)
            {
                label.colorGradient = new VertexGradient(
                    GameUITheme.White,
                    GameUITheme.Cyan,
                    GameUITheme.White,
                    GameUITheme.Cyan);
            }
        }
    }
}
