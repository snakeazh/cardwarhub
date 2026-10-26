using UnityEditor;
using UnityEngine;
using UnityEditor.UI;
using UnityEngine.UI;
using FusionContinent.Extensions;

namespace FusionContinent.Extensions
{
    [CustomEditor(typeof(FlippableImage), true)]
    [CanEditMultipleObjects]
    public class FlippableImageEditor : ImageEditor
    {
        private SerializedProperty m_FlipXProp;
        private SerializedProperty m_FlipYProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_FlipXProp = serializedObject.FindProperty("m_FlipX");
            m_FlipYProp = serializedObject.FindProperty("m_FlipY");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Flip Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_FlipXProp, new GUIContent("Flip X"));
            EditorGUILayout.PropertyField(m_FlipYProp, new GUIContent("Flip Y"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            var img = (FlippableImage)target;
            if (img.type == Image.Type.Filled)
            {
                EditorGUILayout.HelpBox("翻转仅镜像贴图，不改变填充方向。如需反向填充可扩展组件。", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("水平翻转"))
            {
                foreach (var t in targets)
                {
                    var fi = (FlippableImage)t;
                    fi.flipX = !fi.flipX;
                    EditorUtility.SetDirty(fi);
                }
            }
            if (GUILayout.Button("垂直翻转"))
            {
                foreach (var t in targets)
                {
                    var fi = (FlippableImage)t;
                    fi.flipY = !fi.flipY;
                    EditorUtility.SetDirty(fi);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        [MenuItem("GameObject/UI/Flippable Image", false, 10)]
        private static void CreateFlippableImage(MenuCommand menuCommand)
        {
            var go = new GameObject("Flippable Image", typeof(RectTransform), typeof(FlippableImage));
            var parent = menuCommand.context as GameObject;
            GameObjectUtility.SetParentAndAlign(go, parent);
            var fi = go.GetComponent<FlippableImage>();
            fi.raycastTarget = true;
            Undo.RegisterCreatedObjectUndo(go, "Create Flippable Image");
            Selection.activeGameObject = go;
        }
    }
}