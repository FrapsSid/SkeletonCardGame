using UnityEngine;
using UnityEngine.EventSystems;

public sealed class GameTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea] public string title;
    [TextArea] public string body;

    public void Set(string newTitle, string newBody)
    {
        title = newTitle;
        body = newBody;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GameTooltip.Instance?.Show(title, body, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GameTooltip.Instance?.Hide();
    }
}
