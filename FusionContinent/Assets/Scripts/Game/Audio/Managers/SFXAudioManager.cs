using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace Game.Audio
{
    /// <summary>
    /// 音效管理器 - 管理游戏音效
    /// 使用 Unity 原生 AudioSource
    /// </summary>
    public class SFXAudioManager : MonoBehaviour
    {
        public const int MAX_CONCURRENT_SFX = 32;
        private AudioManager _manager;
        private bool _isPaused;
        
        public void Init(AudioManager manager)
        {
            _manager = manager;
        }
        
        public void Play(string sfxName, float volume = 1f, float pitch = 1f)
        {
            if (string.IsNullOrEmpty(sfxName)) return;

            var activeSources = _manager.Pool.GetActiveSources(PooledAudioSource.AudioCategory.SFX);
            if (activeSources.Count >= MAX_CONCURRENT_SFX)
            {
                // 停止并回收最旧的一个
                var oldest = activeSources[0];
                oldest.Stop();
                _manager.Pool.Return(oldest);
            }
            
            var source = _manager.Pool.Get(sfxName, PooledAudioSource.AudioCategory.SFX);
            if (source == null) return;
            
            var clip = _manager.Pool.GetClip(sfxName);
            if (clip == null)
            {
                if (this != null && gameObject.activeInHierarchy)
                {
                    // 异步加载并播放
                    StartCoroutine(LoadAndPlay(source, sfxName, volume, pitch));
                }
                else
                {
                    _manager.Pool.Return(source);
                }
                return;
            }
            
            float finalVolume = _manager.GetFinalVolume(volume * _manager.Settings.SFXVolume.Value);
            source.SetClip(clip);
            source.BaseVolume = volume;
            source.SetVolume(finalVolume);
            source.SetPitch(pitch);
            source.SetLoop(false);
            
            source.Play();
        }
        
        private System.Collections.IEnumerator LoadAndPlay(PooledAudioSource source, string sfxName, float volume, float pitch)
        {
            AudioClip clip = null;
            yield return _manager.Pool.LoadClipAsync(sfxName).ToCoroutine(c => clip = c);

            if (clip != null && source != null)
            {
                float finalVolume = _manager.GetFinalVolume(volume * _manager.Settings.SFXVolume.Value);
                source.SetClip(clip);
                source.BaseVolume = volume;
                source.SetVolume(finalVolume);
                source.SetPitch(pitch);
                source.SetLoop(false);
                
                source.Play();
            }
        }
        
        public void StopAll()
        {
            var activeSources = _manager.Pool.GetActiveSources(PooledAudioSource.AudioCategory.SFX);
            foreach (var source in activeSources)
            {
                if (source != null)
                {
                    source.Stop();
                    _manager.Pool.Return(source);
                }
            }
        }
        
        public void PauseAll()
        {
            if (_isPaused) return;
            
            var activeSources = _manager.Pool.GetActiveSources(PooledAudioSource.AudioCategory.SFX);
            foreach (var source in activeSources)
            {
                source?.Pause();
            }
            _isPaused = true;
        }
        
        public void ResumeAll()
        {
            if (!_isPaused) return;
            
            var activeSources = _manager.Pool.GetActiveSources(PooledAudioSource.AudioCategory.SFX);
            foreach (var source in activeSources)
            {
                source?.Resume();
            }
            _isPaused = false;
        }
        
        public void ApplyVolume()
        {
            ApplyVolumeInternal(_manager.Settings.SFXVolume.Value);
        }

        public void ApplyVolumeInternal(float volume)
        {
            float finalVolume = _manager.GetFinalVolume(volume);
            
            var activeSources = _manager.Pool.GetActiveSources(PooledAudioSource.AudioCategory.SFX);
            foreach (var source in activeSources)
            {
                if (source != null)
                {
                    source.SetVolume(finalVolume * source.BaseVolume);
                }
            }
        }
        
        public async UniTask PreloadAsync(List<string> sfxNames)
        {
            await _manager.Pool.PreloadAsync(sfxNames);
        }

        public void OnUpdate(float deltaTime)
        { 
        }
        
        public void OnDestroy()
        {
            StopAllCoroutines();
            StopAll();
        }
    }
}
