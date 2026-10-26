#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FusionContinent.SpineBake;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace FusionContinent.Monster.Animation.Editor
{
    public class SpineMeshBakeWindow : EditorWindow
    {
        private SkeletonDataAsset _skeletonDataAsset;
        private SkeletonAnimation _previewSkeleton;
        private float _bakeFps = 30f;
        private string _outputFolder = "Assets/BakeMesh";

        [MenuItem("Tools/Spine/Spine Mesh Bake Window")]
        public static void Open()
        {
            GetWindow<SpineMeshBakeWindow>("Spine Mesh Bake");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Spine BakeMesh 烘焙", EditorStyles.boldLabel);
            _skeletonDataAsset = (SkeletonDataAsset)EditorGUILayout.ObjectField(
                "SkeletonDataAsset", _skeletonDataAsset, typeof(SkeletonDataAsset), false);
            _previewSkeleton = (SkeletonAnimation)EditorGUILayout.ObjectField(
                "Preview SkeletonAnimation", _previewSkeleton, typeof(SkeletonAnimation), true);
            _bakeFps = EditorGUILayout.FloatField("Bake FPS", _bakeFps);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.Space();
            if (GUILayout.Button("Bake"))
            {
                BakeSelected();
            }

            EditorGUILayout.HelpBox(
                "输出: Assets/BakeMesh/{SkeletonName}_Frame.bytes\n"
                + "运行时 BakeMeshAnimationPlayer 通过 SkeletonName 加载。",
                MessageType.Info);
        }

        private void BakeSelected()
        {
            SkeletonAnimation skeletonAnimation = ResolveSkeletonAnimation();
            if (skeletonAnimation == null)
            {
                EditorUtility.DisplayDialog("Spine Mesh Bake", "请指定 SkeletonDataAsset 或 SkeletonAnimation", "OK");
                return;
            }

            string skeletonName = skeletonAnimation.gameObject.name;
            if (_skeletonDataAsset != null)
            {
                skeletonName = _skeletonDataAsset.name.Replace("_SkeletonData", string.Empty);
            }

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            SpineMeshFrameData data = MonsterMeshFrameBaker.Bake(skeletonAnimation, _bakeFps);
            string outputPath = Path.Combine(_outputFolder, skeletonName + "_Frame.bytes");
            File.WriteAllBytes(outputPath, data.WriteBinary());
            LogTool.Log("[SpineMeshBake] 优化报告:\n" + data.BuildOptimizationReport());

            if (_previewSkeleton != null
                && _previewSkeleton.gameObject.name == "BakeMeshTemp")
            {
                Object.DestroyImmediate(_previewSkeleton.gameObject);
                _previewSkeleton = null;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Spine Mesh Bake", "烘焙完成:\n" + outputPath, "OK");
        }

        private SkeletonAnimation ResolveSkeletonAnimation()
        {
            if (_previewSkeleton != null)
            {
                return _previewSkeleton;
            }

            if (_skeletonDataAsset == null)
            {
                return null;
            }

            GameObject temp = EditorUtility.CreateGameObjectWithHideFlags(
                "BakeMeshTemp",
                HideFlags.HideAndDontSave,
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(SkeletonAnimation));
            SkeletonAnimation anim = temp.GetComponent<SkeletonAnimation>();
            anim.skeletonDataAsset = _skeletonDataAsset;
            anim.Initialize(true);
            _previewSkeleton = anim;
            return anim;
        }
    }

    public static class MonsterMeshFrameBaker
    {
        public static SpineMeshFrameData Bake(SkeletonAnimation skeletonAnimation, float frameRate)
        {
            float frameLength = frameRate > 0f ? 1f / frameRate : SpineMeshFrameData.DefaultFrameLength;
            var data = new SpineMeshFrameData { FrameLength = frameLength };

            MeshFilter meshFilter = skeletonAnimation.GetComponent<MeshFilter>();
            skeletonAnimation.Initialize(true);

            ExposedList<Spine.Animation> animations = skeletonAnimation.skeleton.Data.Animations;
            for (int i = 0; i < animations.Count; i++)
            {
                BakeAction(skeletonAnimation, meshFilter, data, animations.Items[i], frameLength);
            }

            return data;
        }

        private static void BakeAction(
            SkeletonAnimation skeletonAnimation,
            MeshFilter meshFilter,
            SpineMeshFrameData data,
            Spine.Animation action,
            float frameLength)
        {
            var tempFrames = new List<SpineMeshFrame>();
            var tempEvents = new List<SpineMeshFrameEvent>();
            var firedEvents = new List<(int frame, string name)>();

            Spine.AnimationState.TrackEntryEventDelegate onEvent = (entry, e) =>
            {
                firedEvents.Add((tempFrames.Count, e.Data.Name));
            };

            skeletonAnimation.AnimationState.Event += onEvent;
            skeletonAnimation.ClearState();
            skeletonAnimation.AnimationState.SetAnimation(0, action.Name, false);

            float duration = action.Duration;
            float time = 0f;
            tempFrames.Clear();
            firedEvents.Clear();

            while (time < duration)
            {
                time += frameLength;
                skeletonAnimation.Update(frameLength);
                skeletonAnimation.LateUpdate();
                tempFrames.Add(BakeOneMesh(meshFilter.sharedMesh));
            }

            skeletonAnimation.AnimationState.Event -= onEvent;

            var tempEventArray = new SpineMeshFrameEvent[firedEvents.Count];
            for (int e = 0; e < firedEvents.Count; e++)
            {
                tempEventArray[e] = new SpineMeshFrameEvent
                {
                    FrameIndex = firedEvents[e].frame,
                    EventName = firedEvents[e].name
                };
            }

            var actionFrames = new SpineMeshActionFrames
            {
                ActionName = action.Name,
                Frames = tempFrames.ToArray(),
                Events = tempEventArray
            };
            SpineMeshFrameData.BuildCompactFrames(actionFrames);
            data.ActionDic[action.Name] = actionFrames;
        }

        private static SpineMeshFrame BakeOneMesh(Mesh source)
        {
            var frame = new SpineMeshFrame
            {
                Vertices = source.vertices,
                Uvs = source.uv,
                Colors = source.colors32,
                SubMeshCount = (short)source.subMeshCount,
                Bounds = source.bounds,
                Triangles = new int[source.subMeshCount][]
            };

            for (int i = 0; i < source.subMeshCount; i++)
            {
                frame.Triangles[i] = source.GetTriangles(i);
            }

            return frame;
        }
    }
}
#endif
