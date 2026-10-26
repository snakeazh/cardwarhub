using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// 可绑定属性（简化版，不依赖 UIFramework）
    /// </summary>
    public class BindableProperty<T>
    {
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    T oldValue = _value;
                    _value = value;
                    OnValueChanged?.Invoke(oldValue, value);
                }
            }
        }
        
        public event Action<T, T> OnValueChanged;
        
        public BindableProperty(T defaultValue = default)
        {
            _value = defaultValue;
        }
    }

    /// <summary>
    /// 音频设置数据
    /// 使用PlayerPrefs持久化
    /// </summary>
    public class AudioSettings
    {
        private const string PREFS_KEY = "GameAudioSettings_v1";
        
        public BindableProperty<float> MasterVolume { get; } = new BindableProperty<float>(1f);
        public BindableProperty<float> BGMVolume { get; } = new BindableProperty<float>(1f);
        public BindableProperty<float> SFXVolume { get; } = new BindableProperty<float>(1f);
        public BindableProperty<float> VoiceVolume { get; } = new BindableProperty<float>(1f);
        public BindableProperty<float> AmbientVolume { get; } = new BindableProperty<float>(1f);
        public BindableProperty<bool> MasterMute { get; } = new BindableProperty<bool>(false);
        
        [Serializable]
        private class SettingsData
        {
            public float MasterVolume = 1f;
            public float BGMVolume = 1f;
            public float SFXVolume = 1f;
            public float VoiceVolume = 1f;
            public float AmbientVolume = 1f;
            public bool MasterMute = false;
        }
        
        public void Save()
        {
            var data = new SettingsData
            {
                MasterVolume = MasterVolume.Value,
                BGMVolume = BGMVolume.Value,
                SFXVolume = SFXVolume.Value,
                VoiceVolume = VoiceVolume.Value,
                AmbientVolume = AmbientVolume.Value,
                MasterMute = MasterMute.Value
            };
            
            try
            {
                string json = JsonConvert.SerializeObject(data);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
                
                LogTool.Log("[AudioSettings] 设置已保存");
            }
            catch (Exception e)
            {
                LogTool.LogError($"[AudioSettings] 保存失败: {e.Message}");
            }
        }
        
        public void Load()
        {
            if (!PlayerPrefs.HasKey(PREFS_KEY))
            {
                LogTool.Log("[AudioSettings] 使用默认设置");
                return;
            }
            
            string json = PlayerPrefs.GetString(PREFS_KEY);
            if (string.IsNullOrEmpty(json)) return;
            
            try
            {
                var data = JsonConvert.DeserializeObject<SettingsData>(json);
                
                MasterVolume.Value = data.MasterVolume;
                BGMVolume.Value = data.BGMVolume;
                SFXVolume.Value = data.SFXVolume;
                VoiceVolume.Value = data.VoiceVolume;
                AmbientVolume.Value = data.AmbientVolume;
                MasterMute.Value = data.MasterMute;
                
                LogTool.Log("[AudioSettings] 设置已加载");
            }
            catch (Exception e)
            {
                LogTool.LogError($"[AudioSettings] 加载失败: {e.Message}，使用默认设置");
                Reset();
            }
        }
        
        public void Reset()
        {
            MasterVolume.Value = 1f;
            BGMVolume.Value = 1f;
            SFXVolume.Value = 1f;
            VoiceVolume.Value = 1f;
            AmbientVolume.Value = 1f;
            MasterMute.Value = false;
            
            Save();
            
            LogTool.Log("[AudioSettings] 设置已重置");
        }
    }
}
