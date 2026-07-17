using UnityEngine;

public class UncollectableGlow : MonoBehaviour
{
    private Renderer[]? _renderers;
    private MaterialPropertyBlock? _mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly Color GlowColor = new(0.8f, 0.15f, 0.15f, 1f);

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        ApplyGlow(true);
    }

    private void OnDestroy()
    {
        ApplyGlow(false);
    }

    public void ApplyGlow(bool active)
    {
        if (_renderers == null)
            _renderers = GetComponentsInChildren<Renderer>();
        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            if (active)
            {
                _mpb.SetColor(BaseColorId, GlowColor);
                _mpb.SetColor(ColorId, GlowColor);
            }
            else
            {
                _mpb.Clear();
            }
            r.SetPropertyBlock(_mpb);
        }
    }
}
