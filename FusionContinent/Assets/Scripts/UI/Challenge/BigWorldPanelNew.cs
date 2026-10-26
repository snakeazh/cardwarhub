using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Model;
using FusionContinent.Sounds;
using FusionContinent.UI.Components;
using Game.Challenge;
using Manager;
using QFramework;
using UIFramework;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;
using UILevel = UIFramework.UILevel;
using UIManager = UIFramework.UIManager;
using Game.RedDot;

namespace FusionContinent
{
	[PrefabPath("BigWorldPanel")]
	public class BigWorldPanelNew : UIPanel<BigWorldPanelViewModel>
	{
		private BigWorldPanel mOldPanel;
		private Sequence mShowSeq;

		void Awake()
		{
			mOldPanel = GetComponent<BigWorldPanel>();
			if (mOldPanel == null)
			{
				LogTool.LogError("[BigWorldPanelNew] 找不到 BigWorldPanel 组件");
			}
		}

		protected override void OnBindViewModel(BigWorldPanelViewModel vm)
		{
			if (mOldPanel == null) return;

			InitButtonIcons();

			mOldPanel.EndlessTowerBtn.onClick.AddListener(OnEndlessTowerBtnClicked);
			mOldPanel.MonsterNotesBtn.onClick.AddListener(OnMonsterNotesBtnClicked);
			mOldPanel.StationBtn.onClick.AddListener(OnStationBtnClicked);
			mOldPanel.RankingBtn.onClick.AddListener(OnRankingBtnClicked);
			mOldPanel.ChallengeBtn.onClick.AddListener(OnNotOpenBtnClicked);
			mOldPanel.GameLobbyBtn.onClick.AddListener(OnGameLobbyBtnClicked);
			// mOldPanel.Locked.onClick.AddListener(OnNotOpenBtnClicked);

			// 单机化：移除 PvP 入口
			mOldPanel.ArenaBtn.gameObject.SetActive(false);

			mOldPanel.MonsterNotesBtn.BindRedDot(RedNodeType.MonsterAtlasBtn);
			mOldPanel.StationBtn.BindRedDot(RedNodeType.StationBtn);

			vm.OnOpenChallengeBoss += OnOpenChallengeBoss;
			vm.OnOpenEndlessTower += OnOpenEndlessTower;
			vm.OnOpenMonsterAtlas += OnOpenMonsterAtlas;
			vm.OnOpenStation += OnOpenStation;
			vm.OnOpenRanking += OnOpenRanking;
		}

		protected override void OnUnbindViewModel(BigWorldPanelViewModel vm)
		{
			if (mOldPanel == null) return;

			mOldPanel.EndlessTowerBtn.onClick.RemoveListener(OnEndlessTowerBtnClicked);
			mOldPanel.MonsterNotesBtn.onClick.RemoveListener(OnMonsterNotesBtnClicked);
			mOldPanel.StationBtn.onClick.RemoveListener(OnStationBtnClicked);
			mOldPanel.RankingBtn.onClick.RemoveListener(OnRankingBtnClicked);
			mOldPanel.ChallengeBtn.onClick.RemoveListener(OnNotOpenBtnClicked);
			mOldPanel.GameLobbyBtn.onClick.RemoveListener(OnGameLobbyBtnClicked);
			// mOldPanel.Locked.onClick.RemoveListener(OnNotOpenBtnClicked);

			vm.OnOpenChallengeBoss -= OnOpenChallengeBoss;
			vm.OnOpenEndlessTower -= OnOpenEndlessTower;
			vm.OnOpenMonsterAtlas -= OnOpenMonsterAtlas;
			vm.OnOpenStation -= OnOpenStation;
			vm.OnOpenRanking -= OnOpenRanking;
		}

		private void OnEndlessTowerBtnClicked()
		{
			UnlockGuideManager.Send(ModuleFunctionType.EndlessTower, 2);
			VM.OnEndlessTowerCommand.Execute();
		}

		private void OnMonsterNotesBtnClicked()
		{
			VM.OnMonsterAtlasCommand.Execute();
		}

		private void OnStationBtnClicked()
		{
			VM.OnStationCommand.Execute();
		}

		private void OnRankingBtnClicked()
		{
			VM.OnRankingCommand.Execute();
		}

		private void OnNotOpenBtnClicked()
		{
			Toast.Show("暂未开放");
		}

		private void OnGameLobbyBtnClicked()
		{
			var vm = new GameLobbyPanelViewModel();
			vm.OnCreate();
			UIManager.OpenPanelAsync<GameLobbyPanelNew, GameLobbyPanelViewModel>(
				vm, UILevel.PopUI).Forget();
		}

		private void OnOpenStation()
		{
			var stationVm = new Game.Station.StationPanelViewModel();
			stationVm.OnCreate();
			UIManager.OpenPanelAsync<StationPanelNew, Game.Station.StationPanelViewModel>(
				stationVm, UILevel.PopUI).Forget();
		}

		private void OnOpenChallengeBoss()
		{
			UnlockGuideManager.Send(ModuleFunctionType.WorldBoss, 2);
			UIKit.OpenPanel<ChallengeBossPanel>();
		}

