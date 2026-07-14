#nullable enable
using System;
using System.Collections.Generic;
using Interactions;
using Unity.Netcode;
using UnityEngine;

public class NetworkPickupableSync : NetworkBehaviour
{
    private static NetworkPickupableSync? _instance;
    public static NetworkPickupableSync Instance => _instance!;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            enabled = false;
            return;
        }
        _instance = this;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (_instance == this)
            _instance = null;
    }

    public override void OnNetworkSpawn()
    {
        Pickupable.OnPickupRequested += HandlePickupRequested;
    }

    public override void OnNetworkDespawn()
    {
        Pickupable.OnPickupRequested -= HandlePickupRequested;
    }

    private void HandlePickupRequested(Pickupable pickupable, Skeleton player)
    {
        if (pickupable == null || player == null)
            return;

        if (!IsServer)
        {
            int playerIndex = GetPlayerIndex(player);
            int interactionType = 0;

            if (pickupable.Item is BodyPartItem bpi)
            {
                int sourceIndex = GetSourcePlayerIndex(bpi);
                RequestBodyPartPickupServerRpc((int)bpi.Type, sourceIndex, playerIndex, interactionType);
            }
            else
            {
                int handIndex = GetPreferredHand(player);
                RequestGenericPickupServerRpc(pickupable.Item?.Name ?? "", playerIndex, handIndex);
            }
        }
    }

    public void BroadcastPickupComplete(Pickupable pickupable, PlayerInventoryOwner player)
    {
        if (!IsServer || pickupable == null || player == null)
            return;

        Skeleton? skeleton = player.OwnerSkeleton;
        if (skeleton == null)
            return;

        int playerIndex = GetPlayerIndex(skeleton);
        int handIndex = GetHandIndex(pickupable, player);

        if (pickupable.Item is BodyPartItem bpi)
        {
            int sourceIndex = GetSourcePlayerIndex(bpi);
            PickupBodyPartCompleteClientRpc((int)bpi.Type, sourceIndex, playerIndex, handIndex);
        }
        else
        {
            PickupGenericCompleteClientRpc(pickupable.Item?.Name ?? "", playerIndex, handIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBodyPartPickupServerRpc(int partTypeValue, int sourcePlayerIndex, int pickingPlayerIndex, int interactionType, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        BodyPartType partType = (BodyPartType)partTypeValue;

        Pickupable? pickupable = FindBodyPartPickupable(partType, sourcePlayerIndex, gm);
        if (pickupable == null) return;

        Skeleton? picker = pickingPlayerIndex >= 0 && pickingPlayerIndex < gm.Players.Count
            ? gm.Players[pickingPlayerIndex]
            : null;

        if (picker?.InventoryOwner == null) return;

        Pickupable.OnPickupRequested -= HandlePickupRequested;
        try
        {
            InteractionType iType = interactionType == 1 ? InteractionType.RightHand : InteractionType.LeftHand;
            pickupable.Pickup(picker.InventoryOwner, iType);
        }
        finally
        {
            Pickupable.OnPickupRequested += HandlePickupRequested;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGenericPickupServerRpc(string itemName, int pickingPlayerIndex, int handIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        Pickupable? pickupable = FindGenericPickupable(itemName);
        if (pickupable == null) return;

        Skeleton? picker = pickingPlayerIndex >= 0 && pickingPlayerIndex < gm.Players.Count
            ? gm.Players[pickingPlayerIndex]
            : null;

        if (picker?.InventoryOwner == null) return;

        Pickupable.OnPickupRequested -= HandlePickupRequested;
        try
        {
            InteractionType iType = handIndex == 1 ? InteractionType.RightHand : InteractionType.LeftHand;
            pickupable.Pickup(picker.InventoryOwner, iType);
        }
        finally
        {
            Pickupable.OnPickupRequested += HandlePickupRequested;
        }
    }

    [ClientRpc]
    private void PickupBodyPartCompleteClientRpc(int partTypeValue, int sourcePlayerIndex, int pickingPlayerIndex, int handIndex)
    {
        if (IsHost) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        BodyPartType partType = (BodyPartType)partTypeValue;

        Pickupable? pickupable = FindBodyPartPickupable(partType, sourcePlayerIndex, gm);
        if (pickupable != null)
        {
            IItem? savedItem = pickupable.Item;
            UnityEngine.Object.Destroy(pickupable.gameObject);

            Skeleton? picker = pickingPlayerIndex >= 0 && pickingPlayerIndex < gm.Players.Count
                ? gm.Players[pickingPlayerIndex]
                : null;

            if (picker?.InventoryOwner != null && savedItem != null)
            {
                PlayerHand hand = handIndex == 1 ? picker.InventoryOwner.rightHand : picker.InventoryOwner.leftHand;
                if (hand != null && !hand.HasItem)
                    hand.SetItem(savedItem);
            }
        }
    }

    [ClientRpc]
    private void PickupGenericCompleteClientRpc(string itemName, int pickingPlayerIndex, int handIndex)
    {
        if (IsHost) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        Pickupable? pickupable = FindGenericPickupable(itemName);
        if (pickupable != null)
        {
            IItem? savedItem = pickupable.Item;
            UnityEngine.Object.Destroy(pickupable.gameObject);

            Skeleton? picker = pickingPlayerIndex >= 0 && pickingPlayerIndex < gm.Players.Count
                ? gm.Players[pickingPlayerIndex]
                : null;

            if (picker?.InventoryOwner != null && savedItem != null)
            {
                PlayerHand hand = handIndex == 1 ? picker.InventoryOwner.rightHand : picker.InventoryOwner.leftHand;
                if (hand != null && !hand.HasItem)
                    hand.SetItem(savedItem);
            }
        }
    }

    private static Pickupable? FindBodyPartPickupable(BodyPartType partType, int sourcePlayerIndex, GameManager gm)
    {
        Skeleton? sourcePlayer = sourcePlayerIndex >= 0 && sourcePlayerIndex < gm.Players.Count
            ? gm.Players[sourcePlayerIndex]
            : null;

        Pickupable[] all = FindObjectsByType<Pickupable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Pickupable? closest = null;
        float closestDist = float.MaxValue;

        foreach (var p in all)
        {
            if (p == null || p.Item == null)
                continue;

            if (p.Item is BodyPartItem bpi && bpi.Type == partType)
            {
                if (sourcePlayer != null && bpi.OriginalOwner != null && bpi.OriginalOwner != sourcePlayer)
                    continue;

                float dist = sourcePlayer?.Body != null
                    ? Vector3.Distance(p.transform.position, sourcePlayer.Body.transform.position)
                    : 0f;

                if (closest == null || dist < closestDist)
                {
                    closest = p;
                    closestDist = dist;
                }
            }
        }

        return closest;
    }

    private static Pickupable? FindGenericPickupable(string itemName)
    {
        Pickupable[] all = FindObjectsByType<Pickupable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in all)
        {
            if (p != null && p.Item != null && p.Item.Name == itemName)
                return p;
        }
        return null;
    }

    private static int GetSourcePlayerIndex(BodyPartItem bpi)
    {
        if (bpi.OriginalOwner == null) return -1;
        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return -1;
        var players = new List<Skeleton>(gm.Players);
        return players.IndexOf(bpi.OriginalOwner);
    }

    private static int GetPlayerIndex(Skeleton player)
    {
        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return -1;
        var players = new List<Skeleton>(gm.Players);
        return players.IndexOf(player);
    }

    private static int GetPreferredHand(Skeleton player)
    {
        if (player.InventoryOwner == null) return 0;
        if (player.InventoryOwner.leftHand != null && !player.InventoryOwner.leftHand.HasItem)
            return 0;
        return 1;
    }

    private static int GetHandIndex(Pickupable pickupable, PlayerInventoryOwner player)
    {
        if (player.leftHand != null && !player.leftHand.HasItem)
            return 0;
        return 1;
    }

    public void BroadcastItemDropped(IItem item, Vector3 position, Quaternion rotation, int playerIndex, int handIndex)
    {
        if (!IsServer || item == null)
            return;

        if (item is BodyPartItem bpi)
        {
            BodyPartDroppedClientRpc((int)bpi.Type, playerIndex, position, rotation, handIndex);
        }
        else
        {
            ItemDroppedClientRpc(item.Name, playerIndex, position, rotation, handIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDropServerRpc(string itemName, int playerIndex, int handIndex, Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        Skeleton? player = playerIndex >= 0 && playerIndex < gm.Players.Count
            ? gm.Players[playerIndex]
            : null;

        if (player?.InventoryOwner == null) return;

        PlayerHand hand = handIndex == 0 ? player.InventoryOwner.leftHand : player.InventoryOwner.rightHand;
        IItem? item = hand?.Item;
        if (item == null || item is CardsItem) return;

        ItemUtils.DropItem(item, position, rotation);
        hand.SetItem(null);

        BroadcastItemDropped(item, position, rotation, playerIndex, handIndex);
    }

    [ClientRpc]
    private void BodyPartDroppedClientRpc(int partTypeValue, int droppingPlayerIndex, Vector3 position, Quaternion rotation, int handIndex)
    {
        if (IsHost) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        BodyPartType partType = (BodyPartType)partTypeValue;

        Skeleton? droppingPlayer = droppingPlayerIndex >= 0 && droppingPlayerIndex < gm.Players.Count
            ? gm.Players[droppingPlayerIndex]
            : null;

        if (droppingPlayer?.InventoryOwner != null)
        {
            PlayerHand hand = handIndex == 0 ? droppingPlayer.InventoryOwner.leftHand : droppingPlayer.InventoryOwner.rightHand;
            if (hand != null && hand.HasItem)
            {
                IItem? heldItem = hand.Item;
                hand.SetItem(null);

                if (heldItem != null)
                {
                    GameObject dropped = heldItem.CreateDropped();
                    if (dropped != null)
                    {
                        dropped.transform.position = position;
                        dropped.transform.rotation = rotation;
                    }
                }
                return;
            }
        }

        if (droppingPlayer?.Body != null && droppingPlayer.Body.HasPart(partType))
        {
            BodyPart? detached = droppingPlayer.Body.RemovePart(partType);
            if (detached != null)
            {
                detached.transform.position = position;
                detached.transform.rotation = rotation;
            }
        }
    }

    [ClientRpc]
    private void ItemDroppedClientRpc(string itemName, int droppingPlayerIndex, Vector3 position, Quaternion rotation, int handIndex)
    {
        if (IsHost) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        Skeleton? droppingPlayer = droppingPlayerIndex >= 0 && droppingPlayerIndex < gm.Players.Count
            ? gm.Players[droppingPlayerIndex]
            : null;

        if (droppingPlayer?.InventoryOwner != null)
        {
            PlayerHand hand = handIndex == 0 ? droppingPlayer.InventoryOwner.leftHand : droppingPlayer.InventoryOwner.rightHand;
            if (hand != null && hand.HasItem && hand.Item != null && hand.Item.Name == itemName)
            {
                IItem? heldItem = hand.Item;
                hand.SetItem(null);

                GameObject dropped = heldItem.CreateDropped();
                if (dropped != null)
                {
                    dropped.transform.position = position;
                    dropped.transform.rotation = rotation;
                }
            }
        }
    }
}
