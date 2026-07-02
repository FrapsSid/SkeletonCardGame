#nullable enable
using System;
using UnityEngine;

public class SimpleItemInit : MonoBehaviour
{
    [SerializeField] private string itemName = null!;
    [SerializeField] private string description = null!;
    [SerializeField] private GameObject prefab = null!;

    private void Start()
    {
        GetComponent<Pickupable>().Item = new SimpleItem(itemName, description, prefab);
    }
    
}