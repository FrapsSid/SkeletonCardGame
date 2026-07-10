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
        item.SetCurrentBodyPart(this);
    }

    private void OnDestroy()
    {
        Item.ClearCurrentBodyPart(this);
    }

    public void Detach()
    {
        State = BodyPartState.Detached;
        transform.SetParent(null);
        SetColliderEnabled(true);
        EnableWorldPickup();

        SetOwnRenderersEnabled(true);
        if (Item.Type == BodyPartType.LeftArm || Item.Type == BodyPartType.RightArm)
        {
            var hand = GetComponent<PlayerHand>();
            var item = hand?.Item;
            if (hand != null && item != null)
            {
                ItemUtils.DropItem(item, hand.transform.position, Quaternion.identity);
                hand.SetItem(null);
            }

            if (currentHolder != null)
            {
                var inventoryOwner = currentHolder.GetComponent<PlayerInventoryOwner>();
                if (inventoryOwner != null)
                {
                    if (Item.Type == BodyPartType.LeftArm)
                        inventoryOwner.leftHand = null;
                    if (Item.Type == BodyPartType.RightArm)
                        inventoryOwner.rightHand = null;
                }
            }
        }

        currentHolder = null;
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

        SetOwnRenderersEnabled(false);
        if (Item.Type == BodyPartType.LeftArm || Item.Type == BodyPartType.RightArm)
        {
            var hand = GetComponent<PlayerHand>();
            var inventoryOwner = currentHolder?.GetComponent<PlayerInventoryOwner>();
            if (inventoryOwner != null)
            {
                if (Item.Type == BodyPartType.LeftArm)
                    inventoryOwner.leftHand = hand;
                if (Item.Type == BodyPartType.RightArm)
                    inventoryOwner.rightHand = hand;
            }
        }
    }

    private void SetOwnRenderersEnabled(bool enabled)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (IsHeldItemRenderer(renderer))
                continue;
                
            renderer.enabled = enabled;
        }
    }

    private bool IsHeldItemRenderer(Renderer renderer)
    {
        var hand = GetComponent<PlayerHand>();
        return hand != null && hand.ContainsHeldItemRenderer(renderer);
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
