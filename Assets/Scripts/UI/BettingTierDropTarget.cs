using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Betting Tier Drop Target")]
[DisallowMultipleComponent]
public class BettingTierDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField] private BettingCanvasUI bettingUI = null;
    [SerializeField] private DeclaredCombinationTier tier = DeclaredCombinationTier.Easy;

    private Button button = null;

    public DeclaredCombinationTier Tier => tier;

    private void Awake()
    {
        ResolveButton();
    }

    private void OnEnable()
    {
        RegisterClick();
    }

    private void OnDisable()
    {
        UnregisterClick();
    }

    public void Configure(BettingCanvasUI ui, DeclaredCombinationTier declaredTier)
    {
        bettingUI = ui;
        tier = declaredTier;
        ResolveButton();
        RegisterClick();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (bettingUI == null || eventData == null || eventData.pointerDrag == null)
            return;

        BettingBodyPartButton partButton = eventData.pointerDrag.GetComponentInParent<BettingBodyPartButton>();
        if (partButton != null)
            bettingUI.DropBodyPartOnTier(partButton.BodyPartType, tier);
    }

    private void HandleClicked()
    {
        if (bettingUI != null)
            bettingUI.SelectTier(tier);
    }

    private void ResolveButton()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void RegisterClick()
    {
        ResolveButton();
        if (button == null)
            return;

        button.onClick.RemoveListener(HandleClicked);
        button.onClick.AddListener(HandleClicked);
    }

    private void UnregisterClick()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }
}
