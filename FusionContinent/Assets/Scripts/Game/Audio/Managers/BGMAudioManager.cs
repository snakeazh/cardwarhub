using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Game.Audio
{
    /// <summary>
    /// BGM管理器 - 管理背景音乐
    /// 使用 Unity 原生 AudioSource
    /// </summary>
    public class BGMAudioManager : MonoBehaviour
    {
        [System.Serializable]
        public class TransitionConfig
        {
            public float Duration = 1f;
            public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }

        private const string BGM_SOURCE_A_KEY = "__BGM_SOURCE_A__";
        private const string BGM_SOURCE_B_KEY = "__BGM_SOURCE_B__";

        public TransitionConfig Transition = new TransitionConfig();

        private AudioManager _manager;
        private PooledAudioSource _sourceA;
        private PooledAudioSource _sourceB;
        private PooledAudioSource _currentSource;
        private string _currentName;
        private Coroutine _transitionCoroutine;
        private bool _isPaused;

        public void Init(AudioManager manager)
        {
            _manager = manager;
        }

        public void Play(string bgmName, float volume = 1f, float speed = 1f, bool useTransition = true)
        {
            if (string.IsNullOrEmpty(bgmName)) return;
            if (_currentName == bgmName && _currentSource != null && _currentSource.IsPlaying) return;

            if (useTransition && _currentSource != null && _currentSource.IsPlaying)
            {
                StartTransition(bgmName, volume, speed);
            }
            else
            {
                PlayImmediate(bgmName, volume, speed);
            }
        }

        public void Stop(bool useTransition = true)
        {
            if (_currentSource == null) return;

            if (useTransition)
            {
                StartCoroutine(FadeOutAndStop());
            }
            else
            {
                _currentSource.Stop();
                _currentSource.gameObject.SetActive(false);
                _currentSource = null;
                _currentName = null;
            }
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
            ApplyVolumeInternal(_manager.Settings.BGMVolume.Value);
        }

        public void ApplyVolumeInternal(float volume)
        {
            if (_currentSource == null) return;

            float finalVolume = _manager.GetFinalVolume(volume);
            _currentSource.SetVolume(finalVolume * _currentSource.BaseVolume);
        }

        public void SetVolume(float volume, bool fade = true, float fadeTime = 0.5f)
        {
            if (fade)
            {
                StartCoroutine(FadeVolume(volume, fadeTime));
            }
            else
            {
                _manager.Settings.BGMVolume.Value = volume;
            }
        }

        private void StartTransition(string newBgmName, float targetVolume, float speed)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            if (this != null && gameObject.activeInHierarchy)
            {
                _transitionCoroutine = StartCoroutine(TransitionCoroutine(newBgmName, targetVolume, speed));
            }
        }

        private IEnumerator TransitionCoroutine(string newBgmName, float targetVolume, float speed)
        {
            var oldSource = _currentSource;
            float oldVolume = oldSource?.Volume ?? 0;

            var newSource = GetNextSource(oldSource);
            _currentSource = newSource;
            _currentName = newBgmName;

            var clip = _manager.Pool.GetClip(newBgmName);
            if (clip == null)
            {
                yield return _manager.Pool.LoadClipAsync(newBgmName).ToCoroutine(c => clip = c);
            }

            if (clip != null)
            {
                newSource.SetClip(clip);
                newSource.SetLoop(true);
                newSource.SetPitch(speed);
                newSource.BaseVolume = targetVolume;
                newSource.SetVolume(0);
                newSource.Play();
            }

            float elapsed = 0;
            while (elapsed < Transition.Duration)
            {
                elapsed += Time.deltaTime;
                float t = Transition.Curve.Evaluate(elapsed / Transition.Duration);

                if (oldSource != null)
                    oldSource.SetVolume(Mathf.Lerp(oldVolume, 0, t));

                if (newSource != null)
                {
                    float finalVolume = _manager.GetFinalVolume(Mathf.Lerp(0, targetVolume, t));
                    newSource.SetVolume(finalVolume);
                }

                yield return null;
            }

            if (oldSource != null)
            {
                oldSource.Stop();
                oldSource.gameObject.SetActive(false);
            }

            ApplyVolume();
            _transitionCoroutine = null;
        }

        private IEnumerator FadeOutAndStop()
        {
            if (_currentSource == null) yield break;

            float startVolume = _currentSource.Volume;
            float elapsed = 0;

            while (elapsed < Transition.Duration)
            {
                elapsed += Time.deltaTime;
                float t = Transition.Curve.Evaluate(elapsed / Transition.Duration);
                _currentSource.SetVolume(Mathf.Lerp(startVolume, 0, t));
                yield return null;
            }

            _currentSource.Stop();
            _currentSource.gameObject.SetActive(false);
            _currentSource = null;
            _currentName = null;
        }

        private IEnumerator FadeVolume(float targetVolume, float fadeTime)
        {
            if (_currentSource == null) yield break;

            float startVolume = _currentSource.Volume;
            float elapsed = 0;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;
                float volume = Mathf.Lerp(startVolume, targetVolume, t);
                _currentSource.SetVolume(_manager.GetFinalVolume(volume));
                yield return null;
            }

            _manager.Settings.BGMVolume.Value = targetVolume;
        }

        private void PlayImmediate(string bgmName, float volume, float speed)
        {
            _currentSource ??= EnsureSourceA();
            _currentSource.Stop();
            _currentSource.gameObject.SetActive(false);

            _currentName = bgmName;
            _isPaused = false;

            if (this != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(LoadAndPlay(bgmName, volume, speed));
            }
        }

        private IEnumerator LoadAndPlay(string bgmName, float volume, float speed)
        {
            if (this == null || !gameObject.activeInHierarchy) yield break;

            var clip = _manager.Pool.GetClip(bgmName);
            if (clip == null)
            {
                yield return _manager.Pool.LoadClipAsync(bgmName).ToCoroutine(c => clip = c);
            }

            if (clip != null && _currentSource != null)
            {
                _currentSource.SetClip(clip);
                _currentSource.SetLoop(true);
                _currentSource.SetPitch(speed);
                _currentSource.BaseVolume = volume;
                _currentSource.SetVolume(_manager.GetFinalVolume(volume));
                _currentSource.Play();
            }
        }

        private PooledAudioSource EnsureSourceA()
        {
            _sourceA ??= _manager.Pool.Get(BGM_SOURCE_A_KEY, PooledAudioSource.AudioCategory.BGM);
            return _sourceA;
        }

        private PooledAudioSource EnsureSourceB()
        {
            _sourceB ??= _manager.Pool.Get(BGM_SOURCE_B_KEY, PooledAudioSource.AudioCategory.BGM);
            return _sourceB;
        }

        private PooledAudioSource GetNextSource(PooledAudioSource current)
        {
            var a = EnsureSourceA();
            var b = EnsureSourceB();
            return current == a ? b : a;
        }

        public void OnUpdate(float deltaTime) { }

        public void OnDestroy()
        {
            StopAllCoroutines();
            Stop(false);
        }
    }
}
