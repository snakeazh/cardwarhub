#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.U2D;

namespace UIFramework.Editor
{
    [CustomEditor(typeof(UIPanelManifestDB))]
    public class UIPanelManifestDBEditor : UnityEditor.Editor
    {
        private SerializedProperty mCommonAtlasKeysProperty;
        private SerializedProperty mEntriesProperty;
        private ReorderableList mEntriesList;
        private readonly Dictionary<string, SpriteAtlas> mAtlasLookup = new Dictionary<string, SpriteAtlas>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> mAtlasNames = new List<string>();
        private bool mAtlasCacheBuilt;
        private bool mCommonFoldout = true;

        private void OnEnable()
        {
            EditorApplication.projectChanged += OnProjectChanged;
            mCommonAtlasKeysProperty = serializedObject.FindProperty("mCommonAtlasKeys");
            mEntriesProperty = serializedObject.FindProperty("mEntries");

            mEntriesList = new ReorderableList(serializedObject, mEntriesProperty, true, true, true, true);
            mEntriesList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Panel Manifest Entries");
            };

            mEntriesList.elementHeightCallback = index =>
            {
                var element = mEntriesProperty.GetArrayElementAtIndex(index);
                float line = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;

                if (!element.isExpanded)
                {
                    return line + 8f;
                }

                var atlasKeys = element.FindPropertyRelative("AtlasKeys");
                float atlasRows = Mathf.Max(1, atlasKeys.arraySize);
                return 12f + line + spacing + line + spacing + (atlasRows * (line + spacing)) + line + 4f;
            };

            mEntriesList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = mEntriesProperty.GetArrayElementAtIndex(index);

                var panelKeyProp = element.FindPropertyRelative("PanelKey");
                var atlasKeysProp = element.FindPropertyRelative("AtlasKeys");

                float line = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;

                Rect rowRect = new Rect(rect.x, rect.y + 2f, rect.width, line);
                string header = string.IsNullOrWhiteSpace(panelKeyProp.stringValue)
                    ? $"Entry {index}"
                    : panelKeyProp.stringValue;

                element.isExpanded = EditorGUI.Foldout(rowRect, element.isExpanded, header, true);
                if (!element.isExpanded)
                {
                    return;
                }

                rowRect.y += line + spacing;
                EditorGUI.PropertyField(rowRect, panelKeyProp, new GUIContent("PanelKey"));

                rowRect.y += line + spacing;
                DrawAtlasKeys(rowRect, atlasKeysProp);
            };
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnProjectChanged()
        {
            mAtlasCacheBuilt = false;
            mAtlasLookup.Clear();
            mAtlasNames.Clear();
        }

        private void DrawAtlasKeys(Rect startRect, SerializedProperty atlasKeysProp)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            var headerRect = new Rect(startRect.x, startRect.y, startRect.width, line);
            EditorGUI.LabelField(headerRect, "AtlasKeys");

            var addRect = new Rect(startRect.x + startRect.width - 54f, startRect.y, 54f, line);
            if (GUI.Button(addRect, "+ Atlas"))
            {
                atlasKeysProp.InsertArrayElementAtIndex(atlasKeysProp.arraySize);
                atlasKeysProp.GetArrayElementAtIndex(atlasKeysProp.arraySize - 1).stringValue = string.Empty;
            }

            float y = startRect.y + line + spacing;
            if (atlasKeysProp.arraySize == 0)
            {
                EditorGUI.LabelField(new Rect(startRect.x + 16f, y, startRect.width - 16f, line), "(empty)");
                return;
            }

            for (int i = 0; i < atlasKeysProp.arraySize; i++)
            {
                var keyProp = atlasKeysProp.GetArrayElementAtIndex(i);
                var key = NormalizeAtlasKey(keyProp.stringValue ?? string.Empty);
                var currentAtlas = ResolveAtlasByName(key);

                var objectRect = new Rect(startRect.x, y, startRect.width * 0.5f, line);
                var pickRect = new Rect(objectRect.xMax + 4f, y, 52f, line);
                var keyRect = new Rect(pickRect.xMax + 4f, y, startRect.width - objectRect.width - 84f, line);
                var removeRect = new Rect(startRect.x + startRect.width - 20f, y, 20f, line);

                EditorGUI.BeginChangeCheck();
                var selectedAtlas = (SpriteAtlas)EditorGUI.ObjectField(objectRect, currentAtlas, typeof(SpriteAtlas), false);
                if (EditorGUI.EndChangeCheck())
                {
                    keyProp.stringValue = selectedAtlas != null ? selectedAtlas.name : string.Empty;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }

                if (currentAtlas == null && !string.IsNullOrWhiteSpace(key))
                {
                    var warnRect = new Rect(objectRect.x, objectRect.y, objectRect.width, line);
                    EditorGUI.LabelField(warnRect, "Unresolved", EditorStyles.miniBoldLabel);
                }

                if (GUI.Button(pickRect, "Pick"))
                {
                    ShowAtlasPickMenu(selectedName =>
                    {
                        keyProp.stringValue = selectedName;
                        serializedObject.ApplyModifiedProperties();
                        Repaint();
                    });
                }

                EditorGUI.BeginChangeCheck();
                var input = EditorGUI.TextField(keyRect, keyProp.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    keyProp.stringValue = NormalizeAtlasKey(input);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }

                if (GUI.Button(removeRect, "-"))
                {
                    atlasKeysProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                y += line + spacing;
            }
        }

