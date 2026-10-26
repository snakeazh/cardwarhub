using FusionContinent;
using FusionContinent.Manager;
using UnityEngine;
using DG.Tweening;
using UIFramework;

public class CombatTipController : MonoBehaviour
{
    public static CombatTipController Instance;

    private int _combatTotalPrevNum;

    private const string CombatTipPoolName = "CombatTip";

    private Transform _activeCombatTip;
    private Sequence _lastPlaySeq;
    private Tween _numberRollTween;
    private float _rollingValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        _lastPlaySeq?.Kill();
        _numberRollTween?.Kill();
        HideAndReturnTip();
    }

    private void Start()
    {
        _combatTotalPrevNum = Global.User.FightingNum;
    }

    public void SetCombatNum()
    {
        _combatTotalPrevNum = Global.User.FightingNum;
    }

    public void SetCombatNum(int fightingNum)
    {
        _combatTotalPrevNum = fightingNum;
    }

    /// <summary>
    /// 空方法，保留以兼容外部调用（SkillUpGradePanelViewModel）。
    /// </summary>
    public void SetCombat()
    {
    }

    public void ShowCombat()
    {
        int currentNum = Global.User.FightingNum;
        int diff = currentNum - _combatTotalPrevNum;

        LogTool.Log($"Combat change: {_combatTotalPrevNum} -> {currentNum}, diff: {diff}");

        if (diff == 0) return;

        if (diff > 0)
        {
            CreateCombatTip(_combatTotalPrevNum, currentNum);
        }

        _combatTotalPrevNum = currentNum;
    }

    public void ShowCombatDiff(int startValue, int endValue)
    {
        if (endValue <= startValue) return;

        CreateCombatTip(startValue, endValue);
        _combatTotalPrevNum = endValue;
    }

    private void CreateCombatTip(int startValue, int endValue)
    {
        var tipsLayer = UIRoot.Instance.Config.GetLevelTransform(UILevel.Tips);
        bool isAlreadyShowing = _activeCombatTip != null && _activeCombatTip.gameObject.activeSelf;

        // Tip 已在显示时以当前滚动中的显示值为起点，否则用传入的起始值（服务端基线）；
        // 新值不超过起点说明是重复/无效调用，直接忽略，避免红字显示 0
        int displayStart = isAlreadyShowing ? Mathf.RoundToInt(_rollingValue) : startValue;
        if (endValue <= displayStart)
        {
            return;
        }

        if (_activeCombatTip == null)
        {
            var poolObj = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate(CombatTipPoolName)?.Get();
            if (poolObj == null)
            {
                LogTool.LogError("Failed to get combat tip from pool");
                return;
            }
            _activeCombatTip = poolObj.transform;
            _activeCombatTip.SetParent(tipsLayer);
            _activeCombatTip.localScale = Vector3.one;
            _activeCombatTip.localPosition = Vector3.zero;
        }

        var combatTip = _activeCombatTip;
        var tipComponent = combatTip.GetComponent<CombatTip>();
        if (tipComponent == null)
        {
            LogTool.LogError("CombatTip component not found");
            HideAndReturnTip();
            return;
        }

        // 仅在 Tip 未显示时播放弹入动画
        if (!isAlreadyShowing)
        {
            tipComponent.AnimationNode.DOKill();
            tipComponent.AnimationNode.localScale = Vector3.zero;
            tipComponent.AnimationNode.DOScale(Vector3.one, 0.2f);
        }

        // 每次更新战力都重置自动关闭计时器
        _lastPlaySeq?.Kill();
        _lastPlaySeq = DOTween.Sequence()
            .AppendInterval(2f)
            .AppendCallback(HideAndReturnTip);

        // 从起点值继续滚动，差值 = 新值 - 起点（已在方法开头保证为正）
        _rollingValue = displayStart;
        tipComponent.combatNumTextNew.text = (endValue - displayStart).ToString();

        _numberRollTween?.Kill();
        tipComponent.combatNumText.text = displayStart.ToString();
        _numberRollTween = DOTween.To(
                () => _rollingValue,
                x =>
                {
                    _rollingValue = x;
                    tipComponent.combatNumText.text = Mathf.RoundToInt(x).ToString();
                },
                endValue,
                0.5f)
            .SetEase(Ease.OutQuad);
    }

    private void HideAndReturnTip()
    {
        if (_activeCombatTip == null) return;

        _numberRollTween?.Kill();
        _numberRollTween = null;

        var tipComponent = _activeCombatTip.GetComponent<CombatTip>();
        if (tipComponent != null)
        {
            tipComponent.AnimationNode.localScale = Vector3.zero;
        }

        _activeCombatTip.gameObject.SetActive(false);
        CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate(CombatTipPoolName)?.Return(_activeCombatTip.gameObject);
        _activeCombatTip = null;
    }
}
