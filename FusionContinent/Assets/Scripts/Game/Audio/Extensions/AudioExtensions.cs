namespace Game.Audio
{
    /// <summary>
    /// 音频扩展方法（仅基于字符串资源名）
    /// </summary>
    public static class AudioExtensions
    {
        public static void PlayBGM(this string bgmName, float volume = 1f, float speed = 1f, bool useTransition = true)
        {
            AudioManager.Instance?.BGM.Play(bgmName, volume, speed, useTransition);
        }

        public static void PlaySFX(this string sfxName, float volume = 1f, float pitch = 1f)
        {
            AudioManager.Instance?.SFX.Play(sfxName, volume, pitch);
        }

        public static void PlayVoice(this string voiceName, float volume = 1f, float speed = 1f, bool canInterrupt = true)
        {
            AudioManager.Instance?.Voice.Play(voiceName, volume, speed, canInterrupt);
        }
    }
}
