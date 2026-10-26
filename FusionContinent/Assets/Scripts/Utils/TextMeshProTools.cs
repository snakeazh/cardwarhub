using System.Collections.Generic;
using FusionContinent;
using TMPro;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 正文档位（基准 Font Size = 36，通过 TextStyle 百分比达到视觉字号）。
    /// </summary>
    public enum GameFontSize
    {
        /// <summary>视觉 30，对应 Style Content30</summary>
        S = 30,
        /// <summary>视觉 36，对应 Style Content36</summary>
        M = 36,
        /// <summary>视觉 48，对应 Style Content48</summary>
        L = 48,
    }

    /// <summary>
    /// TextMeshPro 运行时样式工具（颜色、描边、字号等统一放这里）。
    /// 约定：普通文本 Font Size=36 + Content TextStyle；标题 Font Size=60 + TitleDialog60 + GameTitleFont。
    /// </summary>
    public static class TextMeshProTools
    {
        public const float BodyBaseFontSize = 36f;
        public const float TitleBaseFontSize = 60f;

        public const string StyleContent30 = "Content30";
        public const string StyleContent36 = "Content36";
        public const string StyleContent48 = "Content48";
        public const string StyleTitle60 = "TitleDialog60";

        private const string TitleFontAddress = "Fonts_GameTitleFont";

        private static TMP_FontAsset sCachedTitleFont;

        public static void SetUnderlayColor(TMP_Text textMeshProUGUI, Color32 color)
        {
            if (textMeshProUGUI == null)
            {
                return;
            }

            textMeshProUGUI.fontMaterial.SetColor("_UnderlayColor", color);
            textMeshProUGUI.UpdateMeshPadding();
        }

        /// <summary>
        /// 设置垂直渐变色：topRGB 顶部，bottomRGB 底部（RGB 0-255 列表）。
        /// </summary>
        public static void SetTMPVerticalGradient(TMP_Text tmpText, List<int> topRGB, List<int> bottomRGB)
        {
            if (tmpText == null)
            {
                return;
            }

            bool hasTop = topRGB != null && topRGB.Count >= 3;
            bool hasBottom = bottomRGB != null && bottomRGB.Count >= 3;

            Color32 topColor = hasTop
                ? new Color32((byte)topRGB[0], (byte)topRGB[1], (byte)topRGB[2], 255)
                : new Color32(255, 255, 255, 255);

            Color32 bottomColor = hasBottom
                ? new Color32((byte)bottomRGB[0], (byte)bottomRGB[1], (byte)bottomRGB[2], 255)
                : topColor;

            tmpText.enableVertexGradient = true;
            tmpText.colorGradient = new VertexGradient(
                topColor,
                topColor,
                bottomColor,
                bottomColor);
            tmpText.SetVerticesDirty();
        }

        /// <summary>
        /// 应用 TextStyleSheet 中的样式（优先组件 styleSheet，否则 TMP 默认表）。
        /// </summary>
        public static void ApplyTextStyle(TMP_Text tmpText, string styleName)
        {
            if (tmpText == null || string.IsNullOrEmpty(styleName))
            {
                return;
            }

            TMP_Style style = null;
            if (tmpText.styleSheet != null)
            {
                style = tmpText.styleSheet.GetStyle(styleName);
            }

            if (style == null && TMP_Settings.defaultStyleSheet != null)
            {
                style = TMP_Settings.defaultStyleSheet.GetStyle(styleName);
            }

            if (style == null)
            {
                LogTool.LogWarning($"[TextMeshProTools] 未找到 TextStyle: {styleName}");
                return;
            }

            tmpText.textStyle = style;
        }

        /// <summary>
        /// 正文三档：基准字号 36 + Content30/36/48。
        /// </summary>
        public static void SetFontSize(TMP_Text tmpText, GameFontSize size)
        {
            if (tmpText == null)
            {
                return;
            }

            tmpText.fontSize = BodyBaseFontSize;
            ApplyTextStyle(tmpText, GetBodyStyleName(size));
        }

        /// <summary>
        /// 预缓存固定标题字体（可选，启动时调用可避免首次卡顿）。
        /// </summary>
        public static void SetTitleFontAsset(TMP_FontAsset titleFont)
        {
            sCachedTitleFont = titleFont;
        }

        /// <summary>
        /// 固定大标题：GameTitleFont + 基准字号 60 + TitleDialog60。
        /// </summary>
        public static void ApplyTitleFont60(TMP_Text tmpText)
        {
            if (tmpText == null)
            {
                return;
            }

            tmpText.fontSize = TitleBaseFontSize;
            ApplyTextStyle(tmpText, StyleTitle60);

            TMP_FontAsset titleFont = GetOrLoadTitleFont();
            if (titleFont == null)
            {
                LogTool.LogWarning(
                    "[TextMeshProTools] 未加载到 GameTitleFont，已应用 TitleDialog60（基准 60）。请确认已生成并收集 Fonts_GameTitleFont。");
                return;
            }

            tmpText.font = titleFont;
            tmpText.fontSharedMaterial = titleFont.material;
        }

        private static string GetBodyStyleName(GameFontSize size)
        {
            switch (size)
            {
                case GameFontSize.S:
                    return StyleContent30;
                case GameFontSize.L:
                    return StyleContent48;
                default:
                    return StyleContent36;
            }
        }

        private static TMP_FontAsset GetOrLoadTitleFont()
        {
            if (sCachedTitleFont != null)
            {
                return sCachedTitleFont;
            }

            if (Global.ResLoader == null)
            {
                return null;
            }

            sCachedTitleFont = Global.ResLoader.TryLoadSync<TMP_FontAsset>(TitleFontAddress);
            return sCachedTitleFont;
        }
    }
}
