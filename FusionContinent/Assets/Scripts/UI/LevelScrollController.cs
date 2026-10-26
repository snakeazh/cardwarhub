using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Config;
using FusionContinent;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelScrollController : MonoBehaviour
{
    public Transform levelObjParent;
    public ScrollRect scrollRect;
    private TbCardDevelopConfig _developConfig;
    private Dictionary<int, GameObject> levelObjs = new Dictionary<int, GameObject>();
    private Dictionary<int, RectTransform> levelTransforms = new Dictionary<int, RectTransform>();
    private RectTransform contentRect;
    private VerticalLayoutGroup layoutGroup;
    private ContentSizeFitter sizeFitter;

    private void Awake()
    {
        _developConfig = DataConfigManager.Tables.TbCardDevelopConfig;
        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
        }
        if (scrollRect != null)
        {
            contentRect = scrollRect.content;
            layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
            sizeFitter = contentRect.GetComponent<ContentSizeFitter>();
        }
    }

    private void OnDisable()
    {
        // CommonItemPoolManager.Instance.GetGameObjectPool("LevelObj").ReturnAllEffect();
    }

    public void InitLevel(int soliderId)
    {
        levelObjs.Clear();
        levelTransforms.Clear();
        
        var levelObjsItem = _developConfig.GetOrDefault(soliderId).LevelPropertyList;
        int levelclone = -1;
        
        // 先创建所有对象
        for (int i = 0; i < levelObjsItem.Count; i++)
        {
            var levelpool = CommonItemPoolManager.Instance.GetGameObjectPool("LevelObj");
            GameObject levelObj = levelpool.Get();
            if (levelObj == null)
            {
                i--;
                continue;
            }
            
            levelObj.transform.SetParent(levelObjParent);
            levelObj.transform.localPosition = Vector3.zero;
            levelObj.transform.localScale = Vector3.one;
            
            int level = levelObjsItem[i].Level;
            levelObjs.Add(level, levelObj);
            levelTransforms.Add(level, levelObj.GetComponent<RectTransform>());
            
            // 设置文本内容
            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < levelObjsItem[i].UpGradeEffectsDesc.Count; j++)
            {
                if (j > 0) sb.Append(", ");
                sb.Append(levelObjsItem[i].UpGradeEffectsDesc[j]);
            }
            
            levelObj.transform.Find("Attributetxt").GetComponent<TMP_Text>().text = sb.ToString();
            levelObj.transform.Find("Leveltxt").GetComponent<TMP_Text>().text = "Lv." + level;
            
            // 设置解锁状态
            if (level <= Global.GetCardById(soliderId).Lv)
            {
                levelObj.transform.Find("LevelObjBg").localScale = Vector3.one;
                levelclone = level;
                levelObj.transform.Find("UnLevelObjBg").localScale = Vector3.zero;
                levelObj.transform.Find("Lock").localScale = Vector3.zero;
            }
            else
            {
                levelObj.transform.Find("LevelObjBg").localScale = Vector3.zero;
                levelObj.transform.Find("UnLevelObjBg").localScale = Vector3.one;
                levelObj.transform.Find("Lock").localScale = Vector3.one;
            }
        }
        
        // 强制更新布局
        StartCoroutine(ScrollToLevelAfterLayout(levelclone));
    }

    private IEnumerator ScrollToLevelAfterLayout(int level)
    {
        // 等待一帧让布局更新
        yield return null;
        yield return null; // 有时需要两帧
        
        // 现在滚动到指定位置
        ScrollToLevel(level);
    }

    public void RefLevel(int level)
    {
        // 先更新所有小于等于当前等级的状态
        foreach (var kvp in levelObjs)
        {
            int currentLevel = kvp.Key;
            var levelObj = kvp.Value;
        
            if (currentLevel < level)
            {
                // 已解锁的等级
                levelObj.transform.Find("LevelObjBg").localScale = Vector3.one;
                levelObj.transform.Find("UnLevelObjBg").localScale = Vector3.zero;
                levelObj.transform.Find("Lock").localScale = Vector3.zero;
            }
            else
            {
                // 未解锁的等级
                levelObj.transform.Find("LevelObjBg").localScale = Vector3.zero;
                levelObj.transform.Find("UnLevelObjBg").localScale = Vector3.one;
                levelObj.transform.Find("Lock").localScale = Vector3.one;
            }
        }
    
        // 滚动到当前等级
        if (levelObjs.TryGetValue(level, out var targetLevelObj))
        {
            StartCoroutine(ScrollToLevelAfterLayout(level));
        }
    }

    public void ScrollToLevel(int level)
    {
        if (scrollRect == null || contentRect == null) return;
        if (!levelTransforms.TryGetValue(level, out var targetRect)) return;
        float duction=80f;
        // 确保布局是最新的
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        if (level > 35)
        {
            duction = 77;
        }
        // 平滑滚动
        StartCoroutine(SmoothScrollTo((level-1)*duction));
    }

    private IEnumerator SmoothScrollTo(float targetPosition)
    {
        float startPosition = contentRect.anchoredPosition.y;
        float duration = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, Mathf.Lerp(startPosition, targetPosition, t));
            yield return null;
        }
        
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, targetPosition);
    }
}