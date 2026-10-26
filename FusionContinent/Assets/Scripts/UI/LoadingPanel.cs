using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent.Helper;
using FusionContinent.Sounds;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Spine.Unity;
using TMPro;
using UnityEngine.SceneManagement;
using YooAsset;

namespace FusionContinent
{
    public class LoadingPanelData : UIPanelData
    {
    }

    public partial class LoadingPanel : UIPanel
    {
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as LoadingPanelData ?? new LoadingPanelData();
            // please add init code here
        }

        [Header("参数设置")] public string targetScene = "";
        public float smoothSpeed = 3f; // 进度条平滑速度
        public float minLoadingTime = 2.0f; // 最少展示时间（秒）

        private float _targetProgress = 0f;
        private float _currentProgress = 0f;
        private bool _readyToActivate = false;

        private readonly string[] _tipList =
        {
            "提示：合理搭配英雄阵容才能取胜！",
            "提示：敌人越强，掉落也越丰厚！",
            "提示：升级技能能显著提升战力！",
            "提示：使用闪避可以避免致命伤害！"
        };

        private TbLoadingConfig _loadingConfig;

        private void Awake()
        {
            _loadingConfig = DataConfigManager.Tables.TbLoadingConfig;
        }

        IEnumerator LoadSceneAsync()
        {
            // 1. 加载 LoadingScene（假设当前就在 LoadingScene，可跳过重复加载）
            var loadingOp = YooAssets.LoadSceneAsync("LoadingScene", LoadSceneMode.Single);
            yield return loadingOp;

            // 2. 开始加载目标场景（Additive）
            float startTime = Time.time;
            var targetOp = YooAssets.LoadSceneAsync(targetScene, LoadSceneMode.Additive);

            // 等待加载完成（资源到 0.9）且时间足够
            bool timeEnough = false;
            while (!targetOp.IsDone || !timeEnough)
            {
                // 关键：检查是否加载失败
                if (targetOp.Status == EOperationStatus.Failed)
                {
                    LogTool.LogError($"场景加载失败: {targetOp.LastError}");
                    yield break; // 直接退出，不要卸载 LoadingScene
                }

                // 进度计算：Unity 加载到 0.9 视为 100%
                _targetProgress = Mathf.Clamp01(targetOp.Progress / 0.9f);
                _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime * smoothSpeed);

                UpdateUI(_currentProgress);

                timeEnough = Time.time - startTime >= minLoadingTime;
                yield return null;
            }

            // 再次确认状态（防止最后一帧失败）
            if (targetOp.Status != EOperationStatus.Succeed)
            {
                LogTool.LogError("场景未成功加载");
                yield break;
            }

            // 3. 平滑过渡到 100%
            while (_currentProgress < 1f)
            {
                _currentProgress = Mathf.Lerp(_currentProgress, 1f, Time.deltaTime * 100f * smoothSpeed);
                UpdateUI(_currentProgress);
                yield return null;
            }

            // 4. 激活场景（必须在卸载 LoadingScene 之前）
            targetOp.ActivateScene();

            // 等待一帧确保场景激活完成（YooAssets 的 Activate 是异步的）
            yield return new WaitForEndOfFrame();

            // 可选：设置活跃场景
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetScene));

            // 5. 安全卸载 LoadingScene
            // 确保此协程不在 LoadingScene 的对象上运行，否则需要先转移到目标场景的对象
            yield return loadingOp.UnloadAsync();
            loadingOp.Release();

            // 后续逻辑...
            if (targetScene.Equals("GameStart"))
            {
                SoundManager.Instance.PlayBgm(SoundType.SoundMainBGM);
            }

            Hide();
        }

        void UpdateUI(float progress)
        {
            progressBar.value = progress;
            ProgressText.text = (progress * 100f).ToString("F1") + "%";
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }

        public void StartLoad()
        {
            _currentProgress = 0f;
            progressBar.value = 0f;

            targetScene = SceneLoader.TargetScene;
            TipText.text = _tipList[Random.Range(0, _tipList.Length)];
            var spine = this.Spine.GetComponentInChildren<SkeletonGraphic>();
            if (spine)
            {
                spine.Initialize(true);
            }

            StartCoroutine(LoadSceneAsync());

            var loadingCfg = _loadingConfig.DataList[Random.Range(0, _loadingConfig.DataList.Count)];
            if (loadingCfg != null)
            {
                // SpriteHelper.TryLoadTexture(loadingCfg.Icon, );
                Tips.GetChild(0).GetComponent<TMP_Text>().text = $"{loadingCfg.TipText1}";
                Tips.GetChild(1).GetComponent<TMP_Text>().text = $"{loadingCfg.TipText2}";
            }
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