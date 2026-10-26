using System;
using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// 音频管理器 - 管理游戏所有音频
    /// 使用 Unity 原生 AudioSource，不依赖外部业务模块
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region 单例

        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[Audio]/AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Initialize();
        }

        #endregion

        #region 子管理器

        public BGMAudioManager BGM { get; private set; }
        public SFXAudioManager SFX { get; private set; }
        public VoiceAudioManager Voice { get; private set; }
        public AmbientAudioManager Ambient { get; private set; }

        #endregion

        #region 核心组件

        public AudioMixer Mixer { get; private set; }
        public AudioSettings Settings { get; private set; }
        public AudioPoolManager Pool { get; private set; }

        #endregion

        #region 事件

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnBGMVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<float> OnVoiceVolumeChanged;
        public event Action<bool> OnMuteChanged;

        #endregion

        #region 初始化

        private void Initialize()
        {
            LogTool.Log("[AudioManager] 初始化...");

            var rootObj = new GameObject("AudioRoot");
            rootObj.transform.SetParent(transform);

            Settings = new AudioSettings();
            Settings.Load();

            Pool = new AudioPoolManager(rootObj.transform);
            Mixer = new AudioMixer(this);

            BGM = gameObject.AddComponent<BGMAudioManager>();
            BGM.Init(this);

            SFX = gameObject.AddComponent<SFXAudioManager>();
            SFX.Init(this);

            Voice = gameObject.AddComponent<VoiceAudioManager>();
            Voice.Init(this);

            Ambient = gameObject.AddComponent<AmbientAudioManager>();
            Ambient.Init(this);

            SubscribeSettings();

            LogTool.Log("[AudioManager] 初始化完成");
        }

        private void SubscribeSettings()
        {
            Settings.MasterVolume.OnValueChanged += (oldVal, newVal) =>
            {
                ApplyMasterVolume();
                OnMasterVolumeChanged?.Invoke(newVal);
            };

            Settings.BGMVolume.OnValueChanged += (oldVal, newVal) =>
            {
                BGM?.ApplyVolume();
                OnBGMVolumeChanged?.Invoke(newVal);
            };

            Settings.SFXVolume.OnValueChanged += (oldVal, newVal) =>
            {
                SFX?.ApplyVolume();
                OnSFXVolumeChanged?.Invoke(newVal);
            };

            Settings.VoiceVolume.OnValueChanged += (oldVal, newVal) =>
            {
                Voice?.ApplyVolume();
                OnVoiceVolumeChanged?.Invoke(newVal);
            };

            Settings.MasterMute.OnValueChanged += (oldVal, newVal) =>
            {
                ApplyMasterVolume();
                OnMuteChanged?.Invoke(newVal);
            };
        }

        private void ApplyMasterVolume()
        {
            BGM?.ApplyVolume();
            SFX?.ApplyVolume();
            Voice?.ApplyVolume();
            Ambient?.ApplyVolume();
        }

        #endregion

        #region 公共方法

        public float GetFinalVolume(float categoryVolume)
        {
            if (Settings.MasterMute.Value) return 0f;
            return Settings.MasterVolume.Value * categoryVolume;
        }

        public void SaveSettings()
        {
            Settings.Save();
        }

        public void StopAll()
        {
            BGM?.Stop();
            SFX?.StopAll();
            Voice?.Stop();
            Ambient?.StopAll();
        }

        public void PauseAll()
        {
            BGM?.Pause();
            SFX?.PauseAll();
            Voice?.Pause();
            Ambient?.PauseAll();
        }

        public void ResumeAll()
        {
            BGM?.Resume();
            SFX?.ResumeAll();
            Voice?.Resume();
            Ambient?.ResumeAll();
        }

        #endregion

        #region 生命周期

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            Pool?.OnUpdate(deltaTime);
            BGM?.OnUpdate(deltaTime);
            SFX?.OnUpdate(deltaTime);
            Voice?.OnUpdate(deltaTime);
            Ambient?.OnUpdate(deltaTime);
        }

        private void OnDestroy()
        {
            StopAll();
            Pool?.Clear();
            Settings?.Save();
        }

        #endregion
    }
}
