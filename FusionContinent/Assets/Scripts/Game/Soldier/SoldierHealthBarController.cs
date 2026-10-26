using System;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent;
using Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

/// <summary>
/// 角色血条控制器 - 管理血条显示和更新
/// </summary>
public class SoldierHealthBarController
{
    private SoldierCard _soldier;
    
    // 血条组件（引用自 SoldierCard.Designer.cs）
    public Image HP => _soldier.HP;
    public RectTransform HPTransform => _soldier.HP?.rectTransform;
    public TMP_Text HpText => _soldier.HpText;
    public RectTransform LineHpParent => _soldier.LineHpParent;
    public GameObject LineHpObj => _soldier.LineHpObj;
    
    public SoldierHealthBarController(SoldierCard soldier)
    {
        _soldier = soldier;
    }
    
    #region 血条显示控制
    
    /// <summary>
    /// 设置血条可见性
    /// </summary>
    public void SetHealthBarVisible(bool visible)
    {
        if (HP != null)
        {
            HP.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 显示/隐藏血条
    /// </summary>
    public void ShowHealthBar(bool show)
    {
        if (HP != null && HP.gameObject.activeSelf != show)
        {
            HP.gameObject.SetActive(show);
        }
    }
    
    /// <summary>
    /// 设置镜像体血条（不显示）
    /// </summary>
    public void SetupForMirror()
    {
        if (HPTransform != null)
        {
            HPTransform.parent.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 设置无血条模式
    /// </summary>
    public void SetupForNoHealthBar()
    {
        if (HP != null)
        {
            HP.gameObject.SetActive(false);
        }
    }
    
    #endregion
    
    #region 血条更新
    
    /// <summary>
    /// 更新血条显示
    /// </summary>
    public void UpdateHealthBar(int currentHp, int maxHp)
    {
        if (HP == null) return;
        
        float residueHpPer = (float)currentHp / maxHp;
        
        // 更新血条进度
        var progressBar = HP.GetComponent<ProgressBar>();
        if (progressBar != null)
        {
            progressBar.SetProgress(residueHpPer);
        }
        
        // 更新血量文字
        if (HpText != null)
        {
            HpText.text = $"{currentHp}";
        }
    }
    
    #endregion
    
    #region 血条动画
    
    /// <summary>
    /// 血条抖动（受伤时）
    /// </summary>
    public void ShakeHealthBar()
    {
        if (HPTransform == null) return;
        
        HPTransform.DOKill();
        HPTransform.DOShakeAnchorPos(0.3f, new Vector2(5f, 0f), 2, 0);
    }
    
    /// <summary>
    /// 血条渐隐消失
    /// </summary>
    public void FadeOutHealthBar()
    {
        if (HP == null) return;
        
        HP.DOFade(0, 0.5f).OnComplete(() =>
        {
            if (HP != null)
            {
                HP.gameObject.SetActive(false);
                Color c = HP.color;
                c.a = 1;
                HP.color = c;
            }
        });
    }
    
    #endregion
    
    #region 伤害飘字位置
    
    /// <summary>
    /// 获取伤害飘字的随机位置
    /// </summary>
    public Vector3 GetDamageTextOffset()
    {
        if (HpText == null) return Vector3.zero;
        
        float randomX = UnityEngine.Random.Range(-50f, 50f);
        float randomY = UnityEngine.Random.Range(-10f, 10f);
        var rectTransform = HpText.rectTransform;
        return rectTransform != null ? rectTransform.position + new Vector3(randomX / 100, randomY / 100, 0) : Vector3.zero;
    }
    
    #endregion
    
    #region 血条分段
    
    private List<GameObject> _lineHpList = new List<GameObject>();
    
    /// <summary>
    /// 设置血条分段
    /// </summary>
    public void SetLineHp(int maxHp)
    {
        if (LineHpParent == null) return;
        
        int hpGauge = DataConfigManager.Tables.TbGameBattleConst.SoliderHpGauge[0];
        int hpSeparatorCount = DataConfigManager.Tables.TbGameBattleConst.SoliderHpGauge[1];
        int gaugeCount = (maxHp - 1) / hpGauge;

        // 每段宽度
        float hpLineWidth = LineHpParent.rect.width / gaugeCount;

        // 确保已有对象够用，不够再创建
        for (int i = 0; i < gaugeCount; i++)
        {
            GameObject lineHpObj;
            bool IsSeparator = (i + 1) % hpSeparatorCount == 0;
            if (i < _lineHpList.Count) // 已有的直接复用
            {
                lineHpObj = _lineHpList[i];
                lineHpObj.SetActive(true);
            }
            else // 不够时才新建
            {
                lineHpObj = UnityEngine.Object.Instantiate(LineHpObj, LineHpParent.transform);
                lineHpObj.SetActive(true);
                _lineHpList.Add(lineHpObj);
            }

            if (IsSeparator)
            {
                lineHpObj.GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt("Line2");
            }
            else
            {
                lineHpObj.GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt("Line1");
            }
            lineHpObj.GetComponent<Image>().SetNativeSize();

            // 更新位置
            lineHpObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(hpLineWidth * (i + 1), 0);
        }

        // 隐藏多余的
        for (int i = gaugeCount; i < _lineHpList.Count; i++)
        {
            _lineHpList[i].SetActive(false);
        }
    }
    
    #endregion
    
    #region 清理
    
    /// <summary>
    /// 隐藏血条
    /// </summary>
    public void Hide()
    {
        if (HP != null)
        {
            HP.gameObject.SetActive(false);
        }
        if (LineHpObj != null)
        {
            LineHpObj.SetActive(false);
        }
    }
    
    /// <summary>
    /// 销毁
    /// </summary>
    public void Destroy()
    {
        Hide();
        _lineHpList.Clear();
    }
    
    #endregion
}
