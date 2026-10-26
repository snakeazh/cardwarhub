using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using UnityEngine;

/// <summary>
/// 伤害UI管理器 - 集中管理所有伤害飘字和受击特效
/// </summary>
public class DamageUIManager : MonoBehaviour
{
    private static DamageUIManager _instance;
    public static DamageUIManager Instance
    {
        get
        {
            return _instance;
        }
    }
    private GameObjectPool pool;

    public GameObjectPool Pool => pool;

    private Transform _damageParent;
    private Transform DamageParent
    {
        get
        {
            if (_damageParent == null && GameLand.Default != null)
            {
                _damageParent = GameLand.Default.DamageParent;
            }
            return _damageParent;
        }
    }

    // 音效标志
    private bool _isSoundPlaying = false;

    // 伤害Tween缓存
    private List<Tween> _damageTweens = new List<Tween>();
    private int _currentTweenIndex = 0;
    private const int MAX_TWEEN_CACHE = 9;

    private void Awake()
    {
        _instance = GetComponent<DamageUIManager>();
        pool = BattleItemPoolManager.Instance.GetGameObjectPoolOrCreate(GlobalConst.DamageUIPool);  //伤害pool初始化
    }

    private void Start()
    {
        EventBus.Subscribe<HpDamageEvent>(OnHpDamageEvent);
    }

    private IEnumerator ResetSoundFlag(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isSoundPlaying = false;
    }

    // 初始化Tween缓存
    private void InitTweenCache()
    {
        if (_damageTweens.Count == 0)
        {
            for (int i = 0; i < MAX_TWEEN_CACHE; i++)
                _damageTweens.Add(null);
        }
    }

    private void OnHpDamageEvent(HpDamageEvent e)
    {
        if (e.IsMiss)
        {
            ShowMissEffect(e);
            return;
        }

        if (e.IsExecution)
        {
            ShowExecutionEffect(e);
            return;
        }

        ShowDamageEffect(e);
    }

    private void ShowMissEffect(HpDamageEvent e)
    {
        if (DamageParent == null)
            return;

        var monsterCard = e.TargetCard;
        int siblingIndex = monsterCard?.mySiblingIndex ?? 0;
        string groupName = monsterCard != null ? (monsterCard.mySiblingIndex % 1000).ToString() : "0";
        Transform group = DamageParent.Find(groupName);
        if (group == null)
            group = GetOrCreateTempGroup();

        DamageFun.Show("M", e.Position, TakeDamageType.Miss, false, false, default, siblingIndex, group);
    }

    private void ShowExecutionEffect(HpDamageEvent e)
    {
        var monsterCard = e.TargetCard;
        if (monsterCard == null)
        {
            return;
        }

        if (e.Damage > 0)
        {
            ShowDamageNumber(e, monsterCard);
        }

        monsterCard.hp = 0;
        monsterCard.Die();
    }

    private Transform GetOrCreateTempGroup()
    {
        if (DamageParent.Find("TempGroup") == null)
        {
            var temp = new GameObject("TempGroup");
            temp.transform.SetParent(DamageParent);
            temp.transform.localPosition = Vector3.zero;
            temp.transform.localScale = Vector3.one;
        }
        return DamageParent.Find("TempGroup");
    }

