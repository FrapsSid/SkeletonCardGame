using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class RelinkFromSource : EditorWindow
{
    private GameObject sourceRoot; // "final loca" из Project (рабочий, с мешами/материалами)
    private GameObject targetRoot; // "final loca" из Hierarchy (сломанный)

    [MenuItem("Tools/Relink From Source")]
    public static void ShowWindow()
    {
        GetWindow<RelinkFromSource>("Relink From Source");
    }

    void OnGUI()
    {
        GUILayout.Label("Копирование мешей/материалов по совпадению имени", EditorStyles.boldLabel);
        sourceRoot = (GameObject)EditorGUILayout.ObjectField("Source (эталон, из Assets)", sourceRoot, typeof(GameObject), true);
        targetRoot = (GameObject)EditorGUILayout.ObjectField("Target (сломанный, из Hierarchy)", targetRoot, typeof(GameObject), true);

        if (GUILayout.Button("Перепривязать"))
            Relink();
    }

    void Relink()
    {
        if (sourceRoot == null || targetRoot == null)
        {
            Debug.LogError("Укажите и source, и target объекты.");
            return;
        }

        // Собираем все дочерние объекты источника в словарь по имени
        var sourceMeshFilters = sourceRoot.GetComponentsInChildren<MeshFilter>(true)
            .Where(mf => mf.sharedMesh != null)
            .GroupBy(mf => mf.gameObject.name)
            .ToDictionary(g => g.Key, g => g.First());

        var sourceRenderers = sourceRoot.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.sharedMaterials != null && r.sharedMaterials.Length > 0)
            .GroupBy(r => r.gameObject.name)
            .ToDictionary(g => g.Key, g => g.First());

        int meshFixed = 0, matFixed = 0, notFoundMesh = 0, notFoundMat = 0;

        // Меши
        foreach (var targetMf in targetRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            string name = targetMf.gameObject.name;
            if (sourceMeshFilters.TryGetValue(name, out var srcMf))
            {
                Undo.RecordObject(targetMf, "Relink Mesh");
                targetMf.sharedMesh = srcMf.sharedMesh;
                EditorUtility.SetDirty(targetMf);
                meshFixed++;
            }
            else
            {
                notFoundMesh++;
                Debug.LogWarning($"Меш не найден для: {name}", targetMf.gameObject);
            }
        }

        // Материалы
        foreach (var targetR in targetRoot.GetComponentsInChildren<Renderer>(true))
        {
            string name = targetR.gameObject.name;
            if (sourceRenderers.TryGetValue(name, out var srcR))
            {
                Undo.RecordObject(targetR, "Relink Materials");
                targetR.sharedMaterials = srcR.sharedMaterials;
                EditorUtility.SetDirty(targetR);
                matFixed++;
            }
            else
            {
                notFoundMat++;
                Debug.LogWarning($"Материал не найден для: {name}", targetR.gameObject);
            }
        }

        Debug.Log($"Готово. Меши: {meshFixed} привязано, {notFoundMesh} не найдено. " +
                  $"Материалы: {matFixed} привязано, {notFoundMat} не найдено.");
    }
}