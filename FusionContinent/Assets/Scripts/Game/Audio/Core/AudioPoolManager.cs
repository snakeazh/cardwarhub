using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace Game.Audio
{
    /// <summary>
    /// 音频对象池管理器
    /// 使用 Unity 原生 AudioSource
    /// </summary>
    public class AudioPoolManager
    {
        public int DefaultPoolSize = 5;
        public int MaxPoolSize = 20;
        public float AutoReleaseTime = 30f;
        
        private Dictionary<string, Queue<PooledAudioSource>> _pools = new Dictionary<string, Queue<PooledAudioSource>>();
        private Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private Dictionary<string, AssetHandle> _handles = new Dictionary<string, AssetHandle>();
        private Dictionary<string, float> _lastUsedTime = new Dictionary<string, float>();
        private List<PooledAudioSource> _activeSources = new List<PooledAudioSource>();
        private Transform _poolRoot;
        
        public AudioPoolManager(Transform poolRoot)
        {
            _poolRoot = poolRoot;
        }
        
        public PooledAudioSource Get(string audioName, PooledAudioSource.AudioCategory category)
        {
            PooledAudioSource source = null;
            if (_pools.TryGetValue(audioName, out var pool) && pool.Count > 0)
            {
                source = pool.Dequeue();
            }
            else
            {
                source = CreateAudioSource(audioName);
            }

            source.Category = category;
            _lastUsedTime[audioName] = Time.time;
            _activeSources.Add(source);
            return source;
        }

        public List<PooledAudioSource> GetActiveSources(PooledAudioSource.AudioCategory category)
        {
            var result = new List<PooledAudioSource>();
            foreach (var source in _activeSources)
            {
                if (source != null && source.Category == category)
                {
                    result.Add(source);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取音频源
        /// </summary>
        public PooledAudioSource Get(string audioName)
        {
            if (_pools.TryGetValue(audioName, out var pool) && pool.Count > 0)
            {
                var source = pool.Dequeue();
                _lastUsedTime[audioName] = Time.time;
                _activeSources.Add(source);
                return source;
            }
            
            var newSource = CreateAudioSource(audioName);
            _lastUsedTime[audioName] = Time.time;
            _activeSources.Add(newSource);
            return newSource;
        }
        
        /// <summary>
        /// 归还音频源到对象池
        /// </summary>
        public void Return(PooledAudioSource source)
        {
            if (source == null) return;
            
            _activeSources.Remove(source);
            
            string audioName = source.AudioName;
            if (string.IsNullOrEmpty(audioName))
            {
                Object.Destroy(source.gameObject);
                return;
            }
            
            if (!_pools.ContainsKey(audioName))
            {
                _pools[audioName] = new Queue<PooledAudioSource>();
            }
            
            if (_pools[audioName].Count < MaxPoolSize)
            {
                source.Stop();
                source.gameObject.SetActive(false);
                _pools[audioName].Enqueue(source);
            }
            else
            {
                Object.Destroy(source.gameObject);
            }
            
            _lastUsedTime[audioName] = Time.time;
        }
        
        /// <summary>
        /// 异步加载音频:加载后 clip 与 handle 一并缓存,缓存移除时 handle 随之 Release
        /// </summary>
        public async UniTask<AudioClip> LoadClipAsync(string audioName)
        {
            if (_cache.TryGetValue(audioName, out var cached)) return cached;

            var handle = YooAssets.LoadAssetAsync<AudioClip>(audioName);
            await handle.Task;

            var clip = handle.AssetObject as AudioClip;
            if (clip == null)
            {
                handle.Release();
                return null;
            }

            // 并发加载同名音频时只保留先完成的句柄
            if (_cache.TryGetValue(audioName, out var existing))
            {
                handle.Release();
                return existing;
            }

            _cache[audioName] = clip;
            _handles[audioName] = handle;
            return clip;
        }

        /// <summary>
        /// 异步预加载音频
        /// </summary>
        public async UniTask PreloadAsync(List<string> audioNames)
        {
            foreach (var name in audioNames)
            {
                await PreloadSingleAsync(name);
            }
        }

        private async UniTask PreloadSingleAsync(string audioName)
        {
            if (_cache.ContainsKey(audioName)) return;

            var clip = await LoadClipAsync(audioName);
            if (clip != null)
            {
                if (!_pools.ContainsKey(audioName))
                {
                    _pools[audioName] = new Queue<PooledAudioSource>();
                }

                while (_pools[audioName].Count < DefaultPoolSize)
                {
                    var source = CreateAudioSource(audioName);
                    source.gameObject.SetActive(false);
                    _pools[audioName].Enqueue(source);
                }

                LogTool.Log($"[AudioPoolManager] 预加载完成: {audioName}");
            }
        }
        
        /// <summary>
        /// 获取缓存的音频剪辑
        /// </summary>
        public AudioClip GetClip(string audioName)
        {
            if (_cache.TryGetValue(audioName, out var clip))
            {
                return clip;
            }
            return null;
        }
        
        /// <summary>
        /// 创建音频源
        /// </summary>
        private PooledAudioSource CreateAudioSource(string audioName)
        {
            var go = new GameObject($"AudioSource_{audioName}");
            go.transform.SetParent(_poolRoot);
            
            var audioSource = go.AddComponent<AudioSource>();
            var pooledSource = go.AddComponent<PooledAudioSource>();
            pooledSource.Initialize(audioSource, audioName, this);
            
            // 设置音频剪辑
            if (_cache.TryGetValue(audioName, out var clip))
            {
                audioSource.clip = clip;
            }
            
            return pooledSource;
        }
        
        /// <summary>
        /// 移除缓存并释放对应 YooAsset 句柄
        /// </summary>
        private void ReleaseClip(string audioName)
        {
            if (_handles.TryGetValue(audioName, out var handle))
            {
                handle.Release();
                _handles.Remove(audioName);
            }
            _cache.Remove(audioName);
        }

        /// <summary>
        /// 清理对象池
        /// </summary>
        public void Clear()
        {
            foreach (var source in _activeSources)
            {
                if (source != null)
                {
                    Object.Destroy(source.gameObject);
                }
            }
            _activeSources.Clear();
            
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var source = pool.Dequeue();
                    if (source != null)
                    {
                        Object.Destroy(source.gameObject);
                    }
                }
            }
            
            _pools.Clear();
            foreach (var key in new List<string>(_cache.Keys))
            {
                ReleaseClip(key);
            }
            _lastUsedTime.Clear();
        }
        
        /// <summary>
        /// 自动清理长时间未使用的音频
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            var currentTime = Time.time;
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _lastUsedTime)
            {
                // BGM固定节点不参与AutoReleaseTime清理
                if (kvp.Key == "__BGM_SOURCE_A__" || kvp.Key == "__BGM_SOURCE_B__")
                {
                    continue;
                }

                if (currentTime - kvp.Value > AutoReleaseTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                if (_pools.TryGetValue(key, out var pool))
                {
                    while (pool.Count > 0)
                    {
                        var source = pool.Dequeue();
                        if (source != null)
                        {
                            Object.Destroy(source.gameObject);
                        }
                    }
                    
                    _pools.Remove(key);
                    ReleaseClip(key);
                    _lastUsedTime.Remove(key);
                    
                    LogTool.Log($"[AudioPoolManager] 自动清理: {key}");
                }
            }
            
            // 清理已停止的活跃音频源
            for (int i = _activeSources.Count - 1; i >= 0; i--)
            {
                var source = _activeSources[i];
                if (source == null)
                {
                    _activeSources.RemoveAt(i);
                    continue;
                }

                // 如果是非循环音效，且不在播放也不在暂停状态，则自动回收
                if (!source.IsLoop && !source.IsPlaying && !source.IsPaused && source.gameObject.activeInHierarchy)
                {
                    Return(source);
                }
            }
        }
    }
    
    /// <summary>
    /// 池化音频源组件
    /// </summary>
    public class PooledAudioSource : MonoBehaviour
    {
        public enum AudioCategory
        {
            BGM,
            SFX,
            Voice,
            Ambient
        }

        private AudioSource _audioSource;
        private AudioPoolManager _poolManager;
        private string _audioName;
        private bool _isPaused;
        
        public AudioCategory Category { get; set; }
        public string AudioName => _audioName;
        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;
        public float Volume => _audioSource != null ? _audioSource.volume : 0f;
        public bool IsLoop => _audioSource != null && _audioSource.loop;
        public bool IsPaused => _isPaused;
        public float BaseVolume { get; set; } = 1f;
        
        public void Initialize(AudioSource audioSource, string audioName, AudioPoolManager poolManager)
        {
            _audioSource = audioSource;
            _audioName = audioName;
            _poolManager = poolManager;
            _isPaused = false;
            BaseVolume = 1f;
        }
        
        public void SetClip(AudioClip clip)
        {
            if (_audioSource != null)
            {
                _audioSource.clip = clip;
            }
        }
        
        public void SetVolume(float volume)
        {
            if (_audioSource != null)
            {
                _audioSource.volume = volume;
            }
        }
        
        public void SetPitch(float pitch)
        {
            if (_audioSource != null)
            {
                _audioSource.pitch = pitch;
            }
        }
        
        public void SetLoop(bool loop)
        {
            if (_audioSource != null)
            {
                _audioSource.loop = loop;
            }
        }
        
        public void Play()
        {
            if (_audioSource != null && _audioSource.clip != null)
            {
                gameObject.SetActive(true);
                _audioSource.Play();
                _isPaused = false;
            }
        }
        
        public void Stop()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }
        
        public void Pause()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Pause();
                _isPaused = true;
            }
        }
        
        public void Resume()
        {
            if (_audioSource != null && _isPaused)
            {
                _audioSource.UnPause();
                _isPaused = false;
            }
        }
    }
}
