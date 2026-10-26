#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.RedDot.Editor
{
    public static class RedNodeTypeEditorLabels
    {
        static Dictionary<RedNodeType, string> _comments;
        static GUIContent[] _popupContents;
        static RedNodeType[] _popupValues;

        public static string GetComment(RedNodeType type)
        {
            EnsureLoaded();
            return _comments.TryGetValue(type, out string comment) ? comment : string.Empty;
        }

        public static string GetDisplayLabel(RedNodeType type)
        {
            string comment = GetComment(type);
            return string.IsNullOrEmpty(comment) ? type.ToString() : $"{type} — {comment}";
        }

        public static void Reload()
        {
            _comments = null;
            _popupContents = null;
            _popupValues = null;
            EnsureLoaded();
        }

        public static RedNodeType DrawEnumPopup(Rect rect, GUIContent label, RedNodeType value)
        {
            EnsurePopupCache();
            int selected = IndexOf(value);
            int newIndex = EditorGUI.Popup(rect, label, selected, _popupContents);
            return _popupValues[newIndex];
        }

        public static RedNodeType DrawEnumPopupLayout(GUIContent label, RedNodeType value)
        {
            EnsurePopupCache();
            int selected = IndexOf(value);
            int newIndex = EditorGUILayout.Popup(label, selected, _popupContents);
            return _popupValues[newIndex];
        }

        public static void DrawCommentLabel(RedNodeType type)
        {
            string comment = GetComment(type);
            if (string.IsNullOrEmpty(comment))
            {
                return;
            }

            EditorGUILayout.LabelField(comment, EditorStyles.miniLabel);
        }

        static int IndexOf(RedNodeType value)
        {
            for (int i = 0; i < _popupValues.Length; i++)
            {
                if (_popupValues[i].Equals(value))
                {
                    return i;
                }
            }

            return 0;
        }

        static void EnsurePopupCache()
        {
            EnsureLoaded();
            if (_popupContents != null)
            {
                return;
            }

            Array values = Enum.GetValues(typeof(RedNodeType));
            _popupValues = new RedNodeType[values.Length];
            _popupContents = new GUIContent[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                RedNodeType type = (RedNodeType)values.GetValue(i);
                _popupValues[i] = type;
                string comment = GetComment(type);
                _popupContents[i] = new GUIContent(
                    GetDisplayLabel(type),
                    string.IsNullOrEmpty(comment) ? type.ToString() : comment);
            }
        }

        static void EnsureLoaded()
        {
            if (_comments != null)
            {
                return;
            }

            _comments = new Dictionary<RedNodeType, string>();
            string path = FindRedNodeTypeScriptPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            ParseComments(File.ReadAllText(path));
        }

        static string FindRedNodeTypeScriptPath()
        {
            string[] guids = AssetDatabase.FindAssets("RedNodeType t:Script");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith("Game/RedDot/RedNodeType.cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith("Game\\RedDot\\RedNodeType.cs", StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
        }

        static void ParseComments(string source)
        {
            string[] lines = source.Split('\n');
            bool inSummary = false;
            string pendingComment = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimEnd('\r').Trim();
                if (trimmed == "/// <summary>")
                {
                    inSummary = true;
                    pendingComment = null;
                    continue;
                }

                if (trimmed == "/// </summary>")
                {
                    inSummary = false;
                    continue;
                }

                if (inSummary && trimmed.StartsWith("/// ", System.StringComparison.Ordinal))
                {
                    pendingComment = trimmed.Substring(4).Trim();
                    continue;
                }

                int eqIndex = trimmed.IndexOf('=');
                if (eqIndex <= 0 || string.IsNullOrEmpty(pendingComment))
                {
                    continue;
                }

                string name = trimmed.Substring(0, eqIndex).Trim();
                if (Enum.TryParse(name, out RedNodeType type))
                {
                    _comments[type] = pendingComment;
                }

                pendingComment = null;
            }
        }
    }
}
#endif
