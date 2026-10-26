using System;
using System.Collections.Generic;

namespace FusionContinent.Model
{
    // 消息发布者类
    public class MessagePublisher
    {
        public const string CHANGE_USER_NAME = "ChageUserName";

        // 用户头像变化时广播，载荷为 HeadConfig.Id（如 10001）
        public const string CHANGE_USER_AVATAR = "ChangeUserAvatar";

        // 用户头像框变化时广播，载荷为 HeadConfig.Id（如 11001）
        public const string CHANGE_USER_AVATAR_FRAME = "ChangeUserAvatarFrame";
        
        public const string CHANGE_STAMINA = "ChageStamina";
        public const string CHANGE_ITEM_NUM = "ChangeItemNum:";
        public const string CHANGE_CARD_DATA = "CHANGE_CARD_DATA:";
        public const string CHANGE_ITEM_CHANGE = "ChangeItemChange";
        public const string CHANGE_CARD_CHANGE = "CHANGE_CARD_CHANGE";
        public const string TRIGGER_GUIDE_TIP = "TriggerGuideTip";
        public const string MAIN_CITY_GUIDE_TIP = "MainCtiyGuideTip";
        public const string CHANGE_USER_LEVEL_INFO = "ChangeUserLevelInfo";
        public const string CHANGE_UPGRADE_FOOT_ENABLED = "ChangeUpgradeFootEnabled";
        public const string CHANGE_ACCUMULATE_CHARGE_NUM = "ChangeAccumulateChargeNum";
        public const string CHANGE_SHOP_BANNER_FOOT_ENABLED = "ChangeShopBannerFootEnabled";
        public const string CHANGE_GAME_MAIN_FOOT_BTN_ENABLED = "ChangeGameMainFootBtnEnabled";
        public const string CHANGE_PLAYER_MAIN_FOOT_BTN_ENABLED = "ChangePlayerMainFootBtnEnabled";
        public const string CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED = "ChangeGuideFlowPointerBtnEnabled";
        public const string GEMGAME_SCORE_CHANGE = "GEMGAME_SCORE_CHANGE";
        //技能冷却完成
        public const string CHANGE_GAME_CARD_COOLING = "CHANGE_GAME_CARD_COOLING";
        public const string CHANGE_GAME_CARD_RELEASE_COMPEPLETE = "CHANGE_GAME_CARD_RELEASE_COMPEPLETE";
        public const string CHANGE_GAME_SETTLE = "CHANGE_GAME_SETTLE";
        
        // 变更流派星级
        public const string CHANGE_SCHOOL_STAR = "ChangeSchoolStar";
        
        // 解锁流派
        public const string UNLOCK_SCHOOL = "UnlockSchool";
        
        // 流派卡牌飞入特效
        public const string SCHOOL_CARD_FLY_EFFECT = "SchoolCardFlyEffect";
        
        // 技能冷却开始
        public const string SKILL_COOLING_STARTED = "SkillCoolingStarted";

        // 技能冷却初始化完成
        public const string SKILL_COOLING_INITIALIZED = "SkillCoolingInitialized";

        // 战斗中选中英灵
        public const string BATTLE_SOLDIER_SELECTED = "BattleSoldierSelected";

        //引导Mask打开
        public const string CHANGE_GUIDE_STATUS = "CHANGE_GUIDE_STATUS";
        public const string CHANGE_GUIDE_INTERACTION = "CHANGE_GUIDE_INTERACTION";
        public const string CHANGE_GUIDE_PROGRESS = "CHANGE_GUIDE_PROGRESS";
        public const string CHANGE_GUIDE_PROGRESS_SPE = "CHANGE_GUIDE_PROGRESS_SPE";
        public const string CHANGE_GUIDE_SELECT_CARD = "CHANGE_GUIDE_SELECT_CARD";
        public const string CHANGE_GUIDE_PROGRESS_DONE = "CHANGE_GUIDE_PROGRESS_DONE";
        // 活动数据更新
        public const string CHANGE_ACTIVITY_INFO = "CHANGE_ACTIVITY_INFO";

        // 红点
        public const string CHANGE_RED_TYPE = "CHANGE_RED_TYPE";

        // 怪物图鉴状态变化（领取奖励后），无载荷，由 MonsterAtlasPanel 订阅以刷新列表
        public const string MONSTER_ATLAS_STATUS_CHANGED = "MonsterAtlasStatusChanged";
        
        // 卡牌皮肤变更（觉醒后）
        public const string CARD_SKIN_CHANGED = "CARD_SKIN_CHANGED";
        
        // 刷新士兵技能列表
        public const string REFRESH_SOLDIER_SKILL_LIST = "REFRESH_SOLDIER_SKILL_LIST";
        
        // 刷新士兵奥术卡列表
        public const string REFRESH_SOLDIER_ARCANE_LIST = "REFRESH_SOLDIER_ARCANE_LIST";
        
        // 觉醒失败
        public const string AWAKENING_FAILED = "AWAKENING_FAILED";
        
        // 显示提示
        public const string SHOW_TIP = "SHOW_TIP";
        
        // 关闭面板
        public const string CLOSE_PANEL = "CLOSE_PANEL";
        
        // 显示技能详情
        public const string SHOW_SKILL_DETAIL = "SHOW_SKILL_DETAIL";

