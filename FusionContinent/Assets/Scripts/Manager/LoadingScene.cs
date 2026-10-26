using System;
using Config;
using FusionContinent;
using FusionContinent.Helper;
using FusionContinent.Sounds;
using YooAsset;

namespace Manager
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.SceneManagement;
    using TMPro;
    using System.Collections;

    public class LoadingScene : MonoBehaviour
    {
        [Header("UI 组件")]
        public Slider progressBar;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI tipsText;
        
        public RectTransform tips;
        public Image loadingCircle;
        public Image IconImage;

        [Header("参数设置")]
        public string targetScene = "";
        public float smoothSpeed = 3f;      // 进度条平滑速度
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

        void Start()
        {
            targetScene = SceneLoader.TargetScene;
            tipsText.text = _tipList[Random.Range(0, _tipList.Length)];
            StartCoroutine(LoadSceneAsync());
            
            var loadingCfg = _loadingConfig.DataList[Random.Range(0, _loadingConfig.DataList.Count)];
            if (loadingCfg != null)
            {
                SpriteHelper.TryLoadTextureAsync(loadingCfg.Icon, IconImage);
                tips.GetChild(0).GetComponent<TMP_Text>().text = $"{loadingCfg.TipText1}";
                tips.GetChild(1).GetComponent<TMP_Text>().text = $"{loadingCfg.TipText2}";
            }
        }

        void Update()
        {
            // 转动加载圈动画
            if (loadingCircle != null)
                loadingCircle.transform.Rotate(Vector3.forward, -180 * Time.deltaTime);
        }

        IEnumerator LoadSceneAsync()
        {
            float startTime = Time.time;
            var operation = YooAssets.LoadSceneAsync(targetScene);
            // operation. = false;

            while (!operation.IsDone)
            {
                // Unity 加载进度最大只到 0.9f
                _targetProgress = Mathf.Clamp01(operation.Progress / 0.9f);

                // 平滑插值过渡
                _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime * smoothSpeed);

                progressBar.value = _currentProgress;
                progressText.text = (_currentProgress * 100f).ToString("F1") + "%";

                // 检查是否加载完成
                bool loadFinished = operation.Progress >= 0.9f;
                bool timeEnough = Time.time - startTime >= minLoadingTime;

                if (loadFinished && timeEnough && _currentProgress >= 1f)
                {
                    // 缓动到100%
                    _currentProgress = Mathf.Lerp(_currentProgress, 1f, Time.deltaTime * smoothSpeed);
                    progressBar.value = _currentProgress;
                    progressText.text = "100%";

                    tipsText.text = "加载完成，请稍后...";
                    // operation.allowSceneActivation = true;
                    if (targetScene.Equals("GameStart"))
                    {
                        SoundManager.Instance.PlayBgm(SoundType.SoundMainBGM);
                    }
                }

                yield return null;
            }


        }
    }

}