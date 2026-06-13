using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkeletonBody : MonoBehaviour
{
    public event Action OnBodyChanged;

    private Dictionary<BodyPartType, BodyPart> _attachedParts = new Dictionary<BodyPartType, BodyPart>();

    [Header("Bone References")]
    public Transform headBone;
    public Transform leftArmBone;
    public Transform rightArmBone;
    public Transform leftLegBone;
    public Transform rightLegBone;

    public bool IsIncapacitated => !HasSoul();
    public bool CanHoldCards => GetArmCount() > 0;

    private void Start()
    {
        BodyPart[] initialParts = GetComponentsInChildren<BodyPart>();
        foreach (var part in initialParts)
        {
            part.Initialize(gameObject);
            _attachedParts[part.Type] = part;
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
        if (type == BodyPartType.Torso)
        {
            Debug.LogWarning("Невозможно отделить торс");
            return null;
        }

        if (_attachedParts.TryGetValue(type, out BodyPart part))
        {
            _attachedParts.Remove(type);
            part.Detach();
            OnBodyChanged?.Invoke();
            return part;
        }
        return null;
    }

    public void AttachPart(BodyPart part)
    {
        if (part == null) return;

        if (HasPart(part.Type)) 
        {
            RemovePart(part.Type);
        }

        Transform targetBone = GetBoneForType(part.Type);
        
        _attachedParts[part.Type] = part;
        part.Attach(gameObject, targetBone);
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
            _ => transform
        };
    }
}