using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace Game.Audio
{
    /// <summary>
    /// 环境音管理器 - 管理环境背景音
    /// 使用 Unity 原生 AudioSource
    /// </summary>
    public class AmbientAudioManager : MonoBehaviour
    {
        private class AmbientLayer
        {
            public string Name;
            public PooledAudioSource Source;
            public float BaseVolume;
            public bool IsPaused;
        }
        
        private AudioManager _manager;
        private List<AmbientLayer> _layers = new List<AmbientLayer>();
        private bool _isPaused;
        
        public void Init(AudioManager manager)
        {
            _manager = manager;
        }
        
        public async UniTask AddLayerAsync(string ambientName, float volume = 1f, bool loop = true)
        {
            if (string.IsNullOrEmpty(ambientName)) return;
            
            var existingLayer = _layers.Find(l => l.Name == ambientName);
            if (existingLayer != null) return;
            
            var source = _manager.Pool.Get(ambientName, PooledAudioSource.AudioCategory.Ambient);
            source.BaseVolume = volume;
            float finalVolume = _manager.GetFinalVolume(volume * _manager.Settings.AmbientVolume.Value);
            
            var clip = _manager.Pool.GetClip(ambientName);
            if (clip == null)
            {
                clip = await _manager.Pool.LoadClipAsync(ambientName);
            }

            if (this == null || source == null) return;

            if (clip != null)
            {
                source.SetClip(clip);
            }
            
            source.SetLoop(loop);
            source.SetVolume(finalVolume);
            
            var layer = new AmbientLayer
            {
                Name = ambientName,
                Source = source,
                BaseVolume = volume,
                IsPaused = false
            };
            
            _layers.Add(layer);
            source.Play();
        }

        public void AddLayer(string ambientName, float volume = 1f, bool loop = true)
        {
            AddLayerAsync(ambientName, volume, loop).Forget();
        }
        
        public void RemoveLayer(string ambientName)
        {
            var layer = _layers.Find(l => l.Name == ambientName);
            if (layer == null) return;
            
            _manager.Pool.Return(layer.Source);
            _layers.Remove(layer);
        }
        
        public void StopAll()
        {
            foreach (var layer in _layers)
            {
                _manager.Pool.Return(layer.Source);
            }
            _layers.Clear();
        }
        
        public void PauseAll()
        {
            if (_isPaused) return;
            
            foreach (var layer in _layers)
            {
                layer.Source.Pause();
                layer.IsPaused = true;
            }
            _isPaused = true;
        }
        
        public void ResumeAll()
        {
            if (!_isPaused) return;
            
            foreach (var layer in _layers)
            {
                layer.Source.Resume();
                layer.IsPaused = false;
            }
            _isPaused = false;
        }
        
        public void SetLayerVolume(string ambientName, float volume)
        {
            var layer = _layers.Find(l => l.Name == ambientName);
            if (layer == null) return;
            
            layer.BaseVolume = volume;
            float finalVolume = _manager.GetFinalVolume(volume * _manager.Settings.AmbientVolume.Value);
            layer.Source.SetVolume(finalVolume);
        }
        
        public void ApplyVolume()
        {
            ApplyVolumeInternal(_manager.Settings.AmbientVolume.Value);
        }

        public void ApplyVolumeInternal(float volume)
        {
            float finalVolume = _manager.GetFinalVolume(volume);
            
            foreach (var layer in _layers)
            {
                float layerVolume = finalVolume * layer.BaseVolume;
                layer.Source.SetVolume(layerVolume);
            }
        }
        
        public void OnUpdate(float deltaTime)
        {
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                if (_layers[i].Source == null || !_layers[i].Source.IsPlaying)
                {
                    _layers.RemoveAt(i);
                }
            }
        }
        
        public void OnDestroy()
        {
            StopAllCoroutines();
            StopAll();
        }
    }
}
