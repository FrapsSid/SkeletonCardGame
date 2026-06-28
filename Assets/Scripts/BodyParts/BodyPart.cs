using UnityEngine;

public class BodyPart : MonoBehaviour
{
    [Header("Part Info")]
    public BodyPartType Type;
    public BodyPartState State = BodyPartState.Attached;

    [Header("Ownership")]
    public GameObject CurrentOwner;
    public GameObject OriginalOwner;

    [Header("Pickup")]
    public ItemData itemData;

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
        SetColliderEnabled(true);
        EnableWorldPickup();
    }

    public void Attach(GameObject newOwner, Transform boneParent)
    {
        State = BodyPartState.Attached;
        CurrentOwner = newOwner;
        SetColliderEnabled(false);
        DisableWorldPickup();
        
        transform.SetParent(boneParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
    private void SetColliderEnabled(bool enabled)
    {
        foreach (Collider col in GetComponents<Collider>())
            col.enabled = enabled;
    }
    private void EnableWorldPickup()
    {
        if (GetComponent<Rigidbody>() == null) {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.maxAngularVelocity = 2f;
            rb.angularDamping = 5f;
            rb.mass = 1f;
        }

        Pickupable pickupable = GetComponent<Pickupable>();
        if (pickupable == null)
            pickupable = gameObject.AddComponent<Pickupable>();

        pickupable.itemData = itemData;
        pickupable.SetPickupable(true);
    }

    private void DisableWorldPickup()
    {
        Pickupable pickupable = GetComponent<Pickupable>();
        if (pickupable != null) Destroy(pickupable);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
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