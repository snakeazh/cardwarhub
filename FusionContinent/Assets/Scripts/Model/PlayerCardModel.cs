using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Utils;
using Game.CombatPower;

namespace FusionContinent.Model
{
    public class PlayerCard
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int CardId { get; set; } // 卡牌ID
        public int Lv { get; set; } // 等级
        public int Exp { get; set; } // 等级经验
        public bool IsHave { get; set; } // 是否拥有
        public int Num { get; set; } //卡牌数量
        public CardDataStruct ObjData { get; set; } // 对象数据

        public CardBaseConfig Config { get; set; } // 卡牌配置

        public PlayerCard() { }
        
        public PlayerCard(PlayerCard v)
        {
            CardId = v.CardId;
            Config = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(CardId);
            if (Config == null)
            {
                throw new Exception($"卡牌[{CardId}], 基础卡牌配置为空");
            }

            Id = v.Id;
            UserId = v.UserId;
            
            Lv = v.Lv;
            Exp = v.Exp;
            IsHave = true;
            Num = v.Num;
            ObjData = CloneCardData(v.ObjData);
        }

        private static CardDataStruct CloneCardData(CardDataStruct source)
        {
            if (source == null)
            {
                return new CardDataStruct();
            }

            return new CardDataStruct
            {
                Skin = source.Skin,
                IsAwaked = source.IsAwaked,
                ReceiveDevelops = source.ReceiveDevelops != null
                    ? new List<int>(source.ReceiveDevelops)
                    : new List<int>(),
            };
        }


        // 设置角色皮肤（服务器）
        public async void SetSkin(int skinId, Action callback = null)
        {
            if (ObjData.Skin == skinId) return;
            
            try
            {
                CombatPowerChangeHelper.CaptureBaseline();

                var request = new Cardwar.SetSkinRequest { CardId = CardId, SkinId = skinId };
                var proto = await WebDataFun.SendProtoPostRequest<Cardwar.SetSkinResponse>(ApiKeys.ApiCardSetSkin, request);

                if (proto == null || proto.Card == null)
                {
                    throw new Exception("服务器返回空响应");
                }

                // 更新本地数据（protobuf -> 本地模型）
                var localCard = ProtoMappers.MapPlayerCard(proto.Card);
                Global.PlayerCards[localCard.CardId] = localCard;
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }
                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                if (callback != null) callback();
            }
            catch (System.Exception ex)
            {
                var errorMsg = $"设置引导状态失败：{ex.Message}";
                if (GameMainPanel.Default != null) FusionContinent.Toast.Show(errorMsg);
                LogTool.LogError(errorMsg);
            }
        }
        
    }
    
    public class CardDataStruct
    {
        public int Skin; // 皮肤Id
        public List<int> ReceiveDevelops; // 领取养成
        public bool IsAwaked; // 是否觉醒
    }
    
    // 返回数据
    public class CardResult
    {
        public PlayerCard Card { get; set; } 
        public List<RewardItem> Rewards { get; set; } // 奖励内容
        public ItemGroup Items { get; set; } // 同步道具
    }

    public class DecompositionCardResult
    {
        public Dictionary<string, int> Rewards { get; set; }
        public ItemGroup Items { get; set; } // 同步道具
    }
    
    public class CompositionCardResult
    {
        public int CardId { get; set; }
        public ItemGroup Items { get; set; } // 同步道具
    }
}