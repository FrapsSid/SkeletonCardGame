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