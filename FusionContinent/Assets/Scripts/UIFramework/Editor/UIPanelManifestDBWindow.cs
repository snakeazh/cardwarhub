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
    public class UIPanelManifestDBWindow : EditorWindow
    {
        private UIPanelManifestDB mDatabase;
        private SerializedObject mSerializedObject;
        private SerializedProperty mCommonAtlasKeysProperty;
        private SerializedProperty mEntriesProperty;
        private ReorderableList mEntriesList;
        private string mSearchText = string.Empty;
        private readonly Dictionary<string, SpriteAtlas> mAtlasLookup = new Dictionary<string, SpriteAtlas>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> mAtlasNames = new List<string>();
        private bool mAtlasCacheBuilt;
        private bool mCommonFoldout = true;

        [MenuItem("Tools/UIFramework/UIPanel Manifest Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<UIPanelManifestDBWindow>("UIPanel Manifest");
            window.minSize = new Vector2(900f, 540f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += OnProjectChanged;
            if (mDatabase != null)
            {
                BindDatabase(mDatabase);
            }
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

        private void OnGUI()
        {
            try
            {
                DrawToolbar();
                EditorGUILayout.Space();

                if (mDatabase == null)
                {
                    EditorGUILayout.HelpBox("请先选择一个 UIPanelManifestDB 资源。", MessageType.Info);
                    return;
                }

                if (mSerializedObject == null || mEntriesProperty == null)
                {
                    BindDatabase(mDatabase);
                    if (mSerializedObject == null || mEntriesProperty == null)
                    {
                        EditorGUILayout.HelpBox("绑定配置资源失败，请重新选择资源。", MessageType.Error);
                        return;
                    }
                }

                mSerializedObject.Update();
                DrawFilterInfo();
                EditorGUILayout.Space();
                DrawCommonAtlasSection();
                EditorGUILayout.Space();
                mEntriesList?.DoLayoutList();
                EditorGUILayout.Space();
                DrawBottomBar();
                mSerializedObject.ApplyModifiedProperties();
            }
            catch (Exception ex)
            {
                LogTool.LogException(ex);
                EditorGUILayout.HelpBox($"编辑器绘制异常: {ex.Message}", MessageType.Error);
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var selected = (UIPanelManifestDB)EditorGUILayout.ObjectField(
                    mDatabase,
                    typeof(UIPanelManifestDB),
                    false,
                    GUILayout.Width(360f));

                if (selected != mDatabase)
                {
                    BindDatabase(selected);
                }

                GUILayout.Space(8f);
                GUILayout.Label("Search", GUILayout.Width(45f));
                var searchStyle = GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.textField;
                mSearchText = GUILayout.TextField(mSearchText, searchStyle, GUILayout.MinWidth(180f));

                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                {
                    mSearchText = string.Empty;
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Create DB", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    CreateDatabaseAsset();
                }

                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                {
                    if (mDatabase != null)
                    {
                        EditorGUIUtility.PingObject(mDatabase);
                    }
                }
            }
        }

        private void DrawFilterInfo()
        {
            if (string.IsNullOrWhiteSpace(mSearchText))
            {
                EditorGUILayout.HelpBox("显示全部配置项。", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox($"当前过滤关键字: {mSearchText}", MessageType.None);
            }
        }

        private void DrawBottomBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Cache", GUILayout.Height(28f)))
                {
                    mSerializedObject.ApplyModifiedProperties();
                    mDatabase.RebuildCache();
                    EditorUtility.SetDirty(mDatabase);
                    AssetDatabase.SaveAssets();
                    LogTool.Log("[UIPanelManifestDBWindow] 手动重建缓存完成。");
                }

                if (GUILayout.Button("Sort By Key", GUILayout.Height(28f)))
                {
                    SortEntriesByKey();
                }

                if (GUILayout.Button("Save", GUILayout.Height(28f)))
                {
                    mSerializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mDatabase);
                    AssetDatabase.SaveAssets();
                    LogTool.Log("[UIPanelManifestDBWindow] 保存完成。");
                }
            }
        }

        private void BindDatabase(UIPanelManifestDB db)
        {
            mDatabase = db;
            if (mDatabase == null)
            {
                mSerializedObject = null;
                mCommonAtlasKeysProperty = null;
                mEntriesProperty = null;
                mEntriesList = null;
                return;
            }

            mSerializedObject = new SerializedObject(mDatabase);
            mCommonAtlasKeysProperty = mSerializedObject.FindProperty("mCommonAtlasKeys");
            mEntriesProperty = mSerializedObject.FindProperty("mEntries");
            BuildEntriesList();
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

        private void BuildEntriesList()
        {
            mEntriesList = new ReorderableList(mSerializedObject, mEntriesProperty, true, true, true, true);
            mEntriesList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Panel Entries");
            };

            mEntriesList.elementHeightCallback = index =>
            {
                if (mEntriesProperty == null || index < 0 || index >= mEntriesProperty.arraySize)
                {
                    return EditorGUIUtility.singleLineHeight + 6f;
                }

                if (IsFilteredOut(index))
                {
                    return 2f;
                }

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
                if (mEntriesProperty == null || index < 0 || index >= mEntriesProperty.arraySize)
                {
                    return;
                }

                if (IsFilteredOut(index))
                {
                    return;
                }

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
                    mSerializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mDatabase);
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
                        mSerializedObject.ApplyModifiedProperties();
                        Repaint();
                    });
                }

                EditorGUI.BeginChangeCheck();
                var input = EditorGUI.TextField(keyRect, keyProp.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    keyProp.stringValue = NormalizeAtlasKey(input);
                    mSerializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mDatabase);
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

        private bool IsFilteredOut(int index)
        {
            if (string.IsNullOrWhiteSpace(mSearchText))
            {
                return false;
            }

            if (mEntriesProperty == null || index < 0 || index >= mEntriesProperty.arraySize)
            {
                return false;
            }

            var element = mEntriesProperty.GetArrayElementAtIndex(index);
            var panelKey = element.FindPropertyRelative("PanelKey").stringValue ?? string.Empty;

            return panelKey.IndexOf(mSearchText, StringComparison.OrdinalIgnoreCase) < 0;
        }

        private void SortEntriesByKey()
        {
            mSerializedObject.ApplyModifiedProperties();
            Undo.RecordObject(mDatabase, "Sort UIPanelManifest Entries By Key");

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

            mSerializedObject.ApplyModifiedProperties();
            mDatabase.RebuildCache();
            EditorUtility.SetDirty(mDatabase);
            AssetDatabase.SaveAssets();
            LogTool.Log("[UIPanelManifestDBWindow] 已按 Key 排序。");
        }

        private static float GetAtlasKeysDrawHeight(SerializedProperty atlasKeysProp)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float rows = Mathf.Max(1, atlasKeysProp != null ? atlasKeysProp.arraySize : 0);
            return line + spacing + rows * (line + spacing) + 2f;
        }

        private void CreateDatabaseAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create UIPanelManifestDB",
                "UIPanelManifestDB",
                "asset",
                "Select location for new UIPanelManifestDB asset.");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var asset = CreateInstance<UIPanelManifestDB>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            BindDatabase(asset);
            EditorGUIUtility.PingObject(asset);
            LogTool.Log($"[UIPanelManifestDBWindow] 创建配置成功: {path}");
        }
    }
}
#endif
