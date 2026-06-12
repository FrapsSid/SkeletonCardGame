using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CardSpriteMapping
{
    public CardSuit suit;
    public CardValue value;
    public Sprite faceSprite;
}

[CreateAssetMenu(fileName = "NewCardAtlas", menuName = "Cards/Card Atlas")]
public class CardAtlas : ScriptableObject, ISerializationCallbackReceiver
{
    [Header("Auto-Mapping Settings")]
    [Tooltip("Folder path to card sprites relative to the Assets folder (e.g., Assets/Sprites/Cards)")]
    [SerializeField] private string spritesFolderPath = "Assets/Sprites/Cards";

    [Header("Back Sprite")]
    [SerializeField] private Sprite cardBackSprite;

    [Header("Face Sprites")]
    [SerializeField] private List<CardSpriteMapping> faceSpritesList = new List<CardSpriteMapping>();

    private Dictionary<(CardSuit, CardValue), Sprite> _faceSpritesDictionary;

    public Sprite GetBackSprite() => cardBackSprite;

    public string SpritesFolderPath => spritesFolderPath;

    public List<CardSpriteMapping> FaceSpritesList
    {
        get => faceSpritesList;
        set => faceSpritesList = value;
    }

    public Sprite GetFaceSprite(CardSuit suit, CardValue value)
    {
        if (_faceSpritesDictionary != null && _faceSpritesDictionary.TryGetValue((suit, value), out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogError($"Спрайт для карты {value} {suit} не найден в атласе!");
        return null;
    }

    #region Реализация ISerializationCallbackReceiver
    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        _faceSpritesDictionary = new Dictionary<(CardSuit, CardValue), Sprite>();

        foreach (var mapping in faceSpritesList)
        {
            var key = (mapping.suit, mapping.value);
            if (!_faceSpritesDictionary.ContainsKey(key))
            {
                _faceSpritesDictionary.Add(key, mapping.faceSprite);
            }
        }
    }
    #endregion
}
