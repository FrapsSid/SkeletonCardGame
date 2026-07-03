#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace Interactions
{
    [AddComponentMenu("Interactions/Interaction Highlighter")]
    [DisallowMultipleComponent]
    public sealed class InteractionHighlighter : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor? playerInteractor;

        private readonly HashSet<GameObject> highlightedObjects = new();
        private readonly List<GameObject> objectsToRemove = new();

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void LateUpdate()
        {
            ResolveReferences();

            if (playerInteractor == null)
            {
                ClearHighlights();
                return;
            }

            IReadOnlyDictionary<GameObject, IList<Interaction>> interactionsByObject = playerInteractor.InteractionsByObject;
            foreach (GameObject sourceObject in interactionsByObject.Keys)
            {
                if (sourceObject == null || !highlightedObjects.Add(sourceObject))
                {
                    continue;
                }

                SetHighlighted(sourceObject, true);
            }

            objectsToRemove.Clear();
            foreach (GameObject sourceObject in highlightedObjects)
            {
                if (sourceObject == null || !interactionsByObject.ContainsKey(sourceObject))
                {
                    objectsToRemove.Add(sourceObject);
                }
            }

            for (int i = 0; i < objectsToRemove.Count; i++)
            {
                GameObject sourceObject = objectsToRemove[i];
                if (sourceObject != null)
                {
                    SetHighlighted(sourceObject, false);
                }

                highlightedObjects.Remove(sourceObject);
            }
        }

        private void OnDisable()
        {
            ClearHighlights();
        }

        private void ResolveReferences()
        {
            if (playerInteractor == null)
            {
                playerInteractor = GetComponent<PlayerInteractor>();
            }
        }

        private void ClearHighlights()
        {
            foreach (GameObject sourceObject in highlightedObjects)
            {
                if (sourceObject != null)
                {
                    SetHighlighted(sourceObject, false);
                }
            }

            highlightedObjects.Clear();
            objectsToRemove.Clear();
        }

        private static void SetHighlighted(GameObject sourceObject, bool highlighted)
        {
            InteractableHighlight? highlight = sourceObject.GetComponent<InteractableHighlight>();
            if (highlight == null)
            {
                if (!highlighted)
                {
                    return;
                }

                highlight = sourceObject.AddComponent<InteractableHighlight>();
            }

            highlight.SetFocused(highlighted);
        }
    }
}
