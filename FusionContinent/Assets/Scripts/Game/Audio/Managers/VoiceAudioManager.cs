using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Game.Audio
{
    /// <summary>
    /// 语音管理器 - 管理游戏语音
    /// 使用 Unity 原生 AudioSource
    /// </summary>
    public class VoiceAudioManager : MonoBehaviour
    {
        private AudioManager _manager;
        private PooledAudioSource _currentSource;
        private string _currentName;
        private bool _isPaused;
        
        public void Init(AudioManager manager)
        {
            _manager = manager;
        }
        
        public void Play(string voiceName, float volume = 1f, float speed = 1f, bool canInterrupt = true)
        {
            if (string.IsNullOrEmpty(voiceName)) return;
            
            if (!canInterrupt && _currentSource != null && _currentSource.IsPlaying)
            {
                return;
            }
            
            _currentSource = _manager.Pool.Get(voiceName, PooledAudioSource.AudioCategory.Voice);
            _currentName = voiceName;
            _isPaused = false;
            _currentSource.BaseVolume = volume;
            
            StartCoroutine(LoadAndPlay(_currentSource, voiceName, volume, speed));
        }
        
        private IEnumerator LoadAndPlay(PooledAudioSource source, string voiceName, float volume, float speed)
        {
            if (this == null || !gameObject.activeInHierarchy) yield break;

            var clip = _manager.Pool.GetClip(voiceName);
            if (clip == null)
            {
                yield return _manager.Pool.LoadClipAsync(voiceName).ToCoroutine(c => clip = c);
            }
            
            if (clip != null && source != null)
            {
                float finalVolume = _manager.GetFinalVolume(volume * _manager.Settings.VoiceVolume.Value);
                source.SetClip(clip);
                source.SetLoop(false);
                source.SetPitch(speed);
                source.SetVolume(finalVolume);
                source.Play();
            }
        }
        
        public void Stop()
        {
            if (_currentSource == null) return;
            
            _manager.Pool.Return(_currentSource);
            _currentSource = null;
            _currentName = null;
        }
        
        public void Pause()
        {
            if (_currentSource == null || _isPaused) return;
            
            _currentSource.Pause();
            _isPaused = true;
        }
        
        public void Resume()
        {
            if (_currentSource == null || !_isPaused) return;
            
            _currentSource.Resume();
            _isPaused = false;
        }
        
        public void ApplyVolume()
        {
            ApplyVolumeInternal(_manager.Settings.VoiceVolume.Value);
        }

        public void ApplyVolumeInternal(float volume)
        {
            if (_currentSource == null) return;
            
            float finalVolume = _manager.GetFinalVolume(volume);
            _currentSource.SetVolume(finalVolume * _currentSource.BaseVolume);
        }
        
        private IEnumerator PlayWithDelay(string voiceName, float volume, float speed, float delay, bool canInterrupt)
        {
            yield return new WaitForSeconds(delay);
            
            if (this == null || !gameObject.activeInHierarchy) yield break;
            
            Play(voiceName, volume, speed, canInterrupt);
        }

        public void OnUpdate(float deltaTime)
        {
            if (_currentSource != null
                && _currentSource.gameObject.activeInHierarchy
                && !_currentSource.IsPlaying
                && !_isPaused)
            {
                _currentSource = null;
                _currentName = null;
            }
        }
        
        public void OnDestroy()
        {
            StopAllCoroutines();
            Stop();
        }
    }
}
