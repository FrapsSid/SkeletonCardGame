using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkeletonBody : MonoBehaviour
{
    public event Action OnBodyChanged;

    private Dictionary<BodyPartType, BodyPart> _attachedParts = new();

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
        return type switch
        {
            BodyPartType.Head => headMeshFolder,
            BodyPartType.LeftArm => leftArmMeshFolder,
            BodyPartType.RightArm => rightArmMeshFolder,
            BodyPartType.LeftLeg => leftLegMeshFolder,
            BodyPartType.RightLeg => rightLegMeshFolder,
            _ => null
        };
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
        Transform folder = GetMeshFolderForType(type);
        if (folder == null) return;

        foreach (SkinnedMeshRenderer smr in folder.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.enabled = visible;
        }
    }

    public bool IsIncapacitated => !HasSoul();
    public bool CanHoldCards => GetArmCount() > 0;

    private void Start()
    {
        BodyPart[] initialParts = GetComponentsInChildren<BodyPart>();
        foreach (var part in initialParts)
        {
            AttachPart(part);
        }
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

    public BodyPart RemovePart(BodyPartType type)
    {
        print("удалена часть тела");
        if (type == BodyPartType.Torso)
        {
            Debug.LogWarning("Невозможно отделить торс");
            return null;
        }

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