    /// <summary>
    /// 显示伤害效果
    /// </summary>
    public void ShowDamageEffect(HpDamageEvent e)
    {
        var monsterCard = e.TargetCard;

        if (!_isSoundPlaying)
        {
            _isSoundPlaying = true;
            StartCoroutine(ResetSoundFlag(2f));
        }

        // 如果是Boss，更新血条
        if (monsterCard?.IsBoos == true)
        {
            monsterCard.SetHealth();
        }

        InitTweenCache();

        // 受击特效
        var hitEffect = "AnnaNormalAttackHit01";
        switch (e.DamageType)
        {
            case TakeDamageType.MagicDamageThunder:
                hitEffect = "BondCritHit01";
                break;
        }

        // 获取或创建对象池
        var pool2 = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(hitEffect);
        if (pool2 != null)
        {
            var damageFx = pool2.Get();
            damageFx.transform.position = e.Position;
            damageFx.transform.localScale = Vector3.one;

            Vector3 direction = damageFx.transform.position - e.Target.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            damageFx.transform.eulerAngles = new Vector3(0, 0, angle);

            var autoRecycle = damageFx.GetComponent<EffectAutoRecycle>();
            if (autoRecycle == null)
                autoRecycle = damageFx.AddComponent<EffectAutoRecycle>();

            autoRecycle.Init(pool2, 0.5f);
        }
        

        // 计算延迟
        float delay = 0f;

        // Kill旧tween，准备新tween
        if (_damageTweens[_currentTweenIndex] != null)
            _damageTweens[_currentTweenIndex].Kill();

        _damageTweens[_currentTweenIndex] = DOVirtual.DelayedCall(delay, () =>
        {
            if (DamageParent == null)
                return;

            ShowDamageNumber(e, monsterCard);
        })
            .SetId(GlobalConst.BattleTween);

        _currentTweenIndex = (_currentTweenIndex + 1) % _damageTweens.Count;
    }

    private void ShowDamageNumber(HpDamageEvent e, MonsterCard monsterCard)
    {
        Vector3 basePosition = e.Position;
        string groupName = monsterCard != null ? (monsterCard.mySiblingIndex % 1000).ToString() : "0";
        Transform group = DamageParent.Find(groupName);

        if (group == null)
        {
            if (DamageParent.Find("TempGroup") == null)
            {
                var temp = new GameObject("TempGroup");
                temp.transform.SetParent(DamageParent);
                temp.transform.localPosition = Vector3.zero;
                temp.transform.localScale = Vector3.one;
            }
            group = DamageParent.Find("TempGroup");
        }

        int siblingIndex = monsterCard?.mySiblingIndex ?? 0;

        // 闪避
        if (e.DamageType == TakeDamageType.Miss)
        {
            DamageFun.Show("M", basePosition, TakeDamageType.Miss, false, false, default, siblingIndex, group);
        }
        else
        {
            DamageFun.Show(e.Damage.ToString(), basePosition, e.DamageType, e.IsCrit, false, default, siblingIndex, group);
        }
    }

    /// <summary>
    /// 显示韧性伤害飘字
    /// </summary>
    public void ShowToughnessDamage(float toughnessValue, Vector3 pos)
    {
        if (toughnessValue <= 0f)
            return;

        Transform group = DamageParent?.Find("TempGroup");
        if (group == null)
        {
            var temp = new GameObject("TempGroup");
            temp.transform.SetParent(DamageParent);
            temp.transform.localPosition = Vector3.zero;
            temp.transform.localScale = Vector3.one;
            group = temp.transform;
        }

        DamageFun.Show(toughnessValue.ToString(), pos, TakeDamageType.General, false, true, default, -1, group);
    }

    /// <summary>
    /// 回收当前所有伤害飘字（局内重开时调用）
    /// </summary>
    public void RecycleAllActiveDamageTexts()
    {
        ClearAllDamageTweens();

        if (pool == null || DamageParent == null)
        {
            return;
        }

        for (int i = 0; i < DamageParent.childCount; i++)
        {
            var group = DamageParent.GetChild(i);
            for (int j = group.childCount - 1; j >= 0; j--)
            {
                var child = group.GetChild(j).gameObject;
                if (child.activeSelf)
                {
                    pool.Return(child);
                }
            }
        }
    }

    /// <summary>
    /// 清除所有伤害Tween
    /// </summary>
    public void ClearAllDamageTweens()
    {
        foreach (var tween in _damageTweens)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }
        _damageTweens.Clear();
        _currentTweenIndex = 0;
    }

    /// <summary>
    /// 重置状态（切场景用）
    /// </summary>
    public void Reset()
    {
        RecycleAllActiveDamageTexts();
        _isSoundPlaying = false;
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<HpDamageEvent>(OnHpDamageEvent);
        pool.Clear();
    }
}
