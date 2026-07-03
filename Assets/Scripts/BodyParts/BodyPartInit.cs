#nullable enable
using UnityEngine;

namespace BodyParts
{
    public class BodyPartInit : MonoBehaviour
    {
        [SerializeField] private GameObject prefab = null!;
        [SerializeField] private BodyPartType type;

        private void Awake()
        {
            var player = GetComponent<PlayerInventoryOwner>()?.OwnerSkeleton;
            GetComponent<BodyPart>().Initialize(new BodyPartItem(prefab, type, player));
        }
    }
}