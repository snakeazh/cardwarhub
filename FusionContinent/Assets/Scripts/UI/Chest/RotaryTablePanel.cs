using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent.Manager;
using FusionContinent.Model;
using FusionContinent.Sounds;
using FusionContinent.Utils;
using Manager;
using TMPro;
using Random = UnityEngine.Random;

namespace FusionContinent
{
	public class RotaryTablePanelData : UIPanelData
	{
	}

	public partial class RotaryTablePanel : UIPanel
	{
        [Header("奖励图片父节点")]
        public Transform rewardImgTran;

        [Header("光环Prefab（可重复使用）")]
        public Transform haloPrefab;

        [Header("光环数量")]
        public int haloCount = 2;

        [Header("最初速度(越小越快)")]
        public float startSpeed = 0.08f;

        [Header("最终速度(停止前的间隔时间)")]
        public float endSpeed = 0.35f;

        [Header("基础圈数")]
        public int baseRounds = 3;

        [Header("额外随机圈数范围")]
        public int randomExtraRounds = 2;

        private Transform[] _rewardTransArr;
        private List<int> _availableIndexes;
        private HaloState[] _haloStates;

        private bool _isOnClickPlaying;
        private bool _drawEnd;

        private int _totalScore;
        private Action<int> _callback;
        
        private CityLevelList _cityLevelCfg;
        private TbCityLevelConfig _cityLevelConfig;

        private class HaloState
        {
            public Transform Halo;
            public int HaloIndex;
            public int RewardIndex;
            public bool IsFinished;

            public int TotalSteps;     // 目标总步数（格子数）
            public int CurrentStep;    // 当前已转步数
            public float Speed;        // 当前间隔时间
            public float Timer;        // 计时器
        }
        
        private void Awake()
        {
	        _cityLevelConfig = DataConfigManager.Tables.TbCityLevelConfig;
        }

        void Start()
        {
            DrawBtn.onClick.AddListener(OnClickDrawFun);

            _isOnClickPlaying = false;
            _drawEnd = true;
        }

        public void SetData(Action<int> callback)
        {
	        _drawEnd = true;
	        _totalScore = 0;
	        _callback = callback;
	        _cityLevelCfg = _cityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value).LevelList[Global.CityLevel.Value - 1];
	        if (_cityLevelCfg == null || _callback == null) return;
	        
	        haloCount = RandomTool.RandomRangeTarget(_cityLevelCfg.HaloRandomRange[0], _cityLevelCfg.HaloRandomRange[1]);
	        if (haloCount <= 0) return;
	        
	        // 光标数量
	        NameText.SetData(LocalizationModule.UITexts, "UI_ROTARY_HALO_NUM", haloCount);

	        // 初始化奖励格
	        _rewardTransArr = new Transform[rewardImgTran.childCount];
	        for (int i = 0; i < rewardImgTran.childCount; i++)
	        {
		        var rewTran = rewardImgTran.GetChild(i);
		        rewTran.gameObject.SetActive(i < _cityLevelCfg.RotaryTableRewardData.Count);
		        if (!rewTran.gameObject.activeSelf) continue;
		        
		        var data = _cityLevelCfg.RotaryTableRewardData[i];
		        rewTran.Find("Num").GetComponent<TMP_Text>().text = $"{data.Num}";
		        rewTran.Find("RotaryTablePanelEffect02")?.gameObject.SetActive(false);
		        rewTran.GetComponent<Image>().sprite = Global.RotaryTableIconSpriteAtlas.GetSpriteExt(data.ImageBg);
		        _rewardTransArr[i] = rewTran;
	        }
	        
	        // 生成不重复中奖索引
	        _availableIndexes = new List<int>();
	        for (int i = 0; i < _rewardTransArr.Length; i++)
		        _availableIndexes.Add(i);
	        Shuffle(_availableIndexes);

	        // 初始化光环
	        for (int i = 0; i < HaloParent.childCount; i++)
		        HaloParent.GetChild(i).gameObject.SetActive(false);
		        
	        _haloStates = new HaloState[haloCount];
	        for (int i = 0; i < haloCount; i++)
	        {
		        Transform halo = i >= HaloParent.childCount 
			        ? Instantiate(haloPrefab, HaloParent)
			        : HaloParent.GetChild(i);
		        
		        halo.gameObject.SetActive(true);
		        var haloIndex = _availableIndexes[Mathf.Min(i, _availableIndexes.Count - 1)];
		        halo.position = _rewardTransArr[haloIndex].position;

		        _haloStates[i] = new HaloState()
		        {
			        Halo = halo,
			        HaloIndex = haloIndex,
			        RewardIndex = 0,
			        Speed = startSpeed,
			        Timer = 0,
			        CurrentStep = 0,
			        IsFinished = true
		        };
	        }
	        
	        SetScore(0);
	        DrawBtn.interactable = rewardImgTran.childCount <= _cityLevelCfg.RotaryTableRewardData.Count;
	        // GamePlayManager.Default?.TriggerGuide(GuideType.FirstSpinPopUp); // 首次弹出转盘
        }

