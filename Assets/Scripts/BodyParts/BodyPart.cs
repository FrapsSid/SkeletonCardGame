using UnityEngine;

public class BodyPart : MonoBehaviour
{
    [Header("Part Info")]
    public BodyPartType Type;
    public BodyPartState State = BodyPartState.Attached;

    [Header("Ownership")]
    public GameObject CurrentOwner;
    public GameObject OriginalOwner;

    public void Initialize(GameObject owner)
    {
        OriginalOwner = owner;
        CurrentOwner = owner;
        State = BodyPartState.Attached;
    }

    public void Detach()
    {
        State = BodyPartState.Detached;
        CurrentOwner = null;
        transform.SetParent(null);
    }

    public void Attach(GameObject newOwner, Transform boneParent)
    {
        State = BodyPartState.Attached;
        CurrentOwner = newOwner;
        
        transform.SetParent(boneParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}

public static class BodyPartExtensions 
{ 
    public static int GetBodyPartCost(BodyPart bodyPart) 
    { 
        switch (bodyPart.Type) 
        { 
            case BodyPartType.Head:
                return 3; 
            case BodyPartType.LeftArm:
                return 2;
            case BodyPartType.RightArm:
                return 2;
            case BodyPartType.LeftLeg:
                return 2;
            case BodyPartType.RightLeg:
                return 2;
            case BodyPartType.Soul:
                return 6;
            case BodyPartType.Torso:
                return 1;
            default:
                return 1;
        }
    }
}