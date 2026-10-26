using System;
using Config;
using FusionContinent;
using FusionContinent.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SkillItemView : MonoBehaviour
{
    public Transform IsAwake;
    public Transform IsPassive;
    public Image SkillIcon;
    public TextMeshProUGUI SkillName;
    public Transform IsLock;
    public Transform LevelText;

    private Image mCdMask;
    private TextMeshProUGUI mCdTime;

    private int mSkillId;
    private int mCardId;
    private Action<int> mOnClick;

    private void Awake()
    {
        mCdMask = transform.Find("cdMask")?.GetComponent<Image>();
        mCdTime = transform.Find("cdMask/cdTime")?.GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// 设置技能数据并刷新显示
    /// </summary>
    public void SetData(int cardId, SpecialEffectsConfig skillCfg, int soldierLv, Action<int> onClick = null,
        bool isBattleDisplay = false)
    {
        mCardId = cardId;
        mOnClick = onClick;

        if (skillCfg == null)
        {
            Clear();
            return;
        }

        mSkillId = skillCfg.Id;
        gameObject.SetActive(true);

        var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(skillCfg.Id);
        var isAwakingOrPassive = skillCfg.AwakingSkill == 1 || skillCfg.PassiveSkill == 1;
        var showLevelText = isAwakingOrPassive && !isBattleDisplay;
        // 是否已解锁以是否拥有卡牌数据为准；soldierLv >= UnlockLv 只表示达到可解锁等级，不代表已解锁
        var skillCard = Global.GetCardById(skillCfg.Id);
        if (LevelText != null)
        {
            LevelText.gameObject.SetActive(true);
            // if (showLevelText)
            // {
                var levelTextNum = LevelText.Find("LevelNum").GetComponent<TextMeshProUGUI>();
                levelTextNum.text = $"{(skillCard != null ? skillCard.Lv : 1)}";
            // }
        }

        if (SkillIcon != null)
        {
            if (cardCfg != null)
            {
                SkillIcon.sprite = Global.CardIconSpriteAtlas.GetSpriteExt(cardCfg.Icon);
                SkillIcon.gameObject.SetActive(true);
            }
            else
            {
                SkillIcon.sprite = null;
                SkillIcon.gameObject.SetActive(false);
            }
        }

        if (SkillName != null)
        {
            SkillName.text = cardCfg?.Name ?? "";
            // SkillName.gameObject.SetActive(cardCfg != null);
        }

        bool isUnlocked = skillCard != null && skillCard.IsHave;
        if (isBattleDisplay)
        {
            if (IsLock != null)
                IsLock.gameObject.SetActive(false);

            if (SkillIcon != null)
                SkillIcon.color = Color.white;
        }
        else
        {
            if (IsLock != null)
                IsLock.gameObject.SetActive(!isUnlocked);

            if (SkillIcon != null)
                SkillIcon.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        if (IsAwake != null)
            IsAwake.gameObject.SetActive(skillCfg.AwakingSkill == 1);

        // if (IsPassive != null)
        //     IsPassive.gameObject.SetActive(skillCfg.PassiveSkill == 1);

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClicked);
            btn.onClick.AddListener(OnClicked);
        }
    }

    public void UpdateCooldownUI(float remainTime, float totalTime)
    {
        var isCooling = totalTime > 0f && remainTime > 0f;

        if (mCdMask != null)
        {
            mCdMask.gameObject.SetActive(isCooling);
            mCdMask.fillAmount = isCooling ? Mathf.Clamp01(remainTime / totalTime) : 0f;
        }

        if (mCdTime != null)
        {
            mCdTime.gameObject.SetActive(isCooling);
            mCdTime.text = isCooling
                ? $"{Mathf.FloorToInt(remainTime)}.{((int)(remainTime * 10)) % 10}"
                : string.Empty;
        }
    }

        public void SetCooldownReady()
    {
        UpdateCooldownUI(0f, 0f);
    }

    public void SetInteractable(bool interactable)
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = interactable;
        }
    }

    public void SetUnlockTip(string desc)
    {
        FusionContinent.Toast.Show(desc);
    }

    public int GetSkillId() => mSkillId;

    private void OnClicked()
    {
        mOnClick?.Invoke(mSkillId);
    }

    private void Clear()
    {
        mSkillId = 0;
        gameObject.SetActive(false);

        if (SkillIcon != null)
        {
            SkillIcon.sprite = null;
            SkillIcon.gameObject.SetActive(false);
        }

        if (SkillName != null)
        {
            SkillName.text = "";
            SkillName.gameObject.SetActive(false);
        }

        if (IsLock != null)
            IsLock.gameObject.SetActive(false);

        if (IsAwake != null)
            IsAwake.gameObject.SetActive(false);

        if (IsPassive != null)
            IsPassive.gameObject.SetActive(false);

        if (LevelText != null)
            LevelText.gameObject.SetActive(false);

        SetCooldownReady();

        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.RemoveListener(OnClicked);
    }
}
