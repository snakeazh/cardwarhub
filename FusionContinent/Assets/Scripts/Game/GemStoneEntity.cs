using System.Collections.Generic;
using Config;
using FusionContinent;
using Game.Equip;
using QFramework;
using TMPro;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class GemStoneData
{
    public int GemId;
    public bool Equip;
    public int Level;
    public bool IsSelect = false;
}

public class GemStoneEntity : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image frame;
    [SerializeField] private Image frameOut;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Transform effect;
    [SerializeField] private Transform equipIsHave;
    [SerializeField] private Transform Selected;

    private GemStoneData mData;

    private Dictionary<QualityType, QualityTypeEffectstruct> mQualityTypeEffects = new Dictionary<QualityType, QualityTypeEffectstruct>();
    private readonly Dictionary<string, GameObject> mCacheEffects = new Dictionary<string, GameObject>();
    private GameObject mPersistentEffectObj;
    private GameObject mStartEffectObj;

    private static readonly float EffectRate = 120f;

    private void Awake()
    {
        var qualityTypeEffects = DataConfigManager.Tables.TbGameBattleConst.QualityTypeEffects;
        foreach (var v in qualityTypeEffects)
        {
            mQualityTypeEffects.Add(v.Type, v);
        }
    }

    public void SetData(GemStoneData data)
    {
        mData = data;
        RefreshView();
    }
    
    

    private void RefreshView()
    {
        if (mData == null) return;

        var cfg = DataConfigManager.Tables.TbGemFusionConfig.GetOrDefault(mData.GemId);
        if (cfg == null)
        {
            LogTool.LogError($"[GemStoneEntity] 融合石配置不存在，GemId={mData.GemId}");
            return;
        }

        // 图标
        if (icon != null)
        {
            icon.sprite = Global.FusionGemIconSpriteAtlas.GetSprite(cfg.Icon);
        }

        // 品质框
        if (frame != null)
        {
            frame.sprite = Global.GemQualityFrameAtlas.GetSprite($"{cfg.Quality}");
        }

        // 等级
        if (levelText != null)
        {
            levelText.text = GameServiceManager.Get<FusionGemService>().GetGemLevelText(mData.Level);
            levelText.gameObject.SetActive(mData.Level > 0);
        }

        // 特效
        SetEffect(cfg.Quality);
        equipIsHave.gameObject.SetActive(mData.Equip);
        this.Selected?.gameObject.SetActive(mData.IsSelect);
    }

    private void SetEffect(QualityType quality)
    {
        if (effect == null) return;

        if (!mQualityTypeEffects.ContainsKey(quality)) return;

        var qtEffectCfg = mQualityTypeEffects[quality];

        // 出场特效
        mStartEffectObj?.SetActive(false);
        if (qtEffectCfg != null && !string.IsNullOrEmpty(qtEffectCfg.EffectName) && mData?.Equip == true)
        {
            var pool = CommonItemPoolManager.Instance.GetGameObjectPool(qtEffectCfg.EffectName);
            if (pool != null)
            {
                if (mCacheEffects.ContainsKey(qtEffectCfg.EffectName))
                {
                    mStartEffectObj = mCacheEffects[qtEffectCfg.EffectName];
                }
                else
                {
                    mStartEffectObj = pool.Get();
                    mCacheEffects.Add(qtEffectCfg.EffectName, mStartEffectObj);
                }

                mStartEffectObj.GetOrAddComponent<ParticleClipParent>();

                if (mStartEffectObj != null)
                {
                    mStartEffectObj.SetActive(true);
                    mStartEffectObj.transform.SetParent(effect, false);
                    mStartEffectObj.transform.localPosition = Vector3.zero;
                    mStartEffectObj.transform.localScale = Vector3.one * EffectRate;
                }
            }
        }

        // 常驻特效
        mPersistentEffectObj?.SetActive(false);
        if (qtEffectCfg != null && !string.IsNullOrEmpty(qtEffectCfg.EffectName2))
        {
            var pool = CommonItemPoolManager.Instance.GetGameObjectPool(qtEffectCfg.EffectName2);
            if (pool != null)
            {
                if (mCacheEffects.ContainsKey(qtEffectCfg.EffectName2))
                {
                    mPersistentEffectObj = mCacheEffects[qtEffectCfg.EffectName2];
                }
                else
                {
                    mPersistentEffectObj = pool.Get();
                    mCacheEffects.Add(qtEffectCfg.EffectName2, mPersistentEffectObj);
                }

                mPersistentEffectObj.GetOrAddComponent<ParticleClipParent>();
                mPersistentEffectObj.transform.SetParent(effect, false);
                mPersistentEffectObj.SetActive(true);
                mPersistentEffectObj.transform.localScale = Vector3.one * EffectRate;
                mPersistentEffectObj.transform.localPosition = Vector3.zero;
            }
        }
    }

    public void ReturnEffect()
    {
        if (effect == null || mData == null) return;

        var cfg = DataConfigManager.Tables.TbGemFusionConfig.GetOrDefault(mData.GemId);
        if (cfg == null || !mQualityTypeEffects.ContainsKey(cfg.Quality)) return;

        var targetEffect = mQualityTypeEffects[cfg.Quality];

        for (int i = 0; i < effect.childCount; i++)
        {
            Transform child = effect.GetChild(i);
            if (child == null) continue;

            string childBaseName = child.name.Split('(')[0];

            if (childBaseName != targetEffect.EffectName &&
                childBaseName != targetEffect.EffectName2)
            {
                var pool = CommonItemPoolManager.Instance.GetGameObjectPool(childBaseName);
                pool?.Return(child.gameObject);
            }
        }
    }
}
