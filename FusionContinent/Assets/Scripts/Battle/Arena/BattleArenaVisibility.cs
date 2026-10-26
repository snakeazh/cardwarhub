using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 按 BattleMode 切换 GameMap / GameLand / GameLandUI 下 pve、pvp 子树显隐。
    /// </summary>
    public static class BattleArenaVisibility
    {
        public const string PveName = "pve";
        public const string PvpName = "pvp";

        /// <summary>
        /// 在 arena 根下取玩家侧（pve）子路径。节点已迁入 pve/pvp 后，禁止再对根直接 Find 旧路径。
        /// </summary>
        public static Transform FindUnderPve(Transform arenaRoot, string relativePath)
        {
            if (arenaRoot == null)
            {
                return null;
            }

            var pve = arenaRoot.Find(PveName);
            if (pve == null)
            {
                LogTool.LogError($"[BattleArenaVisibility] {arenaRoot.name} 下缺少 pve 子节点，无法解析 {relativePath}");
                return null;
            }

            var found = pve.Find(relativePath);
            if (found == null)
            {
                LogTool.LogError($"[BattleArenaVisibility] 找不到 {arenaRoot.name}/pve/{relativePath}");
            }

            return found;
        }

        public static void Apply(BattleMode mode)
        {
            bool showPvp = mode == BattleMode.PvpAi;

            ApplyRootPair(GameMap.Default != null ? GameMap.Default.transform : null, showPvp);
            ApplyRootPair(GameLand.Default != null ? GameLand.Default.transform : null, showPvp);
            ApplyRootPair(GameLandUI.Default != null ? GameLandUI.Default.transform : null, showPvp);

            if (showPvp)
            {
                HideOpponentHudOnlyControls(GameLandUI.Default != null ? GameLandUI.Default.transform : null);
            }

            LogTool.Log($"[BattleArenaVisibility] mode={mode} showPvp={showPvp}");
        }

        private static void ApplyRootPair(Transform root, bool showPvp)
        {
            if (root == null)
            {
                return;
            }

            var pve = root.Find(PveName);
            var pvp = root.Find(PvpName);
            if (pve != null)
            {
                pve.gameObject.SetActive(true);
            }

            if (pvp != null)
            {
                pvp.gameObject.SetActive(showPvp);
            }
            else if (showPvp)
            {
                LogTool.LogError($"[BattleArenaVisibility] {root.name} 下缺少 pvp 子节点");
            }
        }

        /// <summary>
        /// 对侧 UI 若整树复制了技能/流派等，强制隐藏避免误操作。
        /// </summary>
        private static void HideOpponentHudOnlyControls(Transform gameLandUiRoot)
        {
            if (gameLandUiRoot == null)
            {
                return;
            }

            var pvp = gameLandUiRoot.Find(PvpName);
            if (pvp == null)
            {
                return;
            }

            HideChild(pvp, "SkillRect");
            HideChild(pvp, "Schools");
            HideChild(pvp, "CitySkillCards");
            HideChild(pvp, "MainCitySkill");
            HideChild(pvp, "PassiveCard");
            HideChild(pvp, "PassiveCardDetail");
            HideChild(pvp, "SurpriseCards");
            // 对侧只需飘字/血条等反馈节点；若整树误复制了 SkillRect，深搜再关一次
            HideDeepChild(pvp, "SkillRect");
        }

        private static void HideDeepChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                {
                    child.gameObject.SetActive(false);
                }

                HideDeepChild(child, childName);
            }
        }

        private static void HideChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