		private void OnOpenEndlessTower()
		{
			var realmVm = new Game.Realm.RealmRoomViewModel();
			realmVm.OnCreate();
			UIManager.OpenPanelAsync<RealmRoomNew, Game.Realm.RealmRoomViewModel>(
				realmVm, UILevel.Panel).Forget();
		}

		private void OnOpenMonsterAtlas()
		{
			var atlasVm = new MonsterAtlasPanelViewModel();
			atlasVm.OnCreate();
			UIManager.OpenPanelAsync<MonsterAtlasPanelNew, MonsterAtlasPanelViewModel>(
				atlasVm, UILevel.PopUI).Forget();
		}

		private void OnOpenRanking()
		{
			var rankingVm = new ChallengeRankingPanelViewModel();
			rankingVm.OnCreate();
			UIManager.OpenPanelAsync<ChallengeRankingPanelNew, ChallengeRankingPanelViewModel>(
				rankingVm, UILevel.Panel).Forget();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			UIAnimationUtils.PlayAnimations(this.mOldPanel.TweenTarget, () =>
			{
				if (UnlockGuideManager.Instance != null)
				{
					UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EndlessTower, 2, mOldPanel.EndlessTowerBtn.GetComponent<RectTransform>());
				}
			});
		}

		protected override void OnShow()
		{
			InitButtons();
			SoundManager.Instance.PlayBgm(SoundType.SoundWorldBGM);
			if (mShowSeq != null)
			{
				mShowSeq.Restart();
			}
			UIAnimationUtils.PlayAnimations(this.mOldPanel.TweenTarget, () =>
			{
				if (UnlockGuideManager.Instance != null)
				{
					UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EndlessTower, 2, mOldPanel.EndlessTowerBtn.GetComponent<RectTransform>());
				}
			});
			// GameServiceManager.GetOrCreate<Game.Station.StationService>().RefreshClaimableCacheAsync().Forget();
		}

		protected override void OnHide()
		{
			SoundManager.Instance.PlayBgm(SoundType.SoundMainBGM);
		}

		// private void InitBtnIcon(){}

		private void InitButtonIcons()
		{
			SetButtonIcon(mOldPanel.EndlessTowerBtn, ModuleFunctionType.EndlessTower);
			SetButtonIcon(mOldPanel.MonsterNotesBtn, ModuleFunctionType.MonsterNote);
			SetButtonIcon(mOldPanel.StationBtn, ModuleFunctionType.Rest);
			SetButtonIcon(mOldPanel.RankingBtn, ModuleFunctionType.Ranking);
			SetButtonIcon(mOldPanel.ChallengeBtn, ModuleFunctionType.WorldBoss);
		}

		private void SetButtonIcon(UIPointerClickAndLongPress button, ModuleFunctionType moduleType)
		{
			var cfg = DataConfigManager.Tables.TbModuleFunctionConfig.DataList.Find(ele => ele.ModuleType == moduleType);
			if (cfg == null)
			{
				LogTool.LogError($"[BigWorldPanelNew] ModuleFunctionConfig 未配置: {moduleType}");
				return;
			}

			var iconTransform = button.transform.Find("Icon");
			if (iconTransform == null)
			{
				LogTool.LogError($"[BigWorldPanelNew] {button.name} 缺少 Icon 子节点");
				return;
			}

			iconTransform.GetComponent<Image>().sprite = Global.BigWordIconAtlas.GetSprite(cfg.WorldIcon);
		}

		private void InitAnimation()
		{
			var buttons = new List<UIPointerClickAndLongPress>
			{
				mOldPanel.EndlessTowerBtn,
				mOldPanel.MonsterNotesBtn,
				mOldPanel.StationBtn
			};

			mShowSeq = DOTween.Sequence();
			float duration = 0.1f;

			for (int i = 0; i < buttons.Count; i++)
			{
				var group = buttons[i].GetComponent<CanvasGroup>();
				var trans = buttons[i].transform;
				mShowSeq.Append(group.DOFade(1f, duration));
				mShowSeq.Join(trans.DOScaleY(1f, duration));
			}
			mShowSeq.SetAutoKill(false);
			mShowSeq.Pause();
		}

		private void InitButtons()
		{
			var buttons = new List<UIPointerClickAndLongPress>
			{
				mOldPanel.EndlessTowerBtn,
				mOldPanel.MonsterNotesBtn,
				mOldPanel.StationBtn
			};

			for (int i = 0; i < buttons.Count; i++)
			{
				var group = buttons[i].GetComponent<CanvasGroup>();
				var trans = buttons[i].transform;
				group.alpha = 0;
				trans.localScale = new Vector3(1f, 0f, 1f);
			}

			if (mShowSeq == null)
			{
				InitAnimation();
			}
		}

		public void OpenRealmRoomPanel()
		{
			if (VM != null)
			{
				VM.OnEndlessTowerCommand.Execute();
			}
		}

		public void OpenChallengeBossPanel()
		{
			if (VM != null)
			{
				VM.OnChallengeBossCommand.Execute();
			}
		}

		public void OpenEndlessTowerPanel()
		{
			if (VM != null)
			{
				VM.OnEndlessTowerCommand.Execute();
			}
		}
	}
}
