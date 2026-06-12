using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardAtlas))]
public class CardAtlasEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CardAtlas atlas = (CardAtlas)target;

        GUILayout.Space(15);

        if (GUILayout.Button("Generate Card Mapping", GUILayout.Height(30)))
        {
            GenerateMappings(atlas);
        }
    }

    private void GenerateMappings(CardAtlas atlas)
    {
        string folderPath = atlas.SpritesFolderPath;

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"Directory does not exist: {folderPath}. Ensure the path starts with 'Assets/'");
            return;
        }

        // Retrieve all sprite files in the specified directory
        string[] fileEntries = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        List<CardSpriteMapping> newMappings = new List<CardSpriteMapping>();

        int successfullyMapped = 0;

        foreach (string filePath in fileEntries)
        {
            // Skip .meta files
            if (filePath.EndsWith(".meta")) continue;

            // Extract file name without extension
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string[] parts = fileName.Split('_');

            // Verify if the name matches the "suit_value" format
            if (parts.Length != 2) continue;

            string suitPart = parts[0];
            string valuePart = parts[1];

            // Attempt to parse Suit and Value from the filename
            bool isSuitValid = Enum.TryParse(suitPart, true, out CardSuit suit);
            bool isValueValid = int.TryParse(valuePart, out int intValue);

            // If value is written as text
            if (!isValueValid)
            {
                isValueValid = Enum.TryParse(valuePart, true, out CardValue parsedValue);
                if (isValueValid) intValue = (int)parsedValue;
            }

            if (isSuitValid && isValueValid && Enum.IsDefined(typeof(CardValue), intValue))
            {
                // Load the sprite asset from the Unity project database
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);

                if (sprite != null)
                {
                    CardSpriteMapping mapping = new CardSpriteMapping
                    {
                        suit = suit,
                        value = (CardValue)intValue,
                        faceSprite = sprite
                    };
                    newMappings.Add(mapping);
                    successfullyMapped++;
                }
            }
        }

        // Apply the newly generated list to the ScriptableObject
        Undo.RecordObject(atlas, "Auto Generate Card Mapping"); // Allows undo operations via Ctrl+Z
        atlas.FaceSpritesList = newMappings;
        EditorUtility.SetDirty(atlas); // Informs Unity that the asset data has changed and needs saving

        Debug.Log($"[CardAtlas] Auto-mapping complete! Successfully mapped cards: {successfullyMapped}/52.");
    }
}
