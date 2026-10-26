using System;
using Config;
using FusionContinent;
using FusionContinent.UI.Components;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

/// <summary>
/// 头像列表项数据（POCO），由列表控制器填充。
/// Icon 为图集中的 sprite 名（具体如何由 HeadConfig 推导由调用方决定），
/// 若调用方已直接持有 Sprite，则优先使用 IconSprite，留空 Icon。
/// </summary>
public class HeadEntityData
{
    public int Id = 0;
    public HeadType HeadType = HeadType.Avatar;
    /// <summary>是否当前已佩戴（展示"已佩戴"标识）</summary>
    public bool IsWear = false;
    /// <summary>是否当前选中（展示选中高亮）</summary>
    public bool IsSelected = false;
    /// <summary>是否已解锁（未解锁时灰显图标）</summary>
    public bool IsUnlocked = true;
    /// <summary>点击回调，参数为本项数据，便于列表控制器切换选中态</summary>
    public Action<HeadEntityData> OnClick = null;
}

/// <summary>
/// 头像选择列表项：展示头像图标 + 已佩戴/选中状态，并转发点击。
/// 挂在 HeadEntity 预制体根节点上。字段名与预制体子节点一一对应。
/// 结构与用法参考 RewardEntity / PlayerHead。
/// </summary>
public class HeadEntity : MonoBehaviour
{
    public Image Icon;
    public Image Frame;
    public GameObject IsWear;
    public GameObject IsSelected;

    /// <summary>
    /// 可选：点击按钮。预制体根节点默认只有 Image，若调用方在 prefab 上挂了 Button 并拖到此处，
    /// 则点击会触发 Data.OnClick；未赋值时不响应点击（与原 prefab 不可点击行为一致）。
    /// </summary>
    public UIPointerClickAndLongPress ClickBtn;

    private HeadEntityData _data;

    void Start()
    {
        if (ClickBtn != null)
        {
            ClickBtn.onClick.AddListener(OnClickInternal);
        }
    }

    void OnDestroy()
    {
        if (ClickBtn != null)
        {
            ClickBtn.onClick.RemoveListener(OnClickInternal);
        }
    }

    /// <summary>
    /// 用列表控制器构造好的数据填充本项。
    /// </summary>
    public void SetData(HeadEntityData data)
    {
        _data = data;
        if (data == null) return;

        // 图标：优先用直接传入的 Sprite，否则用 Icon 作为 key 去默认头像图集加载
        if (Icon != null)
        {
            var cfg = DataConfigManager.Tables.TbHeadConfig.Get(data.Id);
            if (data.HeadType == HeadType.AvatarFrame)
            {
                Icon.sprite = Global.PlayerHeadFrameAtlas.GetSprite(cfg.Icon);
            }
            else
            {
                Icon.sprite = Global.PlayerHeadAtlas.GetSprite(cfg.Icon);
            }
        }

        ApplyUnlockVisual(data.IsUnlocked);

        if (IsWear != null)
        {
            IsWear.SetActive(data.IsWear);
        }
        if (IsSelected != null)
        {
            IsSelected.SetActive(data.IsSelected);
        }
    }

    /// <summary>当前持有的数据，供外部读取（如列表控制器刷新选中态时）。</summary>
    public HeadEntityData GetData()
    {
        return _data;
    }

    private void ApplyUnlockVisual(bool isUnlocked)
    {
        if (Icon != null)
        {
            GrayTools.SetGray(Icon.gameObject, isUnlocked);
        }

        if (Frame != null)
        {
            GrayTools.SetGray(Frame.gameObject, isUnlocked);
        }
    }

    private void OnClickInternal()
    {
        if (_data == null) return;
        if (_data.OnClick != null)
        {
            _data.OnClick(_data);
        }
    }
}
