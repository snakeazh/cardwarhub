using Cysharp.Threading.Tasks;
using FusionContinent;
using UnityEngine;

/// <summary>
/// 对象池初始化模块 - 清理对象池，准备资源
/// </summary>
public class SceneInitObjectPool : SceneMapBase
{
    public override string ModuleName => "对象池初始化";

    protected override async void OnStartLoad()
    {
        // 对象池资源归还
        if (CommonItemPoolManager.Instance != null)
        {
            CommonItemPoolManager.Instance.ReturnAll();
        }

        if (BattleItemPoolManager.Instance != null)
        {
            await BattleItemPoolManager.Instance.PreloadPredefinedPrefabsAsync();
   
        }
        progress = 1.0f;
        // progress = 1.0f;
    }
}
