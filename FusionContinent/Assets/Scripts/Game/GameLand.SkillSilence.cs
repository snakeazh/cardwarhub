using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent.Battle;
using UnityEngine;

namespace FusionContinent
{
    public partial class GameLand
    {
        private readonly Dictionary<int, HashSet<int>> _skillSilenceTokensBySkill =
            new Dictionary<int, HashSet<int>>();
        private readonly List<Coroutine> _skillSilenceCoroutines = new List<Coroutine>();
        private int _nextSkillSilenceToken;

        public int SilenceRandomSkillCards(int count, float duration)
        {
            if (count <= 0 || duration <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(count), "Skill silence requires Count>0 and Duration>0.");
            }

            var candidates = new List<int>();
            for (int i = 0; i < _skillCards.Count; i++)
            {
                var card = _skillCards[i];
                if (card == null || card.CardId <= 0 || !card.IsUnlock.Value
                    || IsSkillCardSilenced(card.CardId)
                    || !IsSkillHeroAvailable(card.CardId))
                {
                    continue;
                }

                var specialConfig = _specialEffectsConfig.GetOrDefault(card.CardId);
                if (specialConfig == null)
                {
                    throw new System.InvalidOperationException(
                        "Missing skill config while applying silence. SkillId=" + card.CardId);
                }

                if (specialConfig.PassiveSkill == 0)
                {
                    candidates.Add(card.CardId);
                }
            }

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                int value = candidates[i];
                candidates[i] = candidates[swapIndex];
                candidates[swapIndex] = value;
            }

            int silenceCount = Mathf.Min(count, candidates.Count);
            if (silenceCount == 0)
            {
                LogTool.Log(
                    "[MonsterMechanic] SilenceRandomSkillCards 无可沉默技能"
                    + " RequestCount=" + count
                    + " CandidateCount=" + candidates.Count
                    + " Duration=" + duration);
                return 0;
            }

            int token = ++_nextSkillSilenceToken;
            var silencedSkillIds = candidates.GetRange(0, silenceCount);
            LogTool.Log(
                "[MonsterMechanic] SilenceRandomSkillCards"
                + " Token=" + token
                + " RequestCount=" + count
                + " SilencedCount=" + silenceCount
                + " Duration=" + duration
                + " SkillIds=" + string.Join(",", silencedSkillIds));
            for (int i = 0; i < silencedSkillIds.Count; i++)
            {
                int skillId = silencedSkillIds[i];
                if (!_skillSilenceTokensBySkill.TryGetValue(skillId, out var tokens))
                {
                    tokens = new HashSet<int>();
                    _skillSilenceTokensBySkill.Add(skillId, tokens);
                }

                tokens.Add(token);
                RefreshSkillCardSilenceState(skillId);
            }

            _skillSilenceCoroutines.Add(StartCoroutine(
                RemoveSkillSilenceAfter(token, silencedSkillIds, duration)));
            return silenceCount;
        }

        public bool IsSkillCardSilenced(int skillId)
        {
            return _skillSilenceTokensBySkill.TryGetValue(skillId, out var tokens)
                   && tokens.Count > 0;
        }

        private IEnumerator RemoveSkillSilenceAfter(int token, List<int> skillIds, float duration)
        {
            yield return new WaitForSeconds(duration);
            LogTool.Log(
                "[MonsterMechanic] Silence 到期恢复"
                + " Token=" + token
                + " SkillIds=" + string.Join(",", skillIds));
            for (int i = 0; i < skillIds.Count; i++)
            {
                int skillId = skillIds[i];
                if (_skillSilenceTokensBySkill.TryGetValue(skillId, out var tokens))
                {
                    tokens.Remove(token);
                    if (tokens.Count == 0)
                    {
                        _skillSilenceTokensBySkill.Remove(skillId);
                    }
                }

                RefreshSkillCardSilenceState(skillId);
            }
        }

        private void RefreshSkillCardSilenceState(int skillId)
        {
            bool silenced = IsSkillCardSilenced(skillId);
            if (_skillCardHandlers.TryGetValue(skillId, out var handler))
            {
                if (silenced)
                {
                    handler.SetDragState(DragState.DragFalse);
                }
                else
                {
                    RestoreSkillCardDragState(handler);
                }
            }

            if (_skillCardObjs.TryGetValue(skillId, out var cardObject) && cardObject != null)
            {
                var canvasGroup = cardObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = cardObject.AddComponent<CanvasGroup>();
                }

                canvasGroup.alpha = silenced ? 0.55f : 1f;
            }

            if (GameLandUI.Default != null)
            {
                GameLandUI.Default.RefreshSkillSilenceState(skillId, silenced);
            }
        }

        private void RestoreSkillCardDragState(SkillCardInfoHandler handler)
        {
            var card = handler.Card;
            if (card == null || !card.IsUnlock.Value || Global.AutoReleaseSkillState.Value)
            {
                handler.SetDragState(DragState.DragFalse);
                return;
            }

            var specialConfig = _specialEffectsConfig.GetOrDefault(card.CardId);
            if (specialConfig == null)
            {
                throw new System.InvalidOperationException(
                    "Missing skill config while restoring silence state. SkillId=" + card.CardId);
            }

            bool ready = skill.TryGetValue(card.CardId, out var coolSkill)
                         && (coolSkill.ChargingNum > 0 || !coolSkill.Cool);
            handler.SetDragState(
                specialConfig.PassiveSkill == 0 && ready
                    ? DragState.GamePlay
                    : DragState.DragFalse);
        }

        private void CleanupSkillSilence()
        {
            for (int i = 0; i < _skillSilenceCoroutines.Count; i++)
            {
                StopCoroutine(_skillSilenceCoroutines[i]);
            }

            _skillSilenceCoroutines.Clear();
            _skillSilenceTokensBySkill.Clear();
        }
    }
}
