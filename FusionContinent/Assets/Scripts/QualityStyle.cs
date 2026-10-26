using UnityEngine;

/// <summary>
/// 品质展示样式（通过 GlobalFunc.GetQualityStyle 获取）。
/// 目前只包含主颜色，后续可扩展描边颜色、渐变颜色、特效路径等字段。
/// </summary>
public struct QualityStyle
{
    /// <summary>品质主颜色（来自 TbGameBattleConst.QualityTypeColors 配置）</summary>
    public Color32 MainColor;
}
