using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using Sequence = DG.Tweening.Sequence;

namespace FusionContinent
{
	public class RewardRollPanelData : UIPanelData
	{
	}
	public partial class RewardRollPanel : UIPanel
	{
		[Header("UI")]
	    public RectTransform viewport; // Mask
	    public RectTransform content;  // 奖品父节点
	    public float itemHeight = 120f;

	    [Header("滚动参数")]
	    public int loopCount = 3;             // 滚动圈数
	    public float startSpeed = 2500f;      // 初速度 px/s
	    public float minSpeed = 300f;         // 最小滚动速度
	    public float decel = 2000f;           // 减速度
	    public float slowDownDistance = 400f; // 开始缓动距离

	    [Header("视觉效果")]
	    public bool highlightTarget = true;
	    public Color highlightColor = Color.yellow;

	    private List<RectTransform> items = new List<RectTransform>();
	    private int itemCount;
	    private float panelHeight;
	    private bool isRolling;

	    void Awake()
	    {
	        InitLayout();
	    }

	    void InitLayout()
	    {
	        items.Clear();
	        itemCount = content.childCount;
	        for (int i = 0; i < itemCount; i++)
	        {
	            RectTransform item = content.GetChild(i) as RectTransform;
	            items.Add(item);
	            item.anchoredPosition = new Vector2(0, -i * itemHeight);
	        }
	        panelHeight = itemCount * itemHeight;
	    }

	    [ContextMenu("Test Roll")]
	    public void TestRoll()
	    {
	        StartRoll(Random.Range(0, itemCount));
	    }

	    public void StartRoll(int targetIndex)
	    {
	        if (isRolling) return;
	        isRolling = true;
	        targetIndex = Mathf.Clamp(targetIndex, 0, itemCount - 1);

	        float viewportHeight = viewport.rect.height;
	        float maskCenterOffset = viewportHeight / 2f - itemHeight / 2f;

	        // 总滚动距离 = loopCount * panelHeight + targetIndex * itemHeight - maskCenterOffset
	        float totalDistance = loopCount * panelHeight + targetIndex * itemHeight - maskCenterOffset;

	        StartCoroutine(SmoothRoll(totalDistance, targetIndex));
	    }

	    IEnumerator SmoothRoll(float totalDistance, int targetIndex)
	    {
	        float travelled = 0f;
	        float speed = startSpeed;

	        while (travelled < totalDistance)
	        {
	            // 缓动：接近目标逐渐减速
	            if (totalDistance - travelled < slowDownDistance)
	            {
	                speed = Mathf.Lerp(minSpeed, speed, (totalDistance - travelled) / slowDownDistance);
	            }
	            else
	            {
	                speed -= decel * Time.deltaTime;
	                if (speed < minSpeed) speed = minSpeed;
	            }

	            float delta = speed * Time.deltaTime;
	            travelled += delta;

	            MoveItems(delta);
	            yield return null;
	        }

	        // 滚动结束，高亮中奖 item
	        isRolling = false;
	        if (highlightTarget)
	            HighlightTarget(targetIndex);
	    }

	    void MoveItems(float delta)
	    {
	        for (int i = 0; i < items.Count; i++)
	        {
	            RectTransform item = items[i];
	            Vector2 pos = item.anchoredPosition;
	            pos.y -= delta;

	            // 无限循环
	            if (pos.y < -panelHeight)
	                pos.y += panelHeight;

	            item.anchoredPosition = pos;
	        }
	    }

	    void HighlightTarget(int index)
	    {
	        var target = items[index];
	        var img = target.GetComponentInChildren<Image>();
	        // var txt = target.GetComponentInChildren<Text>();

	        if (img) img.color = highlightColor;
	        // if (txt) txt.color = highlightColor;

	        target.DOScale(1.2f, 0.3f).SetLoops(2, LoopType.Yoyo);
	    }
		

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as RewardRollPanelData ?? new RewardRollPanelData();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}
	}
}
