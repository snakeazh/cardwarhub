using System.Collections;
using Config;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class UnlockGuidePanelData : UIPanelData
	{
	}
	public partial class UnlockGuidePanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UnlockGuidePanelData ?? new UnlockGuidePanelData();
			// please add init code here
			// if (Global.Debug.Value == 0)
			// {
			// 	this.SkipBtn.gameObject.SetActive(false);
			// }
			this.SkipBtn.onClick.AddListener(() =>
			{
				UnlockGuideManager.Instance.ForceCompleteGuide();
			});
		}
		public int currentStep = 1;
		Canvas mParentCanvas;
		GraphicRaycaster  mRaycaster;

		private float searchDuration = 0f;
		protected override void OnOpen(IUIData uiData = null)
		{
			mParentCanvas = GetComponentInParent<Canvas>();
			mRaycaster = GetComponentInParent<GraphicRaycaster>();
			SetFingerDepth();
			currentStep = UnlockGuideManager.Instance != null
				? UnlockGuideManager.Instance.PendingStartStep
				: 1;
			if (currentStep <= 0)
			{
				currentStep = 1;
			}
			StartCoroutine(StartGuide());
		}

		void SetFingerDepth()
		{
			var psList = this.Finger.GetComponentsInChildren<Renderer>();
			foreach (Renderer rn in psList)
			{
				rn.sortingLayerName =  mParentCanvas.sortingLayerName ;
				rn.sortingOrder = mParentCanvas.sortingOrder+20;
			}
		}

		IEnumerator StartGuide()
		{
			this.Dialoge.DOKill(false);
			this.Dialoge.localScale = Vector3.zero;
			
			this.Dialoge.gameObject.SetActive(false);
			this.Finger.gameObject.SetActive(false);
			Maskabled.sizeDelta = Vector2.zero;
			this.NextBtn.gameObject.SetActive(false);
            var config = UnlockGuideManager.Instance.GetModuleStep(currentStep);
			if (config == null)
			{
				LogTool.LogError($"UnlockGuidePanel: 找不到步骤配置 StepId={currentStep}, Module={UnlockGuideManager.Instance.CurrentModuleFunction}");
				UnlockGuideManager.Instance.CloseGuide();
				yield break;
			}
			if (config.ClickMaskNext == GuideNextEvent.ClickTarget)
			{
				var target = UnlockGuideManager.Instance.GetStepTarget(currentStep);
				searchDuration = 0;
				while (target == null || !target.gameObject.activeInHierarchy)
				{
					yield return null;
					searchDuration+= Time.deltaTime;
					if (searchDuration >= 100f)
					{
						UnlockGuideManager.Instance.CloseGuide();
						yield break;
					}
					target = UnlockGuideManager.Instance.GetStepTarget(currentStep);
				}
					
				yield return new WaitForSeconds(0.1f);
				target = UnlockGuideManager.Instance.GetStepTarget(currentStep);
				if (target == null || !target.gameObject.activeInHierarchy)
				{
					yield break;
				}
				SetTarget(target);
				AddCanvas(target);
				this.Finger.gameObject.SetActive(true);
			}
			else if (config.ClickMaskNext == GuideNextEvent.ClickMask)
			{
				this.NextBtn.gameObject.SetActive(true);
				this.NextBtn.onClick.RemoveAllListeners();
				this.NextBtn.onClick.AddListener(() =>
				{
					UnlockGuideManager.Send(UnlockGuideManager.Instance.CurrentModuleFunction, currentStep);
				});
				
				var target = UnlockGuideManager.Instance.GetStepTarget(currentStep);
				searchDuration = 0;
				while (target == null)
				{
					yield return null;
					searchDuration+= Time.deltaTime;
					if (searchDuration >= 100f)
					{
						UnlockGuideManager.Instance.CloseGuide();
						yield break;
					}
					target = UnlockGuideManager.Instance.GetStepTarget(currentStep);
				}
					
				yield return new WaitForSeconds(0.1f);
				SetTarget(target);
			}
		
		

			
			// this.Dialoge.localScale = Vector3.zero;
			this.Finger.anchoredPosition = Maskabled.anchoredPosition;
		

			if (config.ShowDialoge == GuideShowType.Show)
			{
				this.Dialoge.gameObject.SetActive(true);
				this.Dialoge.anchoredPosition = new Vector2(0,Maskabled.anchoredPosition.y) + new Vector2(config.DialogOffset.X,config.DialogOffset.Y );
				this.ContentText.text = config.DialogText;
				this.Dialoge.DOScale(Vector3.one, 0.3f).SetUpdate(true);
			}
		}

		private Canvas lastCanvas = null;
		private GraphicRaycaster lastGraphicRaycaster = null;
		Vector2 Pivot = new Vector2(0.5f, 0.5f);
		void SetTarget(RectTransform target)
		{
			target.Layer("UI");
// 1. 无论 target 的锚点(Anchors)在哪里，target.rect 始终代表其本地矩形范围
			// 使用 rect.size 乘以 localScale 得到缩放后的视觉尺寸
			Vector2 actualSize = target.rect.size;
			Maskabled.sizeDelta = actualSize * target.localScale;

			// 2. 关键：计算 target 的几何中心在世界坐标系中的位置
			// target.rect.center 是相对于 pivot 的偏移，TransformPoint 将其转为世界坐标
			Vector3 targetCenterWorld = target.TransformPoint(target.rect.center);

			// 3. 将 Maskabled 的世界坐标对齐到这个几何中心
			// 注意：这要求 Maskabled 的 Pivot 必须是 (0.5, 0.5)
			Maskabled.position = targetCenterWorld;

		
		}

		public void AddCanvas(RectTransform target)
		{
			lastCanvas = target.GetOrAddComponent<Canvas>();
			lastCanvas.overrideSorting = true;
			lastCanvas.sortingOrder = mParentCanvas.sortingOrder+10;
			lastGraphicRaycaster = target.GetOrAddComponent<GraphicRaycaster>();
		}

		public void ToNextStep()
		{
			if (lastGraphicRaycaster != null)
			{
				this.Finger.gameObject.SetActive(false);
				Object.DestroyImmediate(lastGraphicRaycaster);
				Object.DestroyImmediate(lastCanvas);
				lastGraphicRaycaster = null;
				lastCanvas = null;
			}

		}

		public void RefreshCurrentTarget()
		{
			if (UnlockGuideManager.Instance == null)
			{
				return;
			}

			var target = UnlockGuideManager.Instance.GetStepTarget(currentStep);
			if (target == null || !target.gameObject.activeInHierarchy)
			{
				return;
			}

			ToNextStep();
			SetTarget(target);
			AddCanvas(target);
			this.Finger.gameObject.SetActive(true);
			this.Finger.anchoredPosition = Maskabled.anchoredPosition;
		}

		public void RunNextStep()
		{
			if (!gameObject.activeSelf)
			{
				return;
			}
			currentStep++;
			StartCoroutine(StartGuide());
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
			LogTool.Log("关闭");
		}
		
		protected override void OnClose()
		{
		}
	}
}
