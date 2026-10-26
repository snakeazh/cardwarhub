using Config;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    /// <summary>
    /// 技能卡 UI 通用刷新（品质边框等）。
    /// </summary>
    public static class SkillCardUiHelper
    {
        /// <summary>
        /// 按品质设置 Canvas/Frame 与 Canvas/framekuang（与 CardSlot 命名一致：{Quality}Card / {Quality}CardFrame）。
        /// </summary>
        public static void ApplyQualityFrame(Transform cardRoot, QualityType qualityType)
        {
            if (cardRoot == null || Global.CarFrameAtlas == null)
            {
                return;
            }

            var qualityName = qualityType.ToString();
            var frame = cardRoot.Find("Canvas/Frame")?.GetComponent<Image>();
            if (frame != null)
            {
                var frameSprite = Global.CarFrameAtlas.GetSprite($"{qualityName}Card");
                if (frameSprite != null)
                {
                    frame.sprite = frameSprite;
                }
                frame.SetNativeSize();
            }

            var frameBorder = cardRoot.Find("Canvas/framekuang")?.GetComponent<Image>();
            if (frameBorder != null)
            {
                var borderSprite = Global.CarFrameAtlas.GetSprite($"{qualityName}CardFrame");
                if (borderSprite != null)
                {
                    frameBorder.sprite = borderSprite;
                }
                frameBorder.SetNativeSize();
            }
        }

        public static void ApplyQualityFrame(BattleCardGroup battleSkill, Transform cardRoot)
        {
            if (battleSkill == null)
            {
                return;
            }

            ApplyQualityFrame(cardRoot, battleSkill.GetQualityType());
        }
    }
}
