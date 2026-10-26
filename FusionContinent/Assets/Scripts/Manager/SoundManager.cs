using System;
using System.Collections;
using FusionContinent;
using FusionContinent.Sounds;
using Game.Audio;
using QFramework;
using UnityEngine;

namespace Manager
{
    [MonoSingletonPath("[Audio]/SoundManager")]
    public class SoundManager : MonoBehaviour, ISingleton
    {
        public static SoundManager Instance => MonoSingletonProperty<SoundManager>.Instance;

        public void OnSingletonInit()
        {
        }

        public void PlayBgm(SoundType soundType, float speed = 1.0f)
        {
            PlayBgm((int)soundType, speed);
        }

        public virtual void PlayBgm(int soundType, float speed = 1.0f)
        {
            if (!DataConfigManager.Tables.TbGameSoundConfig.DataMap.ContainsKey(soundType))
            {
                return;
            }

            var soundInfo = DataConfigManager.Tables.TbGameSoundConfig[soundType];
            if (soundInfo != null)
            {
                PlayBgm(soundInfo.SoundEffects, soundInfo.SoundVolume, speed);
            }
        }

        public virtual void PlayVoice(SoundType soundType, float speed = 1.0f)
        {
            PlayVoice((int)soundType, speed);
        }

        public virtual void PlayVoiceInGame(int soundType, float speed = 1.0f)
        {
            if (!DataConfigManager.Tables.TbGameSoundConfig.DataMap.ContainsKey(soundType))
            {
                return;
            }

            var soundInfo = DataConfigManager.Tables.TbGameSoundConfig[soundType];
            if (soundInfo == null)
            {
                return;
            }

            speed = Global.SpeedMultiplier.Value;

            if (soundInfo.SoundDelayTime > 0.001f)
            {
                StartCoroutine(WaitRunAction(soundInfo.SoundDelayTime, () =>
                {
                    PlayVoice(soundInfo.SoundEffects, soundInfo.SoundVolume, speed);
                }));
            }
            else
            {
                PlayVoice(soundInfo.SoundEffects, soundInfo.SoundVolume, speed);
            }
        }

        private IEnumerator WaitRunAction(float time, Action action)
        {
            yield return new WaitForSeconds(time);
            action?.Invoke();
        }

        public virtual void PlayVoice(int soundType, float speed = 1.0f)
        {
            if (!DataConfigManager.Tables.TbGameSoundConfig.DataMap.ContainsKey(soundType))
            {
                return;
            }

            var soundInfo = DataConfigManager.Tables.TbGameSoundConfig[soundType];
            if (soundInfo != null)
            {
                PlayVoice(soundInfo.SoundEffects, soundInfo.SoundVolume, speed);
            }
        }

        public virtual void PlayVoice(string voiceName, float volumeScale = 1.0f, float speed = 1.0f)
        {
            AudioMediator.Instance.PlayVoice(voiceName, volumeScale, speed);
        }

        public virtual void PlayBgm(string bgmName, float volumeScale = 1.0f, float speed = 1.0f)
        {
            AudioMediator.Instance.PlayBGM(bgmName, volumeScale, speed);
        }
    }
}
