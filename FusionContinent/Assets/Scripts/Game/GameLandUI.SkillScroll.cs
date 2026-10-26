using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using FusionContinent.UI.Components;
using Game.Soldier;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    public partial class GameLandUI
    {
        private BattleSkillCardScrollView mSkillCardScrollView;
        private RectTransform mSkillCardTemplate;
        private readonly List<BattleSkillCardScrollItem> mSkillScrollItems = new List<BattleSkillCardScrollItem>();
        private bool mLastSkillScrollManualOperate;
        private readonly List<int> mLastSkillScrollSkillIds = new List<int>();
        private System.Action<bool> mSkillScrollAutoReleaseHandler;
        private System.Action<object> mSkillCoolingStartedHandler;
        private System.Action<object> mSkillCoolingDoneHandler;
        private System.Action<object> mSkillCoolingInitializedHandler;

        private void InitSkillCardScroll()
        {
            if (SkillRect == null)
            {
                LogTool.LogError("[GameLandUI.SkillScroll] SkillRect 未绑定");
                return;
            }

            var scrollRoot = SkillRect.Find("cardScroll/Scroll View");
            if (scrollRoot == null)
            {
                LogTool.LogError("[GameLandUI.SkillScroll] 未找到 SkillRect/cardScroll/Scroll View");
                return;
            }

            mSkillCardScrollView = scrollRoot.GetComponent<BattleSkillCardScrollView>();
            if (mSkillCardScrollView == null)
            {
                LogTool.LogError("[GameLandUI.SkillScroll] Scroll View 请挂载 BattleSkillCardScrollView");
                return;
            }

            mSkillCardTemplate = mSkillCardScrollView.content.Find("CardInfo") as RectTransform;
            if (mSkillCardTemplate == null)
            {
                LogTool.LogError("[GameLandUI.SkillScroll] Content 下未找到 CardInfo 模板");
                return;
            }

            mSkillCardTemplate.gameObject.SetActive(false);

            if (mSkillScrollAutoReleaseHandler == null)
            {
                mSkillScrollAutoReleaseHandler = OnSkillScrollAutoReleaseChanged;
                Global.AutoReleaseSkillState.Register(mSkillScrollAutoReleaseHandler);
            }

            if (mSkillCoolingStartedHandler == null)
            {
                mSkillCoolingStartedHandler = OnSkillScrollCoolingStarted;
                mSkillCoolingDoneHandler = OnSkillScrollCoolingDone;
                mSkillCoolingInitializedHandler = OnSkillScrollCoolingInitialized;
                Global.Publisher.Subscribe(MessagePublisher.SKILL_COOLING_STARTED, mSkillCoolingStartedHandler);
                Global.Publisher.Subscribe(MessagePublisher.CHANGE_GAME_CARD_COOLING, mSkillCoolingDoneHandler);
                Global.Publisher.Subscribe(MessagePublisher.SKILL_COOLING_INITIALIZED, mSkillCoolingInitializedHandler);
            }

            RefreshSkillCardScroll();
        }

        private void RetryInitSkillCardScroll()
        {
            if (mSkillCardScrollView == null || mSkillCardTemplate == null)
            {
                InitSkillCardScroll();
                return;
            }

            RefreshSkillCardScroll();
        }

        /// <summary>
        /// 英灵上场后刷新 cardScroll 技能列表。
        /// </summary>
        public void RefreshSkillCardScrollAfterSoldierEnter()
        {
            RetryInitSkillCardScroll();
        }

        private void DestroySkillCardScroll()
        {
            if (mSkillScrollAutoReleaseHandler != null)
            {
                Global.AutoReleaseSkillState.UnRegister(mSkillScrollAutoReleaseHandler);
                mSkillScrollAutoReleaseHandler = null;
            }

            if (mSkillCoolingStartedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.SKILL_COOLING_STARTED, mSkillCoolingStartedHandler);
                mSkillCoolingStartedHandler = null;
            }

            if (mSkillCoolingDoneHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GAME_CARD_COOLING, mSkillCoolingDoneHandler);
                mSkillCoolingDoneHandler = null;
            }

            if (mSkillCoolingInitializedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.SKILL_COOLING_INITIALIZED, mSkillCoolingInitializedHandler);
                mSkillCoolingInitializedHandler = null;
            }
        }

        private void OnSkillScrollCoolingStarted(object data)
        {
            if (data is not SkillCoolingData coolingData)
            {
                return;
            }

            SyncSkillScrollItemCooldown(coolingData.SkillId);
        }

        public void PlaySkillScrollAutoReleaseAnimation(int skillId)
        {
            for (int i = 0; i < mSkillScrollItems.Count; i++)
            {
                var item = mSkillScrollItems[i];
                if (!item.gameObject.activeSelf || item.SkillId != skillId)
                {
                    continue;
                }

                item.PlayAutoReleaseScaleTween();
                break;
            }
        }

        private void OnSkillScrollCoolingDone(object data)
        {
            if (data is not int skillId)
            {
                return;
            }

            SyncSkillScrollItemCooldown(skillId);
        }

        private void OnSkillScrollCoolingInitialized(object data)
        {
            for (int i = 0; i < mSkillScrollItems.Count; i++)
            {
                var item = mSkillScrollItems[i];
                if (!item.gameObject.activeSelf)
                {
                    continue;
                }

                UpdateSkillScrollItemCooldown(item, item.SkillId);
                Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_CARD_COOLING, item.SkillId);
            }
        }

        private void OnSkillScrollItemPlaced(int skillId)
        {
            SyncSkillScrollItemCooldown(skillId);
            TryRegisterSkillScrollItemForGuide(skillId);
        }

        private void TryRegisterSkillScrollItemForGuide(int skillId)
        {
            if (Global.StageId.Value != 3001001 && Global.StageId.Value != 10001)
            {
                return;
            }

            if (mSkillCardScrollView == null || mSkillCardScrollView.content == null)
            {
                return;
            }

            var stay = mSkillCardScrollView.content.GetComponent<GameCardStay>();
            if (stay == null)
            {
                return;
            }

            for (int i = 0; i < mSkillScrollItems.Count; i++)
            {
                var item = mSkillScrollItems[i];
                if (!item.gameObject.activeSelf || item.SkillId != skillId)
                {
                    continue;
                }

                stay.AddSkillById(skillId, item.GetComponent<RectTransform>());
                break;
            }
        }

        private void SyncSkillScrollItemCooldown(int skillId)
        {
            for (int i = 0; i < mSkillScrollItems.Count; i++)
            {
                var item = mSkillScrollItems[i];
                if (!item.gameObject.activeSelf || item.SkillId != skillId)
                {
                    continue;
                }

                UpdateSkillScrollItemCooldown(item, skillId);
                break;
            }
        }

        private void OnSkillScrollAutoReleaseChanged(bool isOn)
        {
            // 自动模式下仍允许点击未 CD 技能以取消自动战斗
            ApplySkillScrollManualOperate(true);

            for (int i = 0; i < mSkillScrollItems.Count; i++)
            {
                var item = mSkillScrollItems[i];
                if (!item.gameObject.activeSelf)
                {
                    continue;
                }

                item.ClearAutoReleasePresentation();
            }

            if (mSkillCardScrollView != null)
            {
                mSkillCardScrollView.RefreshDisplayStates();
            }
        }

        private void ApplySkillScrollManualOperate(bool canManualOperate)
        {
            for (int i = 0; i < mSkillScrollItems.Count; i++)
            {
                var item = mSkillScrollItems[i];
                if (!item.gameObject.activeSelf)
                {
                    continue;
                }

                item.SetManualOperate(canManualOperate);
            }

            mLastSkillScrollManualOperate = canManualOperate;
        }

        private void RefreshSkillCardScroll()
        {
            if (mSkillCardScrollView == null || mSkillCardTemplate == null)
            {
                return;
            }

            if (mSkillCardScrollView.content == null || mSkillCardScrollView.viewport == null)
            {
                LogTool.LogError("[GameLandUI.SkillScroll] BattleSkillCardScrollView 的 Content/Viewport 未绑定");
                return;
            }

            var savedNormalizedPosition = mSkillCardScrollView.horizontalNormalizedPosition;
            var displaySkills = CollectSkillScrollEntries();
            // 自动模式下仍允许点击未 CD 技能以取消自动战斗
            var canManualOperate = true;
            var previousIds = new HashSet<int>(mLastSkillScrollSkillIds);
            EnsureSkillScrollItemCount(displaySkills.Count);

            for (int i = 0; i < mSkillScrollItems.Count; i++)
            {
                var item = mSkillScrollItems[i];
                if (i >= displaySkills.Count)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }

                var battleSkill = displaySkills[i];
                var skillId = battleSkill.CardId;
                item.Bind(battleSkill, canManualOperate);
                UpdateSkillScrollItemCooldown(item, skillId);

                if (!previousIds.Contains(skillId))
                {
                    OnSkillScrollItemPlaced(skillId);
                }
            }

            mSkillCardScrollView.SetScrollEnabled(displaySkills.Count > 5);
            mSkillCardScrollView.RefreshCardItems(savedNormalizedPosition);

            mLastSkillScrollManualOperate = canManualOperate;
            UpdateLastSkillScrollSkillIds(displaySkills);
        }

        private void UpdateLastSkillScrollSkillIds(List<BattleCardGroup> displaySkills)
        {
            mLastSkillScrollSkillIds.Clear();
            foreach (var skill in displaySkills)
            {
                mLastSkillScrollSkillIds.Add(skill.CardId);
            }
        }

        private bool HasSkillScrollDisplayChanged(List<BattleCardGroup> displaySkills)
        {
            if (displaySkills.Count != mLastSkillScrollSkillIds.Count)
            {
                return true;
            }

            for (int i = 0; i < displaySkills.Count; i++)
            {
                if (displaySkills[i].CardId != mLastSkillScrollSkillIds[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void SyncSkillCardScrollCooldown()
        {
            if (mSkillCardScrollView == null)
            {
                return;
            }

            if (mSkillCardTemplate == null)
            {
                InitSkillCardScroll();
                return;
            }

            var displaySkills = CollectSkillScrollEntries();
            if (HasSkillScrollDisplayChanged(displaySkills))
            {
                RefreshSkillCardScroll();
                return;
            }

            if (mSkillScrollItems.Count == 0)
            {
                return;
            }

            // 自动模式下仍允许点击未 CD 技能以取消自动战斗
            var canManualOperate = true;
            if (canManualOperate != mLastSkillScrollManualOperate)
            {
                ApplySkillScrollManualOperate(canManualOperate);
                return;
            }

            for (int i = 0; i < mSkillScrollItems.Count; i++)
            {
                var item = mSkillScrollItems[i];
                if (!item.gameObject.activeSelf)
                {
                    continue;
                }

                UpdateSkillScrollItemCooldown(item, item.SkillId);
            }
        }

        private void EnsureSkillScrollItemCount(int count)
        {
            while (mSkillScrollItems.Count < count)
            {
                var cardObj = Instantiate(mSkillCardTemplate.gameObject, mSkillCardScrollView.content);
                cardObj.name = $"CardInfo_{mSkillScrollItems.Count}";
                cardObj.SetActive(true);

                var item = cardObj.GetComponent<BattleSkillCardScrollItem>();
                if (item == null)
                {
                    item = cardObj.AddComponent<BattleSkillCardScrollItem>();
                }

                mSkillScrollItems.Add(item);
            }
        }

        /// <summary>
        /// 仅收集已上场英雄对应的主动技能，按编队槽位顺序排列。
        /// </summary>
        private List<BattleCardGroup> CollectSkillScrollEntries()
        {
            var result = new List<BattleCardGroup>();
            var addedSkillIds = new HashSet<int>();

            foreach (var teamSoldier in Global.SoldierTeams
                         .Where(card => card != null &&
                                        card.CardType == CardType.Soldier &&
                                        card.CardId > 0 &&
                                        card.IsUnlock.Value)
                         .OrderBy(card => card.CardSlotIndex))
            {
                if (!IsSoldierOnBattlefield(teamSoldier.CardId))
                {
                    continue;
                }

                foreach (var skillCard in Global.SoldierSkillTeams)
                {
                    if (!TryGetActiveSkillForSoldier(skillCard, teamSoldier.CardId, out var battleSkill))
                    {
                        continue;
                    }

                    if (addedSkillIds.Add(battleSkill.CardId))
                    {
                        result.Add(battleSkill);
                    }
                }
            }

            foreach (var skillCard in Global.SoldierSkillTeams)
            {
                if (!TryGetGlobalActiveSkill(skillCard, out var battleSkill))
                {
                    continue;
                }

                if (addedSkillIds.Add(battleSkill.CardId))
                {
                    result.Add(battleSkill);
                }
            }

            return result;
        }

        private bool IsSoldierOnBattlefield(int soldierId)
        {
            if (GamePlayManager.Default == null)
            {
                return false;
            }

            return GamePlayManager.Default.villageGameCards.Any(card =>
                card != null &&
                card.RoleType == SoldierRoleType.Self &&
                !card.isDie &&
                card.cardId == soldierId);
        }

        private bool TryGetActiveSkillForSoldier(BattleCardGroup skillCard, int soldierId, out BattleCardGroup battleSkill)
        {
            battleSkill = null;
            if (skillCard == null || skillCard.CardId == 0 || skillCard.CardType != CardType.Skill)
            {
                return false;
            }

            if (!skillCard.IsUnlock.Value)
            {
                return false;
            }

            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillCard.CardId);
            if (specialCfg == null || specialCfg.PassiveSkill == 1 || specialCfg.ActsOnCard != soldierId)
            {
                return false;
            }

            battleSkill = skillCard;
            return true;
        }

        private bool TryGetGlobalActiveSkill(BattleCardGroup skillCard, out BattleCardGroup battleSkill)
        {
            battleSkill = null;
            if (skillCard == null || skillCard.CardId == 0 || skillCard.CardType != CardType.Skill)
            {
                return false;
            }

            if (!skillCard.IsUnlock.Value)
            {
                return false;
            }

            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillCard.CardId);
            if (specialCfg == null || specialCfg.PassiveSkill == 1 || specialCfg.ActsOnCard != 0)
            {
                return false;
            }

            battleSkill = skillCard;
            return true;
        }

        private void UpdateSkillScrollItemCooldown(BattleSkillCardScrollItem item, int skillId)
        {
            if (item == null)
            {
                return;
            }

            bool silenced = GameLand.Default != null && GameLand.Default.IsSkillCardSilenced(skillId);
            item.SetSilenced(silenced);

            if (GameLand.Default != null &&
                GameLand.Default.TryGetSkillCooldownProgress(skillId, out var remainTime, out var totalTime))
            {
                item.UpdateCooldownUI(remainTime, totalTime);
                return;
            }

            item.SetCooldownReady();
        }

        public void RefreshSkillSilenceState(int skillId, bool silenced)
        {
            for (int i = 0; i < mSkillScrollItems.Count; i++)
            {
                var item = mSkillScrollItems[i];
                if (item.SkillId == skillId)
                {
                    item.SetSilenced(silenced);
                }
            }
        }
    }
}
