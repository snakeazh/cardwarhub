using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuideFramework.EditorTools
{
    internal class SceneSwitcherWindow : EditorWindow
    {
        private Vector2 _scroll;

        private struct SceneEntry
        {
            public string Path;
            public string Name;
            public bool Enabled;
            public int Index;
        }

        private SceneEntry[] _entries = new SceneEntry[0];

        [MenuItem("Tools/Scene Switcher %#o")] // Ctrl+Shift+O
        private static void OpenWindow()
        {
            var win = GetWindow<SceneSwitcherWindow>(true, "Scene Switcher");
            win.minSize = new Vector2(360, 240);
            win.Refresh();
            win.Show();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                Refresh();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    GUI.enabled = e.Enabled;
                    if (GUILayout.Button($"[{e.Index}] {e.Name}", GUILayout.Height(28)))
                    {
                        OpenScene(e.Path);
                    }
                    GUI.enabled = true;
                    EditorGUILayout.LabelField(e.Path);
                }
            }

            EditorGUILayout.EndScrollView();

            if (_entries.Length == 0)
            {
                EditorGUILayout.HelpBox("Build Settings 中未配置任何场景。", MessageType.Info);
                if (GUILayout.Button("打开 Build Settings"))
                {
                    EditorApplication.ExecuteMenuItem("File/Build Settings...");
                }
            }
        }

        private void Refresh()
        {
            var scenes = EditorBuildSettings.scenes;
            _entries = new SceneEntry[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                var s = scenes[i];
                _entries[i] = new SceneEntry
                {
                    Path = s.path,
                    Name = Path.GetFileNameWithoutExtension(s.path),
                    Enabled = s.enabled,
                    Index = i
                };
            }
        }

        private void OpenScene(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}