        // 英灵升级完成
        public const string CARD_UPGRADED = "CARD_UPGRADED";

        // 轮语者融合石穿戴列表变更（UseGemList 发生变化：穿戴/卸下成功后触发）
        public const string SPEAKER_FUSION_GEM_WEAR_LIST_CHANGED = "SPEAKER_FUSION_GEM_WEAR_LIST_CHANGED";

        public const string FUSION_GEM_WEAR_CHANGED = "FUSION_GEM_WEAR_CHANGED";
        public const string FRIEND_LIST_CHANGED = "FRIEND_LIST_CHANGED";
        public const string FRIEND_APPLY_LIST_CHANGED = "FRIEND_APPLY_LIST_CHANGED";
        public const string FRIEND_SERVICE_DATA_CHANGED = "FRIEND_SERVICE_DATA_CHANGED";
        public const string MAIL_LIST_CHANGED = "MAIL_LIST_CHANGED";
        public const string MAIL_SERVICE_DATA_CHANGED = "MAIL_SERVICE_DATA_CHANGED";
        public const string MAIL_RED_DOT_CHANGED = "MAIL_RED_DOT_CHANGED";
        public const string CHAT_WORLD_CHANNEL_MESSAGE_RECEIVED = "CHAT_WORLD_CHANNEL_MESSAGE_RECEIVED";
        public const string CHAT_SYSTEM_CHANNEL_MESSAGE_RECEIVED = "CHAT_SYSTEM_CHANNEL_MESSAGE_RECEIVED";

        /// <summary>
        /// 玩家装备数据变更（穿戴/卸下等更新 Global.PlayerEquips 成功后发布，payload 可为 null）
        /// </summary>
        public const string PLAYER_EQUIP_DATA_CHANGED = "PLAYER_EQUIP_DATA_CHANGED";

        // 英灵技能穿戴列表变更
        public const string TEAM_HERO_SKILL_WEAR_CHANGED = "TEAM_HERO_SKILL_WEAR_CHANGED";
        
        /// <summary>
        /// 城墙数据变更（升级/激活/保存槽位成功后发布，payload 可为 skillId）
        /// </summary>
        public const string CITY_WALL_DATA_CHANGED = "CITY_WALL_DATA_CHANGED";

        /// <summary>
        /// 武器数据变更（激活/升级成功后发布，payload 为 weaponId 配置 id）
        /// </summary>
        public const string WEAPON_DATA_CHANGED = "WEAPON_DATA_CHANGED";

        /// <summary>
        /// 轮语者当前穿戴武器变更（payload 为 weaponId 配置 id，0 表示卸下）
        /// </summary>
        public const string SPEAKER_EQUIPPED_WEAPON_CHANGED = "SPEAKER_EQUIPPED_WEAPON_CHANGED";

        /// <summary>
        /// 轮语者皮肤数据变更（激活/升级成功后发布，payload 为 skinId 配置 id）
        /// </summary>
        public const string SPEAKER_SKIN_DATA_CHANGED = "SPEAKER_SKIN_DATA_CHANGED";

        /// <summary>
        /// 轮语者当前穿戴皮肤变更（payload 为 skinId 配置 id，0 表示卸下）
        /// </summary>
        public const string SPEAKER_EQUIPPED_SKIN_CHANGED = "SPEAKER_EQUIPPED_SKIN_CHANGED";

        public const string CHANGE_UNLOCK_GUIDE_CHANGE =  "CHANGE_UNLOCK_GUIDE_CHANGE";

        // 挂机收益界面关闭（需要跑每日弹出链的关闭路径时发布，无载荷），主界面监听后延迟一帧触发 DailyPopUpLogic
        public const string STAGE_SWEEPING_PANEL_CLOSED = "STAGE_SWEEPING_PANEL_CLOSED";
        
        // 存储订阅键和对应的回调函数列表
        private readonly Dictionary<string, List<Action<object>>> _subscribers = new Dictionary<string, List<Action<object>>>();

        // 订阅方法，根据订阅键和回调函数进行订阅
        public void Subscribe(string key, Action<object> callback, object onceRunObj = null)
        {
            if (!_subscribers.ContainsKey(key))
            {
                _subscribers[key] = new List<Action<object>>();
            }
            _subscribers[key].Add(callback);

            // 立即执行一次
            if (onceRunObj != null) callback(onceRunObj);
        }

        // 取消订阅方法，根据订阅键和回调函数取消订阅
        public void Unsubscribe(string key, Action<object> callback)
        {
            if (_subscribers.ContainsKey(key))
            {
                _subscribers[key].Remove(callback);
            }
        }
        public void UnsubscribeKeyAll(string key)
        {
            if (_subscribers.ContainsKey(key))
            {
                _subscribers.Remove(key);
            }
        }

        // 发布消息方法，根据订阅键和消息内容发布消息
        public void Publish(string key, object message)
        {
            if (!_subscribers.TryGetValue(key, out var callbacks) || callbacks == null || callbacks.Count == 0)
            {
                return;
            }

            // 回调里可能 Subscribe/Unsubscribe 同源事件（如穿装备触发红点 Sync），必须先拷贝再遍历
            var snapshot = callbacks.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                var callback = snapshot[i];
                if (callback != null)
                {
                    callback(message);
                }
            }
        }
    }

}
