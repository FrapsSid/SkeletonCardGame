#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkeletonBody : MonoBehaviour
{
    public event Action? OnBodyChanged;

    private Dictionary<BodyPartType, BodyPart> _attachedParts = new();
    private Dictionary<BodyPartType, List<Renderer>> _autoDiscoveredMeshRenderers = new();

    [Header("Bone References")]
    public Transform headBone;
    public Transform leftArmBone;
    public Transform rightArmBone;
    public Transform leftLegBone;
    public Transform rightLegBone;
    public Transform torsoBone;

    [Header("Visible Mesh Folders")]
    public Transform headMeshFolder;
    public Transform leftArmMeshFolder;
    public Transform rightArmMeshFolder;
    public Transform leftLegMeshFolder;
    public Transform rightLegMeshFolder;

    public Transform? GetMeshFolderForType(BodyPartType type)
    {
        Transform? folder = type switch
        {
            BodyPartType.Head => headMeshFolder,
            BodyPartType.LeftArm => leftArmMeshFolder,
            BodyPartType.RightArm => rightArmMeshFolder,
            BodyPartType.LeftLeg => leftLegMeshFolder,
            BodyPartType.RightLeg => rightLegMeshFolder,
            _ => null
        };

        // Don't use BodyPart objects as mesh folders - they conflict with BodyPart's own renderer management
        if (folder != null && (folder.GetComponent<BodyPart>() != null || folder.GetComponentInParent<BodyPart>() != null))
            return null;

        return folder;
    }

    public void RefreshMeshVisibility()
    {
        SetMeshFolderVisible(BodyPartType.Head, HasPart(BodyPartType.Head));
        SetMeshFolderVisible(BodyPartType.LeftArm, HasPart(BodyPartType.LeftArm));
        SetMeshFolderVisible(BodyPartType.RightArm, HasPart(BodyPartType.RightArm));
        SetMeshFolderVisible(BodyPartType.LeftLeg, HasPart(BodyPartType.LeftLeg));
        SetMeshFolderVisible(BodyPartType.RightLeg, HasPart(BodyPartType.RightLeg));
    }

    private void SetMeshFolderVisible(BodyPartType type, bool visible)
    {
        // Try explicit mesh folder first
        Transform? folder = GetMeshFolderForType(type);
        if (folder != null)
        {
            foreach (Renderer renderer in folder.GetComponentsInChildren<Renderer>())
            {
                if (renderer.GetComponentInParent<BodyPart>() != null)
                    continue;
                renderer.enabled = visible;
            }
            return;
        }

        // Fallback: auto-discovered renderers
        if (_autoDiscoveredMeshRenderers.TryGetValue(type, out var renderers))
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                if (renderer.GetComponentInParent<BodyPart>() != null)
                    continue;
                renderer.enabled = visible;
            }
        }
    }

    public bool IsIncapacitated => !HasSoul();
    public bool CanHoldCards => GetArmCount() > 0;

    private void Start()
    {
        // Auto-discover rigged mesh renderers if mesh folders not assigned
        if (AreAllMeshFoldersNull())
        {
            AutoDiscoverRiggedMeshRenderers();
        }

        BodyPart[] initialParts = GetComponentsInChildren<BodyPart>();
        foreach (var part in initialParts)
        {
            AttachPart(part);
        }
    }

    private bool AreAllMeshFoldersNull()
    {
        return headMeshFolder == null && leftArmMeshFolder == null && rightArmMeshFolder == null && leftLegMeshFolder == null && rightLegMeshFolder == null;
    }

    private void AutoDiscoverRiggedMeshRenderers()
    {
        // Find the rigged skeleton root (RealBodyParts or Armature)
        Transform? riggedRoot = FindRiggedSkeletonRoot();
        if (riggedRoot == null)
        {
            Debug.LogWarning($"[SkeletonBody] Could not find rigged skeleton root on {name}");
            return;
        }

        // Get all SkinnedMeshRenderers under the rigged skeleton
        var skinnedRenderers = riggedRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skinnedRenderers.Length == 0)
        {
            Debug.LogWarning($"[SkeletonBody] No SkinnedMeshRenderers found under rigged root {riggedRoot.name}");
            return;
        }

        // Group renderers by the bone they're primarily skinned to
        foreach (var smr in skinnedRenderers)
        {
            if (smr.rootBone == null) continue;
            
            BodyPartType? type = InferBodyPartTypeFromBone(smr.rootBone);
            if (type.HasValue)
            {
                if (!_autoDiscoveredMeshRenderers.ContainsKey(type.Value))
                    _autoDiscoveredMeshRenderers[type.Value] = new List<Renderer>();
                _autoDiscoveredMeshRenderers[type.Value].Add(smr);
            }
        }

        // Also check MeshRenderers (e.g., for head if it's not skinned)
        var meshRenderers = riggedRoot.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in meshRenderers)
        {
            // Check if this renderer's transform is near a bone
            BodyPartType? type = InferBodyPartTypeFromTransform(mr.transform);
            if (type.HasValue && !_autoDiscoveredMeshRenderers.ContainsKey(type.Value))
            {
                _autoDiscoveredMeshRenderers[type.Value] = new List<Renderer>();
            }
            if (type.HasValue)
            {
                _autoDiscoveredMeshRenderers[type.Value].Add(mr);
            }
        }

        Debug.Log($"[SkeletonBody] Auto-discovered mesh renderers: {string.Join(", ", _autoDiscoveredMeshRenderers.Select(kvp => $"{kvp.Key}: {kvp.Value.Count}"))}");
    }

    private Transform? FindRiggedSkeletonRoot()
    {
        // Look for RealBodyParts or Armature under this skeleton
        foreach (Transform child in transform)
        {
            if (child.name == "RealBodyParts" || child.name == "Armature" || child.name == "skelet_rig")
                return child;
        }
        // Fallback: first child with SkinnedMeshRenderer descendants
        foreach (Transform child in transform)
        {
            if (child.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0)
                return child;
        }
        return null;
    }

    private BodyPartType? InferBodyPartTypeFromBone(Transform bone)
    {
        string name = bone.name.ToLower();
        if (name.Contains("head") || name.Contains("skull") || name.Contains("neck")) return BodyPartType.Head;
        if (name.Contains("left") && (name.Contains("arm") || name.Contains("shoulder") || name.Contains("elbow") || name.Contains("hand") || name.Contains("wrist"))) return BodyPartType.LeftArm;
        if (name.Contains("right") && (name.Contains("arm") || name.Contains("shoulder") || name.Contains("elbow") || name.Contains("hand") || name.Contains("wrist"))) return BodyPartType.RightArm;
        if (name.Contains("left") && (name.Contains("leg") || name.Contains("hip") || name.Contains("knee") || name.Contains("foot") || name.Contains("ankle"))) return BodyPartType.LeftLeg;
        if (name.Contains("right") && (name.Contains("leg") || name.Contains("hip") || name.Contains("knee") || name.Contains("foot") || name.Contains("ankle"))) return BodyPartType.RightLeg;
        return null;
    }

    private BodyPartType? InferBodyPartTypeFromTransform(Transform t)
    {
        // Walk up to find a known bone
        Transform? current = t;
        while (current != null && current != transform)
        {
            var type = InferBodyPartTypeFromBone(current);
            if (type.HasValue) return type.Value;
            current = current.parent;
        }
        return null;
    }

    public List<BodyPart> GetAttachedParts() => _attachedParts.Values.ToList();
    
    public bool HasPart(BodyPartType type) => _attachedParts.ContainsKey(type);
    
    public int GetArmCount() => (HasPart(BodyPartType.LeftArm) ? 1 : 0) + (HasPart(BodyPartType.RightArm) ? 1 : 0);
    
    public int GetLegCount() => (HasPart(BodyPartType.LeftLeg) ? 1 : 0) + (HasPart(BodyPartType.RightLeg) ? 1 : 0);
    
    public bool HasSkull() => HasPart(BodyPartType.Head);
    
    public bool HasSoul() => HasPart(BodyPartType.Soul);

    public float GetMovementMultiplier()
    {
        int legs = GetLegCount();
        if (legs == 0) return 0.0f;
        if (legs == 1) return 0.5f;
        return 1.0f;
    }

    public BodyPart? RemovePart(BodyPartType type)
    {
        print("удалена часть тела");

        if (_attachedParts.TryGetValue(type, out BodyPart part))
        {
            _attachedParts.Remove(type);
            part.Detach();
            RefreshMeshVisibility();
            OnBodyChanged?.Invoke();
            return part;
        }
        return null;
    }

    public void AttachPart(BodyPart part)
    {
        if (part == null) return;

        print(part);
        if (HasPart(part.Item.Type)) 
        {   
            RemovePart(part.Item.Type);
        }

        Transform targetBone = GetBoneForType(part.Item.Type);
        
        _attachedParts[part.Item.Type] = part;
        part.Attach(gameObject, targetBone);
        RefreshMeshVisibility();
        OnBodyChanged?.Invoke();
    }

    private Transform GetBoneForType(BodyPartType type)
    {
        return type switch
        {
            BodyPartType.Head => headBone,
            BodyPartType.LeftArm => leftArmBone,
            BodyPartType.RightArm => rightArmBone,
            BodyPartType.LeftLeg => leftLegBone,
            BodyPartType.RightLeg => rightLegBone,
            BodyPartType.Torso => torsoBone,
            _ => transform
        };
    }
}