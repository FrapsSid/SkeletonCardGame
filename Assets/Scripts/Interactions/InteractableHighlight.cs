#nullable enable
using UnityEngine;

namespace Interactions
{
    public class InteractableHighlight : MonoBehaviour
    {
        [Header("Focus Visuals")] public bool tintRenderersOnFocus = true;
        public Color focusTint = new(1f, 0.9f, 0.35f, 1f);

        private Renderer[]? _renderers;
        private MaterialPropertyBlock? _propertyBlock;
        private bool _isFocused;
        public bool IsFocused => _isFocused;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            CacheRenderers();
            SetFocused(false);
        }

        public void SetFocused(bool focused)
        {
            if (_isFocused == focused)
                return;
            _isFocused = focused;
            ApplyFocusTint(_isFocused);
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void ApplyFocusTint(bool focused)
        {
            if (!tintRenderersOnFocus || _renderers == null)
            {
                return;
            }

            foreach (Renderer itemRenderer in _renderers)
            {
                if (_propertyBlock == null)
                    continue;
                itemRenderer.GetPropertyBlock(_propertyBlock);
                if (focused)
                {
                    _propertyBlock.SetColor(BaseColorId, focusTint);
                    _propertyBlock.SetColor(ColorId, focusTint);
                }
                else
                {
                    _propertyBlock.Clear();
                }

                itemRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}