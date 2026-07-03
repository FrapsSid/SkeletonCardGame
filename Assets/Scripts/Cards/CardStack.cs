#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CardStack : MonoBehaviour
{
    [Header("Cards")]
    [SerializeField] private GameObject? cardPrefab;
    [SerializeField] private List<CardData> cards = new();

    [Header("Layout")]
    [SerializeField] private Vector3 cardStep = new(0.45f, 0f, -0.001f);

    [SerializeField, HideInInspector] private List<GameObject> cardInstances = new();
    [SerializeField, HideInInspector] private List<WorldCardView> cardViews = new();

    public IReadOnlyList<CardData> Cards => cards;
    public GameObject? CardPrefab => cardPrefab;
    public Vector3 CardStep => cardStep;

    private void OnEnable()
    {
        Refresh();
    }

    private void OnValidate()
    {
        Refresh();
    }

    public void SetCardPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            throw new ArgumentNullException(nameof(prefab));
        }

        cardPrefab = prefab;
        Refresh();
    }

    public void SetCards(IReadOnlyList<CardData> newCards)
    {
        if (newCards == null)
        {
            throw new ArgumentNullException(nameof(newCards));
        }

        List<CardData> copiedCards = new List<CardData>(newCards.Count);
        for (int i = 0; i < newCards.Count; i++)
        {
            ValidateRequiredCard(newCards[i], nameof(newCards));
            copiedCards.Add(newCards[i]);
        }

        cards.Clear();
        cards.AddRange(copiedCards);
        Refresh();
    }

    public void AddCard(CardData card)
    {
        AddRequiredCard(card, nameof(card));
        Refresh();
    }

    public void InsertCard(int index, CardData card)
    {
        ValidateRequiredCard(card, nameof(card));
        cards.Insert(index, card);
        Refresh();
    }

    public void SetCard(int index, CardData card)
    {
        ValidateRequiredCard(card, nameof(card));
        cards[index] = card;
        Refresh();
    }

    public void SetCardStep(Vector3 step)
    {
        cardStep = step;
        LayoutCards();
    }

    public bool RemoveCard(CardData card)
    {
        bool removed = cards.Remove(card);
        if (removed)
        {
            Refresh();
        }

        return removed;
    }

    public bool RemoveCardAt(int index)
    {
        if (index < 0 || index >= cards.Count)
        {
            return false;
        }

        cards.RemoveAt(index);
        Refresh();
        return true;
    }

    public void ClearCards()
    {
        if (cards.Count == 0)
        {
            return;
        }

        cards.Clear();
        Refresh();
    }

    public List<CardData> GetCards()
    {
        return new List<CardData>(cards);
    }

    public void Refresh()
    {
        PruneMissingInstances();
        EnsureInstanceCount();
        ApplyCards();
        LayoutCards();
    }

    private void EnsureInstanceCount()
    {
        if (cardPrefab == null)
        {
            DestroyExtraInstances(0);
            return;
        }

        while (cardInstances.Count < cards.Count)
        {
            if (!CreateCardInstance(cardInstances.Count))
            {
                break;
            }
        }

        DestroyExtraInstances(cards.Count);
    }

    private bool CreateCardInstance(int index)
    {
        if (cardPrefab == null)
        {
            return false;
        }

        GameObject instance = Instantiate(cardPrefab, transform, false);
        instance.name = $"{cardPrefab.name} ({index})";

        WorldCardView? view = instance.GetComponentInChildren<WorldCardView>(true);
        if (view == null)
        {
            Debug.LogError("CardStack card prefab must contain a WorldCardView component.", this);
            DestroyGeneratedObject(instance);
            return false;
        }

        cardInstances.Add(instance);
        cardViews.Add(view);
        return true;
    }

    private void ApplyCards()
    {
        int visibleCount = Mathf.Min(cards.Count, cardViews.Count);

        for (int i = 0; i < visibleCount; i++)
        {
            WorldCardView view = cardViews[i];
            CardData card = cards[i];

            if (view == null || card == null)
            {
                SetInstanceActive(i, false);
                continue;
            }

            SetInstanceActive(i, true);
            view.SetCard(card);
        }
    }

    private void LayoutCards()
    {
        int instanceCount = cardInstances.Count;
        for (int i = 0; i < instanceCount; i++)
        {
            GameObject instance = cardInstances[i];
            if (instance == null)
            {
                continue;
            }

            instance.transform.SetSiblingIndex(i);
            instance.transform.localPosition = cardStep * i;
        }
    }

    private void SetInstanceActive(int index, bool isActive)
    {
        if (index < 0 || index >= cardInstances.Count)
        {
            return;
        }

        GameObject instance = cardInstances[index];
        if (instance != null && instance.activeSelf != isActive)
        {
            instance.SetActive(isActive);
        }
    }

    private void DestroyExtraInstances(int requiredCount)
    {
        for (int i = cardInstances.Count - 1; i >= requiredCount; i--)
        {
            DestroyCardInstanceAt(i);
        }
    }

    private void DestroyCardInstanceAt(int index)
    {
        GameObject instance = cardInstances[index];

        cardInstances.RemoveAt(index);
        if (index < cardViews.Count)
        {
            cardViews.RemoveAt(index);
        }

        DestroyGeneratedObject(instance);
    }

    private void PruneMissingInstances()
    {
        for (int i = cardInstances.Count - 1; i >= 0; i--)
        {
            GameObject instance = cardInstances[i];
            if (instance == null)
            {
                cardInstances.RemoveAt(i);
                if (i < cardViews.Count)
                {
                    cardViews.RemoveAt(i);
                }
                continue;
            }

            if (i >= cardViews.Count || cardViews[i] == null)
            {
                WorldCardView? view = instance.GetComponentInChildren<WorldCardView>(true);
                if (view == null)
                {
                    DestroyCardInstanceAt(i);
                }
                else if (i >= cardViews.Count)
                {
                    cardViews.Add(view);
                }
                else
                {
                    cardViews[i] = view;
                }
            }
        }

        while (cardViews.Count > cardInstances.Count)
        {
            cardViews.RemoveAt(cardViews.Count - 1);
        }
    }

    private void AddRequiredCard(CardData card, string parameterName)
    {
        ValidateRequiredCard(card, parameterName);
        cards.Add(card);
    }

    private static void ValidateRequiredCard(CardData card, string parameterName)
    {
        if (card == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    private static void DestroyGeneratedObject(UnityEngine.Object? target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
