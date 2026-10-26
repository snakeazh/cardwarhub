using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public class CreateShowLeader : MonoBehaviour
{
    public SkeletonGraphic skeletonGraphic;
    private TbPlayerFigureConfig _playerFigureConfig;
    private TbPlayerConst _playerConst;
    public Button boybtn;
    public Button girlbtn;

    // 缓存：slotName -> 默认附件
    private Dictionary<string, Spine.Attachment> _defaultAttachments = new Dictionary<string, Spine.Attachment>();
    // Start is called before the first frame update
    private void Awake()
    {
        _playerFigureConfig = DataConfigManager.Tables.TbPlayerFigureConfig;
        _playerConst = DataConfigManager.Tables.TbPlayerConst;

        // 缓存所有插槽的默认附件
        if (skeletonGraphic?.Skeleton != null)
        {
            var skeleton = skeletonGraphic.Skeleton;
            foreach (var slot in skeleton.Slots)
            {
                var defaultAttachment = skeleton.GetAttachment(slot.Data.Index, slot.Data.Name);
                if (defaultAttachment != null)
                {
                    _defaultAttachments[slot.Data.Name] = defaultAttachment;
                }
            }
        }
    }

    void Start()
    {
        boybtn.onClick.AddListener(() =>
        {
            CloseAllSlots();
            boybtn.transform.GetChild(0).localScale = Vector3.one;
            girlbtn.transform.GetChild(0).localScale = Vector3.zero;
            Global.Player.FigureData = _playerConst.InitManFigures;
            foreach (var figureId in _playerConst.InitManFigures)
            {
                var figureCfg = _playerFigureConfig.GetOrDefault(figureId);
                if (figureCfg != null)
                {
                    SetPlayerClothing(figureCfg.Id);
                }
            }
        });
        girlbtn.onClick.AddListener(() =>
        {
            CloseAllSlots();
            boybtn.transform.GetChild(0).localScale = Vector3.zero;
            girlbtn.transform.GetChild(0).localScale = Vector3.one;
            Global.Player.FigureData = _playerConst.InitGirlFigures;
            foreach (var figureId in _playerConst.InitGirlFigures)
            {
                var figureCfg = _playerFigureConfig.GetOrDefault(figureId);
                if (figureCfg != null)
                {
                    SetPlayerClothing(figureCfg.Id);
                }
            }
        });
        boybtn.onClick.Invoke();
    }
    
    private void SetPlayerClothing(int figureId)
    {
        var clothing = _playerFigureConfig.GetOrDefault(figureId);
        if (clothing != null)
        {
            // ShowSlot(clothing.ShowSpine);
        }
    }

    private void CloseAllSlots()
    {
        foreach (var cfg in _playerFigureConfig.DataList)
        {
            // HideSlot(cfg.ShowSpine);
        }
    }

    private void HideSlot(List<string> slotNames)
    {
        if (slotNames == null || skeletonGraphic?.Skeleton == null) return;

        var skeleton = skeletonGraphic.Skeleton;
        foreach (var slotName in slotNames)
        {
            var slot = skeleton.FindSlot(slotName);
            if (slot != null)
            {
                slot.Attachment = null;
            }
        }

        skeletonGraphic.LateUpdate();
    }

    private void ShowSlot(List<string> slotNames)
    {
        if (slotNames == null || skeletonGraphic?.Skeleton == null) return;

        var skeleton = skeletonGraphic.Skeleton;
        bool updated = false;

        foreach (var slotName in slotNames)
        {
            var slot = skeleton.FindSlot(slotName);
            if (slot == null) continue;

            // 获取 setup pose 的 attachmentName
            var attachmentName = slot.Data.AttachmentName;

            if (!string.IsNullOrEmpty(attachmentName))
            {
                skeleton.SetAttachment(slotName, attachmentName); // ✅ 官方推荐用法
                updated = true;
            }
            else
            {
                LogTool.LogWarning($"插槽 {slotName} 没有默认附件名，请检查 Spine 导出配置。");
            }
        }

        if (updated) skeletonGraphic.LateUpdate();
    }
}