        void OnClickDrawFun()
        {
	        if (_isOnClickPlaying) return;
	        Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS,GuideType.FirstSpinPopUp);
			SoundManager.Instance.PlayVoice(SoundType.SoundLottery);
	        DrawBtn.interactable = false;
	        _isOnClickPlaying = true;
	        _drawEnd = false;
	        _totalScore = 0;
	        SetScore(0);

	        // 洗牌
	        Shuffle(_availableIndexes);

	        // 初始化每个光环
	        for (int i = 0; i < _haloStates.Length; i++)
	        {
		        HaloState halo = _haloStates[i];
		        halo.IsFinished = false;
		        // halo.HaloIndex = 0;
		        halo.CurrentStep = 0;
		        halo.Speed = startSpeed;
		        halo.Timer = 0;

		        halo.RewardIndex = _availableIndexes[Mathf.Min(i, _availableIndexes.Count - 1)];

		        // 计算需要的总步数（圈数 + 中奖格偏移）
		        int totalRounds = baseRounds + Random.Range(0, randomExtraRounds + 1);
		        halo.TotalSteps = totalRounds * _rewardTransArr.Length + halo.RewardIndex;

		        LogTool.Log($"🎲 光环[{i}] 目标索引 {halo.RewardIndex}, 总步数 {halo.TotalSteps}");
	        }
        }

        void Update()
        {
            if (_drawEnd) return;

            bool allFinished = true;

            foreach (var halo in _haloStates)
            {
                if (halo.IsFinished) continue;
                allFinished = false;

                halo.Timer += Time.deltaTime;
                if (halo.Timer >= halo.Speed)
                {
                    halo.Timer = 0;
                    halo.HaloIndex++;
                    if (halo.HaloIndex >= _rewardTransArr.Length)
                        halo.HaloIndex = 0;

                    // 播放滚动特效
                    var rewTran = _rewardTransArr[halo.HaloIndex];
                    var rotarylEffect01 = rewTran.Find("RotaryTablePanelEffect01");
                    if (rotarylEffect01 != null)
                    {
	                    rotarylEffect01.gameObject.SetActive(true);
	                    DOVirtual.DelayedCall(0.4f, () =>
	                    {
		                    rotarylEffect01.gameObject.SetActive(false);
	                    });
                    }

                    halo.Halo.position = rewTran.position;
                    halo.CurrentStep++;

                    // 计算剩余步数（用于平滑减速）
                    int remainSteps = halo.TotalSteps - halo.CurrentStep;

                    // 进入减速阶段：剩余20格以内逐步增加间隔
                    if (remainSteps < 20)
                    {
                        float t = Mathf.InverseLerp(20, 0, remainSteps); // 0~1
                        halo.Speed = Mathf.Lerp(startSpeed, endSpeed, t);
                    }
                    else
                    {
	                    // 开始加速
	                    halo.Speed -= 0.001f;
                    }

                    // 到达目标
                    if (halo.CurrentStep >= halo.TotalSteps)
                    {
                        halo.IsFinished = true;
                        halo.Halo.DOScale(1.2f, 0.3f).SetLoops(2, LoopType.Yoyo);
                        LogTool.Log($"光环[{halo.Halo.name}] 停止在奖品索引 {halo.RewardIndex}");
                        
                        // 中奖循环特效
                        var rotarylEffect02 = rewTran.Find("RotaryTablePanelEffect02");
                        if (rotarylEffect02 != null)
	                        rotarylEffect02.gameObject.SetActive(true);

                        // 设置中奖积分
                        var rewNum = _rewardTransArr[halo.HaloIndex].Find("Num").GetComponent<TMP_Text>().text;
                        SetScore(int.Parse(rewNum));
                    }
                }
            }

            if (allFinished)
            {
                _drawEnd = true;
                _isOnClickPlaying = false;

                // 抽奖结束，回调总分数
                OnAllFinished();
            }
        }

        void OnAllFinished()
        {
	        DOVirtual.DelayedCall(1f, () =>
	        {
		        GamePlayManager.Default?.StartGame();
		        _callback(_totalScore);
		        OnClose();
	        });
        }

        // 洗牌算法
        void Shuffle(List<int> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rnd = Random.Range(0, list.Count);
                (list[i], list[rnd]) = (list[rnd], list[i]);
            }
        }

        void SetScore(int score)
        {
	        // 增加特效
	        if (score > _totalScore)
	        {
		        CoinGainEffect01.gameObject.SetActive(true);
		        DOVirtual.DelayedCall(1.5f, () =>
		        {
			        CoinGainEffect01.gameObject.SetActive(false);
		        });
		        
		        // 播放声音 - 金币获得
		        // AudioKit.PlayMusic("resources://Audio/fx_goldDrop", false);
	        }

	        _totalScore += score;
	        ScoreText.GetComponent<RollingNumberManager>().Play(_totalScore);
        }


        protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as RotaryTablePanelData ?? new RotaryTablePanelData();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			GamePlayManager.Default?.PauseGame();
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			gameObject.SetActive(false);
		}
	}
}
