using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// 音频混合器 - 管理音频快照和动态混合
    /// </summary>
    public class AudioMixer
    {
        public enum SnapshotType
        {
            Normal,
            Battle,
            Dialogue,
            Pause,
            Cutscene
        }
        
        [Serializable]
        public class Snapshot
        {
            public float BGMVolume = 1f;
            public float SFXVolume = 1f;
            public float VoiceVolume = 1f;
            public float AmbientVolume = 1f;
        }
        
        private AudioManager _manager;
        private Dictionary<SnapshotType, Snapshot> _snapshots;
        private SnapshotType _currentSnapshot;
        private Coroutine _transitionCoroutine;
        
        public float TransitionTime { get; set; } = 1f;
        public SnapshotType CurrentSnapshot => _currentSnapshot;
        
        public AudioMixer(AudioManager manager)
        {
            _manager = manager;
            InitializeSnapshots();
        }
        
        private void InitializeSnapshots()
        {
            _snapshots = new Dictionary<SnapshotType, Snapshot>
            {
                [SnapshotType.Normal] = new Snapshot 
                { 
                    BGMVolume = 1f, 
                    SFXVolume = 1f, 
                    VoiceVolume = 1f, 
                    AmbientVolume = 1f 
                },
                [SnapshotType.Battle] = new Snapshot 
                { 
                    BGMVolume = 0.6f, 
                    SFXVolume = 1f, 
                    VoiceVolume = 0.8f, 
                    AmbientVolume = 0.5f 
                },
                [SnapshotType.Dialogue] = new Snapshot 
                { 
                    BGMVolume = 0.4f, 
                    SFXVolume = 0f, 
                    VoiceVolume = 1f, 
                    AmbientVolume = 0.3f 
                },
                [SnapshotType.Pause] = new Snapshot 
                { 
                    BGMVolume = 0f, 
                    SFXVolume = 0f, 
                    VoiceVolume = 0f, 
                    AmbientVolume = 0f 
                },
                [SnapshotType.Cutscene] = new Snapshot 
                { 
                    BGMVolume = 0.5f, 
                    SFXVolume = 0.3f, 
                    VoiceVolume = 1f, 
                    AmbientVolume = 0.4f 
                }
            };
        }
        
        public void TransitionTo(SnapshotType snapshotType, float? customTransitionTime = null)
        {
            if (_currentSnapshot == snapshotType) return;
            
            _currentSnapshot = snapshotType;
            var targetSnapshot = _snapshots[snapshotType];
            
            ApplySnapshot(targetSnapshot, customTransitionTime ?? TransitionTime);
            
            LogTool.Log($"[AudioMixer] 切换到快照: {snapshotType}");
        }
        
        public void SetSnapshotImmediate(SnapshotType snapshotType)
        {
            _currentSnapshot = snapshotType;
            var snapshot = _snapshots[snapshotType];
            
            _manager.Settings.BGMVolume.Value = snapshot.BGMVolume;
            _manager.Settings.SFXVolume.Value = snapshot.SFXVolume;
            _manager.Settings.VoiceVolume.Value = snapshot.VoiceVolume;
            _manager.Settings.AmbientVolume.Value = snapshot.AmbientVolume;
        }
        
        private void ApplySnapshot(Snapshot targetSnapshot, float transitionTime)
        {
            if (_transitionCoroutine != null)
            {
                _manager.StopCoroutine(_transitionCoroutine);
            }
            
            _transitionCoroutine = _manager.StartCoroutine(TransitionCoroutine(targetSnapshot, transitionTime));
        }
        
        private IEnumerator TransitionCoroutine(Snapshot targetSnapshot, float transitionTime)
        {
            float startBGM = _manager.Settings.BGMVolume.Value;
            float startSFX = _manager.Settings.SFXVolume.Value;
            float startVoice = _manager.Settings.VoiceVolume.Value;
            float startAmbient = _manager.Settings.AmbientVolume.Value;
            
            float elapsed = 0;
            while (elapsed < transitionTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionTime;
                
                // 使用临时变量计算并直接应用，而不修改 BindableProperty 触发事件循环
                float currentBGM = Mathf.Lerp(startBGM, targetSnapshot.BGMVolume, t);
                float currentSFX = Mathf.Lerp(startSFX, targetSnapshot.SFXVolume, t);
                float currentVoice = Mathf.Lerp(startVoice, targetSnapshot.VoiceVolume, t);
                float currentAmbient = Mathf.Lerp(startAmbient, targetSnapshot.AmbientVolume, t);

                // 这里需要一种方式直接更新音量而不触发事件，或者在更新后统一设置结果
                // 考虑到目前的架构，我们可以在过渡结束后设置最终值，过渡过程中手动调用 ApplyVolume
                ApplyTemporaryVolumes(currentBGM, currentSFX, currentVoice, currentAmbient);
                
                yield return null;
            }
            
            // 过渡结束后设置最终值，这会触发一次事件，是可接受的
            _manager.Settings.BGMVolume.Value = targetSnapshot.BGMVolume;
            _manager.Settings.SFXVolume.Value = targetSnapshot.SFXVolume;
            _manager.Settings.VoiceVolume.Value = targetSnapshot.VoiceVolume;
            _manager.Settings.AmbientVolume.Value = targetSnapshot.AmbientVolume;
            
            _transitionCoroutine = null;
        }

        private void ApplyTemporaryVolumes(float bgm, float sfx, float voice, float ambient)
        {
            _manager.BGM?.ApplyVolumeInternal(bgm);
            _manager.SFX?.ApplyVolumeInternal(sfx);
            _manager.Voice?.ApplyVolumeInternal(voice);
            _manager.Ambient?.ApplyVolumeInternal(ambient);
        }
    }
}
