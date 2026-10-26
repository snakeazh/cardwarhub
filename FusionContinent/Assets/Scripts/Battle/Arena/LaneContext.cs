using Config;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 单侧战场挂点与移动参数。
    /// </summary>
    public class LaneContext
    {
        public BattleSide Side;
        public MainCity Wall;
        public Transform MonsterPos;
        public Transform OrdinaryMonster;
        public Transform EliteMonster;
        public Transform BossMonster;
        public Transform FriendlySlot;
        public Transform Bullet;
        public Transform GenerateEffect;
        public RectTransform DamageParent;
        public RectTransform HeavyBlowPos;

        /// <summary>直行阶段世界位移方向，玩家向下 -1，对侧向上 +1。</summary>
        public float MoveDirectionY = -1f;

        /// <summary>开始朝士兵移动的 Y 阈值。</summary>
        public float EngageDepthY = -3f;

        public Transform GetMonsterParent(MonsterQuality quality)
        {
            switch (quality)
            {
                case MonsterQuality.Elite:
                    return EliteMonster;
                case MonsterQuality.Boss:
                    return BossMonster;
                default:
                    return OrdinaryMonster;
            }
        }

        public static LaneContext FromSerializedPlayer(
            MainCity wall,
            Transform monsterPos,
            Transform ordinaryMonster,
            Transform eliteMonster,
            Transform bossMonster,
            Transform friendlySlot,
            Transform bullet,
            Transform generateEffect,
            RectTransform damageParent,
            RectTransform heavyBlowPos)
        {
            return new LaneContext
            {
                Side = BattleSide.Player,
                Wall = wall,
                MonsterPos = monsterPos,
                OrdinaryMonster = ordinaryMonster,
                EliteMonster = eliteMonster,
                BossMonster = bossMonster,
                FriendlySlot = friendlySlot,
                Bullet = bullet,
                GenerateEffect = generateEffect,
                DamageParent = damageParent,
                HeavyBlowPos = heavyBlowPos,
                MoveDirectionY = -1f,
                EngageDepthY = -3f,
            };
        }

        public static LaneContext FromArenaRoots(Transform landSideRoot, Transform uiSideRoot, BattleSide side)
        {
            if (landSideRoot == null)
            {
                LogTool.LogError($"[LaneContext] landSideRoot 为空 side={side}");
                return null;
            }

            var wallTransform = landSideRoot.Find("wall");
            var ctx = new LaneContext
            {
                Side = side,
                Wall = wallTransform != null ? wallTransform.GetComponent<MainCity>() : null,
                MonsterPos = landSideRoot.Find("MonsterPos"),
                OrdinaryMonster = landSideRoot.Find("OrdinaryMonster"),
                EliteMonster = landSideRoot.Find("EliteMonster"),
                BossMonster = landSideRoot.Find("BossMonster"),
                Bullet = landSideRoot.Find("Bullet"),
                GenerateEffect = landSideRoot.Find("GenerateEffect"),
                DamageParent = uiSideRoot != null ? uiSideRoot.Find("DamageParent") as RectTransform : null,
                HeavyBlowPos = landSideRoot.Find("HeavyBlowPos") as RectTransform,
            };

            // 移动方向按当前侧布局推导：墙在出生点下方则怪向下走（-1），反之向上（+1）。
            // 当前场景 pvp 根为横向镜像（与 pve 同向 Y 布局），对侧同样向下朝自己的墙移动。
            if (ctx.MonsterPos != null && ctx.Wall != null)
            {
                ctx.MoveDirectionY = ctx.Wall.transform.position.y < ctx.MonsterPos.position.y ? -1f : 1f;
            }

            ctx.EngageDepthY = ctx.MoveDirectionY < 0f ? -3f : 3f;

            // FriendlySlot 直接挂在 pve/pvp 根下（场景已移除旧 Canvas 包装节点）
            ctx.FriendlySlot = landSideRoot.Find("FriendlySlot");
            if (ctx.FriendlySlot == null)
            {
                ctx.FriendlySlot = FindDeepChild(landSideRoot, "FriendlySlot");
            }

            if (ctx.FriendlySlot == null)
            {
                LogTool.LogError($"[LaneContext] 缺少 FriendlySlot side={side} root={landSideRoot.name}");
            }
            else if (side == BattleSide.Opponent)
            {
                LogTool.Log($"[LaneContext] Opponent FriendlySlot={ctx.FriendlySlot.name} under={landSideRoot.name}");
            }

            if (uiSideRoot != null)
            {
                ctx.DamageParent = uiSideRoot.Find("DamageParent") as RectTransform;
            }

            if (ctx.MonsterPos == null || ctx.OrdinaryMonster == null)
            {
                LogTool.LogError($"[LaneContext] 对侧挂点不完整 side={side} root={landSideRoot.name}");
            }

            return ctx;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