        private void EnsureAtlasCache()
        {
            if (mAtlasCacheBuilt)
            {
                return;
            }

            mAtlasLookup.Clear();
            mAtlasNames.Clear();
            var guids = AssetDatabase.FindAssets("t:SpriteAtlas");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas == null || string.IsNullOrEmpty(atlas.name))
                {
                    continue;
                }

                if (!mAtlasNames.Contains(atlas.name))
                {
                    mAtlasNames.Add(atlas.name);
                }

                AddAtlasLookupKey(atlas.name, atlas);
                AddAtlasLookupKey(Path.GetFileNameWithoutExtension(path), atlas);
                AddAtlasLookupKey(path, atlas);
            }

            mAtlasNames.Sort(StringComparer.OrdinalIgnoreCase);
            mAtlasCacheBuilt = true;
        }

        private SpriteAtlas ResolveAtlasByName(string atlasName)
        {
            if (string.IsNullOrWhiteSpace(atlasName))
            {
                return null;
            }

            EnsureAtlasCache();
            if (mAtlasLookup.TryGetValue(atlasName, out var atlas))
            {
                return atlas;
            }

            var normalized = NormalizeAtlasKey(atlasName);
            if (mAtlasLookup.TryGetValue(normalized, out atlas))
            {
                return atlas;
            }

            var guids = AssetDatabase.FindAssets($"{normalized} t:SpriteAtlas");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas != null)
                {
                    AddAtlasLookupKey(atlasName, atlas);
                    AddAtlasLookupKey(normalized, atlas);
                }
            }

            return atlas;
        }

        private void AddAtlasLookupKey(string key, SpriteAtlas atlas)
        {
            if (atlas == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!mAtlasLookup.ContainsKey(key))
            {
                mAtlasLookup.Add(key, atlas);
            }
        }

        private void ShowAtlasPickMenu(Action<string> onSelect)
        {
            EnsureAtlasCache();
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("(None)"), false, () => onSelect?.Invoke(string.Empty));

            for (int i = 0; i < mAtlasNames.Count; i++)
            {
                string name = mAtlasNames[i];
                menu.AddItem(new GUIContent(name), false, () => onSelect?.Invoke(name));
            }

            menu.ShowAsContext();
        }

        private static string NormalizeAtlasKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            key = key.Trim();
            if (key.Contains("/") || key.Contains("\\"))
            {
                return Path.GetFileNameWithoutExtension(key);
            }

            return key;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("配置面板与图集依赖映射。每个条目只需要一个 PanelKey。", MessageType.Info);
            EditorGUILayout.Space();

            DrawCommonAtlasSection();
            EditorGUILayout.Space();

            mEntriesList.DoLayoutList();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild Cache", GUILayout.Height(28f)))
            {
                serializedObject.ApplyModifiedProperties();
                var db = (UIPanelManifestDB)target;
                db.RebuildCache();
                EditorUtility.SetDirty(db);
                LogTool.Log("[UIPanelManifestDBEditor] 手动重建缓存完成。");
            }

            if (GUILayout.Button("Sort By Key", GUILayout.Height(28f)))
            {
                SortEntriesByKey();
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCommonAtlasSection()
        {
            if (mCommonAtlasKeysProperty == null)
            {
                return;
            }

            mCommonFoldout = EditorGUILayout.Foldout(mCommonFoldout, "CommonAtlasKeys (初始化通用图集)", true);
            if (!mCommonFoldout)
            {
                return;
            }

            float height = GetAtlasKeysDrawHeight(mCommonAtlasKeysProperty);
            Rect sectionRect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            DrawAtlasKeys(sectionRect, mCommonAtlasKeysProperty);
        }

        private void SortEntriesByKey()
        {
            serializedObject.ApplyModifiedProperties();
            var db = (UIPanelManifestDB)target;
            if (db == null || db.Entries == null)
            {
                return;
            }

            Undo.RecordObject(db, "Sort UIPanelManifest Entries By Key");

            // 借助 SerializedProperty 直接交换，避免反射或改动运行时 API。
            for (int i = 0; i < mEntriesProperty.arraySize - 1; i++)
            {
                for (int j = i + 1; j < mEntriesProperty.arraySize; j++)
                {
                    var left = mEntriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("PanelKey").stringValue;
                    var right = mEntriesProperty.GetArrayElementAtIndex(j).FindPropertyRelative("PanelKey").stringValue;

                    if (string.Compare(left, right, StringComparison.Ordinal) > 0)
                    {
                        mEntriesProperty.MoveArrayElement(j, i);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            db.RebuildCache();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            LogTool.Log("[UIPanelManifestDBEditor] 已按 Key 排序。");
        }

        private static float GetAtlasKeysDrawHeight(SerializedProperty atlasKeysProp)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float rows = Mathf.Max(1, atlasKeysProp != null ? atlasKeysProp.arraySize : 0);
            return line + spacing + rows * (line + spacing) + 2f;
        }
    }
}
#endif
