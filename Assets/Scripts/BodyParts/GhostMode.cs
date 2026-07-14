#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GhostMode : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField] private float ghostAlpha = 0.35f;
    [SerializeField] private Color ghostTint = new(0.4f, 0.6f, 1f, 1f);

    private SkeletonBody? _body;
    private Skeleton? _skeleton;
    private readonly List<Renderer> _renderers = new();
    private MaterialPropertyBlock? _propBlock;
    private bool _isGhost;
    private CameraController? _cameraController;

    public bool IsGhost => _isGhost;
    public event Action<bool>? OnGhostModeChanged;

    private void Awake()
    {
        _body = GetComponent<SkeletonBody>();
        _propBlock = new MaterialPropertyBlock();
        _cameraController = FindFirstObjectByType<CameraController>();
    }

    private void OnEnable()
    {
        if (_body != null)
            _body.OnBodyChanged += HandleBodyChanged;
    }

    private void OnDisable()
    {
        if (_body != null)
            _body.OnBodyChanged -= HandleBodyChanged;
    }

    private void HandleBodyChanged()
    {
        if (_body == null) return;

        bool hasTorso = _body.HasPart(BodyPartType.Torso);

        if (!hasTorso && !_isGhost)
            EnterGhostMode();
        else if (hasTorso && _isGhost)
            ExitGhostMode();
    }

    public void EnterGhostMode()
    {
        if (_isGhost) return;
        _isGhost = true;

        CacheRenderers();
        SetRenderersAlpha(ghostAlpha, ghostTint);
        RefreshMeshVisibility();

        ApplyGlowToDetachedParts(true);

        if (_cameraController != null)
            _cameraController.SetForceThirdPerson(true);

        OnGhostModeChanged?.Invoke(true);
    }

    public void ExitGhostMode()
    {
        if (!_isGhost) return;
        _isGhost = false;

        ClearRenderersAlpha();
        RefreshMeshVisibility();

        ApplyGlowToDetachedParts(false);

        if (_cameraController != null)
            _cameraController.SetForceThirdPerson(false);

        OnGhostModeChanged?.Invoke(false);
    }

    public void RegenerateLimb(BodyPartType type)
    {
        if (_body == null) return;

        if (_body.HasPart(type))
            return;

        PlayerInventoryOwner? inventoryOwner = GetComponent<PlayerInventoryOwner>();
        if (inventoryOwner == null) return;

        Skeleton? skeleton = inventoryOwner.OwnerSkeleton;
        if (skeleton == null) return;

        Team? team = skeleton.team;
        if (team == null) return;

        StakeAsset? asset = FindTeamAsset(team, type, skeleton);
        if (asset == null || asset.bodyPart == null) return;

        BodyPart part = asset.bodyPart;
        if (part.State == BodyPartState.Detached)
        {
            _body.AttachPart(part);
        }
    }

    public void RegenerateAllAvailableLimbs()
    {
        if (_body == null || _skeleton == null) return;

        Team? team = _skeleton.team;
        if (team == null) return;

        foreach (BodyPartType type in Enum.GetValues(typeof(BodyPartType)))
        {
            if (type == BodyPartType.Torso || type == BodyPartType.Soul)
                continue;

            if (!_body.HasPart(type))
                RegenerateLimb(type);
        }
    }

    private static StakeAsset? FindTeamAsset(Team team, BodyPartType type, Skeleton excludeOwner)
    {
        foreach (StakeAsset asset in team.Assets)
        {
            if (asset == null || asset.bodyPart == null)
                continue;

            if (asset.bodyPart.Item.Type != type)
                continue;

            if (asset.bodyPart.State != BodyPartState.Detached)
                continue;

            if (asset.sourceOwner == excludeOwner)
                continue;

            return asset;
        }
        return null;
    }

    private void CacheRenderers()
    {
        _renderers.Clear();
        if (_body == null) return;

        foreach (BodyPart part in _body.GetAttachedParts())
        {
            foreach (Renderer r in part.GetComponentsInChildren<Renderer>())
            {
                if (r != null && !_renderers.Contains(r))
                    _renderers.Add(r);
            }
        }

        SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var smr in smrs)
        {
            if (smr != null && !_renderers.Contains(smr))
                _renderers.Add(smr);
        }

        MeshRenderer[] mrs = GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in mrs)
        {
            if (mr != null && !_renderers.Contains(mr))
                _renderers.Add(mr);
        }
    }

    private void SetRenderersAlpha(float alpha, Color tint)
    {
        Color color = tint;
        color.a = alpha;

        foreach (Renderer r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, color);
            _propBlock.SetColor(ColorId, color);
            r.SetPropertyBlock(_propBlock);
        }
    }

    private void ClearRenderersAlpha()
    {
        foreach (Renderer r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_propBlock);
            _propBlock.Clear();
            r.SetPropertyBlock(_propBlock);
        }
    }

    private void RefreshMeshVisibility()
    {
        if (_body != null)
            _body.RefreshMeshVisibility();
    }

    private void ApplyGlowToDetachedParts(bool apply)
    {
        var pickupables = FindObjectsByType<Pickupable>(FindObjectsSortMode.None);
        foreach (var pickupable in pickupables)
        {
            if (pickupable.Item is not BodyPartItem bpi) continue;
            BodyPart? bp = bpi.CurrentBodyPart;
            if (bp == null) continue;

            GameObject? holder = bp.currentHolder;
            if (holder == null || holder != gameObject) continue;

            if (apply)
            {
                if (pickupable.GetComponent<UncollectableGlow>() == null)
                    pickupable.gameObject.AddComponent<UncollectableGlow>();
            }
            else
            {
                UncollectableGlow? glow = pickupable.GetComponent<UncollectableGlow>();
                if (glow != null)
                    Destroy(glow);
            }
        }
    }
}
