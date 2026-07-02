#nullable enable
using UnityEngine;

public class BodyPart : MonoBehaviour
{
    public BodyPartState State { get; private set; } = BodyPartState.Detached;
    public BodyPartItem Item { get; private set; } = null!;
    public BodyPartType Type => Item.Type;
    public GameObject? currentHolder;

    public void Initialize(BodyPartItem item)
    {
        Item = item;
    }

    public void Detach()
    {
        currentHolder = null;
        State = BodyPartState.Detached;
        transform.SetParent(null);
        SetColliderEnabled(true);
        EnableWorldPickup();
    }

    public void Attach(GameObject newOwner, Transform boneParent)
    {
        currentHolder = newOwner;
        State = BodyPartState.Attached;
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
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.maxAngularVelocity = 2f;
            rb.angularDamping = 5f;
            rb.mass = 1f;
        }

        Pickupable pickupable = GetComponent<Pickupable>();
        if (pickupable == null)
        {
            pickupable = gameObject.AddComponent<Pickupable>();
        }

        pickupable.Item = Item;
    }

    private void DisableWorldPickup()
    {
        Pickupable pickupable = GetComponent<Pickupable>();
        if (pickupable != null)
        {
            Destroy(pickupable);
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }
    }
}
