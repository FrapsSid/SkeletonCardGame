using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class RelinkMaterialsFromFolder : EditorWindow
{
    private GameObject targetRoot;      // сломанный "final loca" из Hierarchy
    private Object materialsFolder;     // например Assets/Location
    private bool alsoMatchByObjectName = true; // если материал null - пробовать по имени объекта

    [MenuItem("Tools/Relink Materials From Folder")]
    public static void ShowWindow()
    {
        GetWindow<RelinkMaterialsFromFolder>("Relink Materials From Folder");
    }

    void OnGUI()
    {
        GUILayout.Label("Замена материалов на версии из папки (по имени)", EditorStyles.boldLabel);
        targetRoot = (GameObject)EditorGUILayout.ObjectField("Target (объект в сцене)", targetRoot, typeof(GameObject), true);
        materialsFolder = EditorGUILayout.ObjectField("Папка с материалами", materialsFolder, typeof(Object), false);
        alsoMatchByObjectName = EditorGUILayout.Toggle("Если материал пустой - искать по имени объекта", alsoMatchByObjectName);

        if (GUILayout.Button("Заменить материалы"))
            Relink();
    }

    // Убираем типичные суффиксы, которые Unity/FBX добавляют к именам материалов
    string CleanName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Replace(" (Instance)", "");
        // Убираем хвост вида .001, .002 и т.п., если нужно грубое совпадение
        return name.Trim();
    }

    void Relink()
    {
        if (targetRoot == null || materialsFolder == null)
        {
            Debug.LogError("Укажите Target и папку с материалами.");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(materialsFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError("Указанный объект не является папкой.");
            return;
        }

        // Собираем словарь материалов из папки по имени
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        var materialsByName = new Dictionary<string, Material>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string key = CleanName(mat.name);
            if (!materialsByName.ContainsKey(key))
                materialsByName.Add(key, mat);
        }

        int replaced = 0, notFound = 0;

        foreach (var renderer in targetRoot.GetComponentsInChildren<Renderer>(true))
        {
            var mats = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                string lookupKey = null;

                if (mats[i] != null)
                {
                    // Материал есть (пусть даже битый/старый) - ищем аналог по его имени
                    lookupKey = CleanName(mats[i].name);
                }
                else if (alsoMatchByObjectName)
                {
                    // Материала нет - пробуем по имени объекта
                    lookupKey = CleanName(renderer.gameObject.name);
                }

                if (lookupKey == null) continue;

                if (materialsByName.TryGetValue(lookupKey, out var folderMat))
                {
                    mats[i] = folderMat;
                    changed = true;
                    replaced++;
                }
                else
                {
                    notFound++;
                    Debug.LogWarning($"Материал '{lookupKey}' не найден в папке для объекта {renderer.gameObject.name}", renderer.gameObject);
                }
            }

            if (changed)
            {
                Undo.RecordObject(renderer, "Relink Material From Folder");
                renderer.sharedMaterials = mats;
                EditorUtility.SetDirty(renderer);
            }
        }

        Debug.Log($"Готово. Заменено слотов: {replaced}, не найдено: {notFound}");
    }
}