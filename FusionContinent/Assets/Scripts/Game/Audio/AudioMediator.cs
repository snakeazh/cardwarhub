namespace Game.Audio
{
    /// <summary>
    /// Audio 中介者：对外统一音频控制入口
    /// 外部模块应优先通过该中介者调用音频能力，避免直接依赖 AudioManager 细节。
    /// </summary>
    public sealed class AudioMediator
    {
        private static readonly AudioMediator sInstance = new AudioMediator();
        public static AudioMediator Instance => sInstance;

        private AudioMediator() { }

        private AudioManager Manager => AudioManager.Instance;

        #region 播放控制

        public void PlayBGM(string bgmName, float volume = 1f, float speed = 1f, bool useTransition = true)
        {
            Manager.BGM?.Play(bgmName, volume, speed, useTransition);
        }

        public void StopBGM(bool useTransition = true)
        {
            Manager.BGM?.Stop(useTransition);
        }

        public void PlaySFX(string sfxName, float volume = 1f, float pitch = 1f)
        {
            Manager.SFX?.Play(sfxName, volume, pitch);
        }

        public void PlayVoice(string voiceName, float volume = 1f, float speed = 1f, bool canInterrupt = true)
        {
            Manager.Voice?.Play(voiceName, volume, speed, canInterrupt);
        }

        public void PlayAmbient(string ambientName, float volume = 1f, bool loop = true)
        {
            Manager.Ambient?.AddLayer(ambientName, volume, loop);
        }

        public void RemoveAmbient(string ambientName)
        {
            Manager.Ambient?.RemoveLayer(ambientName);
        }

        public void StopAllAmbient()
        {
            Manager.Ambient?.StopAll();
        }

        #endregion

        #region 音量与静音

        public void SetMasterVolume(float volume)
        {
            Manager.Settings.MasterVolume.Value = volume;
        }

        public void SetBGMVolume(float volume)
        {
            Manager.Settings.BGMVolume.Value = volume;
        }

        public void SetSFXVolume(float volume)
        {
            Manager.Settings.SFXVolume.Value = volume;
        }

        public void SetVoiceVolume(float volume)
        {
            Manager.Settings.VoiceVolume.Value = volume;
        }

        public void SetAmbientVolume(float volume)
        {
            Manager.Settings.AmbientVolume.Value = volume;
        }

        public void SetMute(bool mute)
        {
            Manager.Settings.MasterMute.Value = mute;
        }

        public void SaveSettings()
        {
            Manager.SaveSettings();
        }

        #endregion

        #region 快照与全局控制

        public void TransitionToSnapshot(AudioMixer.SnapshotType snapshotType, float? transitionTime = null)
        {
            Manager.Mixer?.TransitionTo(snapshotType, transitionTime);
        }

        public void SetSnapshotImmediate(AudioMixer.SnapshotType snapshotType)
        {
            Manager.Mixer?.SetSnapshotImmediate(snapshotType);
        }

        public void PauseAll()
        {
            Manager.PauseAll();
        }

        public void ResumeAll()
        {
            Manager.ResumeAll();
        }

        public void StopAll()
        {
            Manager.StopAll();
        }

        #endregion
    }
}
