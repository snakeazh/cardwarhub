using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core.Enums;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace FusionContinent
{
	public class YooAssetHandleAdapter : IAssetHandle {
		private AssetHandle handle;
		public YooAssetHandleAdapter(AssetHandle h) { handle = h; }
		public UnityEngine.Object AssetObject => handle.AssetObject;
		public float Progress => handle.Progress;
		public bool IsDone => handle.IsDone;
		public IEnumerator Wait() { return handle; }
		public void Release() { handle.Release(); }
	}
	public class YooAssetProvider : IAssetProvider {
		public IAssetHandle LoadAssetSync(string location, Type type) {
			var h = YooAssets.LoadAssetSync<UnityEngine.Object>(location);
			return new YooAssetHandleAdapter(h);
		}
		public IAssetHandle LoadAssetAsync(string location, Type type) {
			var h = YooAssets.LoadAssetAsync<UnityEngine.Object>(location);
			return new YooAssetHandleAdapter(h);
		}
	}
	public partial class GameStart: MonoBehaviour
	{
		Pipeline resourceInitPipeline;
		
		public bool EditorCodeSimulateMode = true;
		public HotUpdateUI UpdateUI;
		// 在检查 WiFi 前调用
		IEnumerator RequestLocationPermissionThenCheckWifi()
		{
			// Android 6.0 (API 23) 以上需要动态申请
			if (Application.platform == RuntimePlatform.Android )
			{
				if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
				{
					UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
            
					// 等待用户响应
					yield return new WaitForSeconds(0.5f);
					yield return new WaitUntil(() => 
							UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation) || 
							Application.isFocused == false // 用户可能拒绝并切出
					);
				}
			}
		}
		System.Collections.IEnumerator Start()
		{
	
			yield return RequestLocationPermissionThenCheckWifi();
			resourceInitPipeline = new Pipeline();
			yield return ConfigAotManager.Instance.Initialize();
			new GameObject("ChannelManager",typeof(ChannelManager));
			var PlayMode = ConfigAotManager.Instance.GetLocalPlayMode();
#if UNITY_WEBGL && !UNITY_EDITOR
			// WebGL/微信小游戏只支持 WebPlayMode（Offline/Host 模式 YooAsset 不支持 WebGL 平台），忽略配置强制覆盖
			PlayMode = EPlayMode.WebPlayMode;
			EditorCodeSimulateMode = false;
#elif !UNITY_EDITOR
			if (PlayMode == EPlayMode.EditorSimulateMode)
			{
				PlayMode = EPlayMode.OfflinePlayMode;
			}
			EditorCodeSimulateMode = false;
#endif
			ResourceInitContext ctx = new ResourceInitContext();
			ctx.cfgData = ConfigAotManager.Instance.ConfigData;
			ctx.UpdateUI = UpdateUI;
			resourceInitPipeline.AddStep(new ResourceInitStep(PlayMode,ctx,new YooAssetProvider()));
			yield return resourceInitPipeline.Run();

#if UNITY_EDITOR
			// var shader = Shader.Find("TextMeshPro/Distance Field");
			//
			// var assets = YooAssets.LoadAssetSync<TMP_FontAsset>("Fonts_ChironGoRoundTC-700BSDF");
			// yield return assets;
			// (assets.AssetObject as TMP_FontAsset).material.shader = shader;	
#endif

			// DOTween 的安全模式设置 - 全模式触发异常只打印错误信息
			DOTween.safeModeLogBehaviour = SafeModeLogBehaviour.Error;


			// if (Global.User.UserId == 0)
			// {
			// 	if (GameObject.Find("UIRoot/CanvasUpList") != null)
			// 	{
			// 		Destroy(GameObject.Find("UIRoot/CanvasUpList").gameObject);
			// 	}
			// 	UIKit.OpenPanel<LoginPanel01>();
			// }
			// else
			// {
			// 	UIKit.OpenPanel<GameMainPanel>(UILevel.Const);
			// }

			yield return null;
			// UIKit.OpenPanel<LogShow>();
		}
	}
}
