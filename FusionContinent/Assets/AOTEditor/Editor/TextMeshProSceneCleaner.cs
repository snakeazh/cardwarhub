#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class TextMeshProSceneCleaner : EditorWindow
{
    [MenuItem("Tools/Remove Bold From All TextMeshPro (Current Scene)")]
    public static void RemoveBoldFromCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!EditorUtility.DisplayDialog("Warning", 
                $"This will scan the CURRENT scene '{scene.name}' and remove Bold style from TextMeshProUGUI components.\n\nAre you sure?", 
                "Yes, Process Current Scene", "Cancel"))
        {
            return;
        }

        int modifiedCount = 0;
        bool isModified = false;
        
        // Find all root objects
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in texts)
            {
                if ((tmp.fontStyle & FontStyles.Bold) == FontStyles.Bold)
                {
                    Undo.RecordObject(tmp, "Remove Bold Style");
                    tmp.fontStyle &= ~FontStyles.Bold;
                    isModified = true;
                    modifiedCount++;
                }
            }
        }

        if (isModified)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log($"<color=green>Finished!</color> Modified {modifiedCount} TextMeshPro components in current scene.");
        EditorUtility.DisplayDialog("Complete", $"Modified {modifiedCount} TextMeshPro components in current scene.", "OK");
    }
}

#endif