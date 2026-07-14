using System;
using System.Collections;
using UnityEngine;

public class FlyingLimbEffect : MonoBehaviour
{
    private Transform _target;
    private float _duration;
    private float _elapsed;
    private Vector3 _startPos;
    private Vector3 _startScale;
    private Action _onComplete;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private bool _wasEnabled;

    public static FlyingLimbEffect Create(GameObject obj, Transform target, float duration = 0.8f, Action onComplete = null)
    {
        var effect = obj.AddComponent<FlyingLimbEffect>();
        effect._target = target;
        effect._duration = duration;
        effect._onComplete = onComplete;
        effect._startScale = obj.transform.localScale;
        effect._renderers = obj.GetComponentsInChildren<Renderer>();
        effect._mpb = new MaterialPropertyBlock();
        return effect;
    }

    private void Start()
    {
        _startPos = transform.position;
        _elapsed = 0f;

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
            r.SetPropertyBlock(_mpb);
        }
    }

    private void Update()
    {
        if (_target == null)
        {
            Finish();
            return;
        }

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);
        float eased = EaseInOutQuad(t);

        transform.position = Vector3.Lerp(_startPos, _target.position, eased);

        float arc = Mathf.Sin(t * Mathf.PI) * 2f;
        transform.position += Vector3.up * arc;

        float scaleT = t < 0.7f ? 1f : Mathf.Lerp(1f, 0.3f, (t - 0.7f) / 0.3f);
        transform.localScale = _startScale * scaleT;

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            float alpha = t < 0.8f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.8f) / 0.2f);
            _mpb.SetColor("_BaseColor", new Color(1f, 0.8f, 0.6f, alpha));
            r.SetPropertyBlock(_mpb);
        }

        if (t >= 1f)
            Finish();
    }

    private void Finish()
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", Color.white);
            r.SetPropertyBlock(_mpb);
        }

        _onComplete?.Invoke();
        Destroy(this);
    }

    private static float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
}
