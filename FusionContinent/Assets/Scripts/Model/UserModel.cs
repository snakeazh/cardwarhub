using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Manager;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UIFramework.Services;
using Object = UnityEngine.Object;

namespace FusionContinent.Model
{
    public class UserData
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int Avatar { get; set; }
        public int AvatarFrame { get; set; }
        public int Level { get; set; }
        
        public int LastDisplayLevel
        {
            get
            {
                return PlayerPrefs.GetInt("LastDisplayLevel" + UserId, 0);
            }
            set
            {
                PlayerPrefs.SetInt("LastDisplayLevel" + UserId, value);
            }
        }

        public int Exp { get; set; }
        public int Sex { get; set; }
        public int CombatNum { get; set; }
        public int FightingNum { get; set; }
        public List<int> GuidesList { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime CreateTime { get; set; }

        // 设置等级信息
        public void SetLevelInfo(UserLevelInfo info)
        {
            Exp = info.Exp;
            Level = info.Level;
            
            // 通知订阅等级信息修改
            Global.Publisher.Publish(MessagePublisher.CHANGE_USER_LEVEL_INFO, info);
        }

        public const int DefaultAvatarId = 10001;

        /// <summary>当前佩戴头像的 HeadConfig.Id，0 时回退默认头像。</summary>
        public int GetAvatarHeadConfigId()
        {
            if (Avatar > 0) return Avatar;
            return DefaultAvatarId;
        }

        /// <summary>头像展示序号（1、2、3…），用于图集 sprite 名。</summary>
        public int GetAvatarDisplayIndex()
        {
            var id = GetAvatarHeadConfigId();
            if (id >= 10001 && id < 11000) return id - 10000;
            return id;
        }

        /// <summary>PlayerHeadAtlas 使用的 sprite 名（如 Avatar1）。</summary>
        public string GetAvatarSpriteName()
        {
            return "Avatar" + GetAvatarDisplayIndex();
        }

        /// <summary>AvatarIconSpriteAtlas 使用的 sprite 名（如 Head_1）。</summary>
        public string GetAvatarIconSpriteName()
        {
            return "Head_" + GetAvatarDisplayIndex();
        }

        /// <summary>将服务端 avatar 字符串解析为 HeadConfig.Id。</summary>
        public static int ParseAvatarFromProto(string avatar)
        {
            if (string.IsNullOrEmpty(avatar)) return DefaultAvatarId;

            int n;
            if (int.TryParse(avatar, out n))
            {
                if (n >= 10001 && n < 11000) return n;
                if (n >= 1) return 10000 + n;
            }

            var digits = "";
            for (int i = avatar.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(avatar[i])) digits = avatar[i] + digits;
                else break;
            }
            if (digits.Length > 0 && int.TryParse(digits, out n) && n >= 1)
            {
                return 10000 + n;
            }

            LogTool.LogWarning("[UserData] 无法识别的 Avatar 格式: " + avatar + "，回退为默认头像");
            return DefaultAvatarId;
        }
        

        public UserLevelInfo GetLevelInfo()
        {
            return new UserLevelInfo(Level, Exp);
        }

        
    }
    
    public class UserLevelInfo
    {
        public int Exp { get; set; }
        public int Level { get; set; }

        public UserLevelInfo(int lv, int exp)
        {
            Level = lv;
            Exp = exp;
        }
    }

    public static class UserModel
    {
        // 注销
        public static void LogOut()
        {
            UIFramework.UIManager.CloseAllPanels();
            Global.User = new UserData();
            Global.Publisher = new MessagePublisher();

            // UIKit.ClosePanel<AdventurePanel>()
            // UIKit.ClosePanel<GameMainPanel>();;
            if (GameObject.Find("UIRoot/CanvasUpList/UpList") != null)
            {
                GameObject.Destroy(GameObject.Find("UIRoot/CanvasUpList/UpList"));
            }
            if (GameObject.Find("UIRoot/CanvasUpList/UserInfo") != null)
            {
                GameObject.Destroy(GameObject.Find("UIRoot/CanvasUpList/UserInfo"));
            }

            if(ConditionManager.Instance)
                Object.Destroy(ConditionManager.Instance.gameObject);   
               
            UIKit.CloseAllPanel(false);
            if(UnlockGuideManager.Instance)
                Object.Destroy(UnlockGuideManager.Instance.gameObject);
            

            
            GameServiceManager.Get<Game.RedDot.RedDotService>().Reset();   
            SceneLoader.Load("GameStart");
        }
    }
}
