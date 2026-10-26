using FusionContinent.Skill.Config;
using Manager;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 音效节点处理器
    /// </summary>
    public class SoundHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "Sound";

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var soundId = node.GetParamInt("sound", 0);
            if (soundId <= 0) return;

            LogTool.Log($"[SoundHandler] 播放音效: {soundId}");

            SoundManager.Instance.PlayVoiceInGame(soundId);
            // 实际项目中播放音效
            // AudioManager.Play(sound);
        }
    }
}
