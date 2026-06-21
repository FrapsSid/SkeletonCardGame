using UnityEngine;

public abstract class GameUIScreen : MonoBehaviour
{
    protected GameUIManager UI { get; private set; }

    public abstract ScreenId Id { get; }
    public virtual bool IsModal => false;

    public void Initialize(GameUIManager manager)
    {
        UI = manager;
        Build();
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        OnShow();
    }

    public void Hide()
    {
        OnHide();
        gameObject.SetActive(false);
    }

    protected abstract void Build();
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}
