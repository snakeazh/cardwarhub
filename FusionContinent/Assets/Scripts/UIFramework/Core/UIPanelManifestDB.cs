using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIFramework
{
    [Serializable]
    public class UIPanelManifestEntry
    {
        [Tooltip("界面唯一 Key（例如 CardGroupPanel）")]
        public string PanelKey;

        [Tooltip("该面板依赖的图集资源路径列表")]
        public List<string> AtlasKeys = new List<string>();
    }

    [CreateAssetMenu(fileName = "UIPanelManifestDB", menuName = "UIFramework/UIPanelManifestDB")]
    public class UIPanelManifestDB : ScriptableObject
    {
        [SerializeField]
        private List<string> mCommonAtlasKeys = new List<string>();

        [SerializeField]
        private List<UIPanelManifestEntry> mEntries = new List<UIPanelManifestEntry>();

        private Dictionary<string, UIPanelManifestEntry> mLookup;

        private static UIPanelManifestDB sRuntimeDB;

        public IReadOnlyList<string> CommonAtlasKeys => mCommonAtlasKeys;
        public IReadOnlyList<UIPanelManifestEntry> Entries => mEntries;

        private void OnEnable()
        {
            RebuildCache();
        }

        public void RebuildCache()
        {
            mLookup = new Dictionary<string, UIPanelManifestEntry>(StringComparer.Ordinal);

            for (int i = 0; i < mEntries.Count; i++)
            {
                var entry = mEntries[i];
                if (entry == null)
                {
                    continue;
                }

                AddLookupKey(entry.PanelKey, entry);
            }
        }

        private void AddLookupKey(string key, UIPanelManifestEntry entry)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (mLookup.ContainsKey(key))
            {
                LogTool.LogWarning($"[UIPanelManifestDB] 重复配置面板Key: {key}，将使用最后一次命中。");
            }

            mLookup[key] = entry;
        }

        public bool TryGetByPanelKey(string panelKey, out UIPanelManifestEntry entry)
        {
            if (string.IsNullOrWhiteSpace(panelKey))
            {
                entry = null;
                return false;
            }

            EnsureCacheReady();
            return mLookup.TryGetValue(panelKey, out entry);
        }

        public bool TryGetByPanelType(Type panelType, out UIPanelManifestEntry entry)
        {
            entry = null;
            if (panelType == null)
            {
                return false;
            }

            // 兼容旧调用：Type 查询自动使用 Type.Name 作为 Key。
            return TryGetByPanelKey(panelType.Name, out entry);
        }

        public static void SetRuntimeDB(UIPanelManifestDB db)
        {
            sRuntimeDB = db;
            sRuntimeDB?.RebuildCache();
        }

        public static UIPanelManifestDB GetRuntimeDB()
        {
            return sRuntimeDB;
        }

        public static bool TryGet(string panelKey, out UIPanelManifestEntry entry)
        {
            entry = null;
            var db = GetRuntimeDB();
            return db != null && db.TryGetByPanelKey(panelKey, out entry);
        }

        public static bool TryGet(Type panelType, out UIPanelManifestEntry entry)
        {
            entry = null;
            var db = GetRuntimeDB();
            return db != null && db.TryGetByPanelType(panelType, out entry);
        }

        private void EnsureCacheReady()
        {
            if (mLookup == null)
            {
                RebuildCache();
            }
        }
    }
}
