#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

public class TextMeshProStyleCleaner : EditorWindow
{
    [MenuItem("Tools/Remove Bold From All TextMeshPro")]
    public static void RemoveBoldFromAll()
    {
        if (!EditorUtility.DisplayDialog("Warning", 
            "This will scan all Prefabs in the project and remove Bold style from TextMeshProUGUI components.\nThis operation cannot be undone easily (you might need to revert via git).\n\nAre you sure?", 
            "Yes, Do it", "Cancel"))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;
        int modifiedCount = 0;

        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Skip Packages folder (immutable or external packages)
                if (path.StartsWith("Packages/") || path.Contains("/Packages/"))
                    continue;

                EditorUtility.DisplayProgressBar("Processing Prefabs", Path.GetFileName(path), (float)count / guids.Length);
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                bool isModified = false;
                
                // Load the prefab contents to modify
                string contentsRootPath = AssetDatabase.GetAssetPath(prefab);
                using (var editScope = new PrefabUtility.EditPrefabContentsScope(contentsRootPath))
                {
                    GameObject root = editScope.prefabContentsRoot;
                    TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);

                    foreach (var tmp in texts)
                    {
                        // Check if Bold style is applied
                        if ((tmp.fontStyle & FontStyles.Bold) == FontStyles.Bold)
                        {
                            // Remove Bold flag
                            tmp.fontStyle &= ~FontStyles.Bold;
                            isModified = true;
                        }
                    }

                    if (isModified)
                    {
                        modifiedCount++;
                        // The scope will automatically save changes when disposed if we don't throw exception
                        // But we can also manually mark as dirty just in case logic changes
                    }
                }
                
                count++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>Finished!</color> Processed {count} prefabs. Modified {modifiedCount} prefabs.");
        EditorUtility.DisplayDialog("Complete", $"Processed {count} prefabs.\nModified {modifiedCount} prefabs.", "OK");
    }
}

#endif
