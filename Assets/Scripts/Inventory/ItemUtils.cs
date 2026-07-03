#nullable enable

using UnityEngine;

public static class ItemUtils
{
    public static void DropItem(IItem item, Vector3 position, Quaternion rotation)
    {
        GameObject droppedObject = item.CreateDropped();
        if (droppedObject == null)
        {
            return;
        }

        Pickupable pickupable = droppedObject.GetComponent<Pickupable>();
        if (pickupable != null && pickupable.Item == null)
        {
            pickupable.Item = item;
        }

        droppedObject.transform.SetPositionAndRotation(position, rotation);
    }

    public static GameObject InstantiateWithPickupable(GameObject prefab, bool isPickupable)
    {
        var obj = Object.Instantiate(prefab);
        var pickupable = obj.GetComponent<Pickupable>();
        if (isPickupable)
            pickupable = obj.AddComponent<Pickupable>();
        if (pickupable != null)
            pickupable.enabled = isPickupable;
        return obj;
    }
}
