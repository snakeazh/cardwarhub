using Config;
using FusionContinent;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 主界面编队角色 Spine 槽位：按英灵卡 ID 异步加载对应 Spine 预制体并循环播放 walk 动画。
/// </summary>
public class MainInterfaceSpineSlot : MonoBehaviour
{
    private const string WalkAnimationName = "walk";
    public GameObject ShadowObject;
    private GameObject mSpineObj;
    private int mSoldierCardId;
    private int mLoadVersion;

    /// <summary>当前显示的英灵卡 ID，0 表示空</summary>
    public int SoldierCardId
    {
        get { return mSoldierCardId; }
    }

    /// <summary>
    /// 异步加载指定英灵的 Spine 并循环播放 walk 动画；传入相同 ID 且已加载完成时不重复加载。
    /// </summary>
    public void SetData(int soldierCardId)
    {
        if (mSoldierCardId == soldierCardId && mSpineObj != null) return;
        ShadowObject.SetActive(false);

        Clear();
        mSoldierCardId = soldierCardId;

        var battleCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(soldierCardId);
        if (battleCfg == null || string.IsNullOrEmpty(battleCfg.Spine))
        {
            LogTool.LogError($"[MainInterfaceSpineSlot] 英灵 {soldierCardId} 未配置 Spine");
            return;
        }

        int version = ++mLoadVersion;
        float spineScale = battleCfg.SpineScale;
        StartCoroutine(Global.ResLoader.LoadAsync<GameObject>(battleCfg.Spine,
            prefab => OnSpineLoaded(prefab, version, spineScale)));
    }

    private void OnSpineLoaded(GameObject prefab, int version, float spineScale)
    {
        // 加载期间已切换英灵或被清理，丢弃本次结果
        if (version != mLoadVersion) return;

        if (prefab == null)
        {
            LogTool.LogError($"[MainInterfaceSpineSlot] Spine 异步加载失败: {mSoldierCardId}");
            return;
        }

        mSpineObj = Instantiate(prefab, transform);
        mSpineObj.transform.localPosition = Vector3.zero;
        mSpineObj.transform.localScale = spineScale > 0 ? Vector3.one * spineScale : Vector3.one;

        var skeletonGraphic = mSpineObj.GetComponent<SkeletonGraphic>();
        if (skeletonGraphic == null)
        {
            LogTool.LogError($"[MainInterfaceSpineSlot] Spine 预制体缺少 SkeletonGraphic 组件: {mSoldierCardId}");
            return;
        }
        ShadowObject.SetActive(true);

        skeletonGraphic.AnimationState.SetAnimation(0, WalkAnimationName, true);
    }

    /// <summary>
    /// 清理已加载的 Spine，并使进行中的异步加载结果失效。
    /// </summary>
    public void Clear()
    {
        mLoadVersion++;
        mSoldierCardId = 0;
        if (mSpineObj != null)
        {
            Destroy(mSpineObj);
            mSpineObj = null;
        }
    }
}
