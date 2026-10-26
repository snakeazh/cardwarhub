using System.Text;
using UIFramework;
using UnityEditor;

/// <summary>
/// 图集调试工具：打印当前已加载图集（引用计数 / 常驻 pin），验证 UIAtlasPreloadTable 瘦身效果。
/// 仅 Play 模式下有意义。
/// </summary>
public static class AtlasDebugTool
{
    [MenuItem("Tools/图集/打印当前已加载图集")]
    private static void DumpLoadedAtlases()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("已加载图集", "请先进入 Play 模式再查看。", "确定");
            return;
        }

        var snapshot = AtlasManager.Instance.GetDebugSnapshot();
        var sb = new StringBuilder();
        sb.AppendLine($"[AtlasDebug] 已加载图集 {snapshot.Count} 个:");
        foreach (var (key, refCount, pinned) in snapshot)
        {
            sb.AppendLine($"  {key}  ref={refCount}{(pinned ? "  [常驻]" : "")}");
        }
        LogTool.Log(sb.ToString());
    }
}
