using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using FusionContinent.Utils;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace FusionContinent
{
    public class CreatePlayerPanelData : UIPanelData
    {
    }

    public partial class CreatePlayerPanel : UIPanel
    {
        private bool sexplayer;
        private int sexType = 0;
        private TbPlayerFigureConfig _tbPlayerFigureConfig;
        private List<PlayerFigureConfig> boyPlayerFigures = new List<PlayerFigureConfig>();
        private List<PlayerFigureConfig> girlPlayerFigures = new List<PlayerFigureConfig>();

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as CreatePlayerPanelData ?? new CreatePlayerPanelData();

            // please add init code here
        }

        private void Awake()
        {
            _tbPlayerFigureConfig = DataConfigManager.Tables.TbPlayerFigureConfig;
            InitFigureBtn();
        }

        private void InitFigureBtn()
        {
            for (int i = 0; i < _tbPlayerFigureConfig.DataList.Count; i++)
            {
                // if (_tbPlayerFigureConfig.DataList[i].Sex == SexType.Man)
                // {
                //     boyPlayerFigures.Add(_tbPlayerFigureConfig.DataList[i]);
                // }
                // else if (_tbPlayerFigureConfig.DataList[i].Sex == SexType.Girl)
                // {
                //     girlPlayerFigures.Add(_tbPlayerFigureConfig.DataList[i]);
                // }
            }
        }

        private void CreateFigurebtns()
        {
            List<PlayerFigureConfig> figures = !sexplayer ? boyPlayerFigures : girlPlayerFigures;

            // 清除所有现有按钮
            ClearAllButtonParents();
            

            // 为每个配置创建按钮
            for (int i = 0; i < figures.Count; i++)
            {
                var config = figures[i];

            }
        }

// 清除所有按钮父物体下的子物体
        private void ClearAllButtonParents()
        {
            ClearChildren(Hair_accessoriesbtns);
            ClearChildren(expressionbtns);
            ClearChildren(Hairbtns);
            ClearChildren(bodybtns);
        }

// 清除指定父物体下的所有子物体
        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

// 创建单个角色配置按钮
        private void CreateFigureButton(PlayerFigureConfig config, Transform parent, string prefix, int index)
        {
            GameObject buttonPrefab =  Global.ResLoader.LoadSync<GameObject>("goodsbtn1").Instantiate();
            buttonPrefab.transform.SetParent(parent);
            buttonPrefab.SetActive(true);
            buttonPrefab.transform.localPosition = Vector3.one;
            buttonPrefab.transform.localScale = Vector3.one;

            // 设置图标
            Image iconImage = buttonPrefab.transform.Find("icon").GetComponent<Image>();
            if (iconImage != null)
            {
                // iconImage.sprite = Global.PlayerFigureIconSpriteAtlas.GetSprite(config.Icon);
            }

            // 设置按钮点击事件
            Button button = buttonPrefab.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => { SwitchPlayer(sexplayer, prefix + config.Name); });
            }
        }

        private void Start()
        {
            sexboybtn.onClick.AddListener(() =>
            {
                sexplayer = false;
                Playerboy.SetActive(true);
                Playergirl.SetActive(false);
                sexType = 0;
                CreateFigurebtns();
            });
            sexgirlbtn.onClick.AddListener(() =>
            {
                sexplayer = true;
                Playerboy.SetActive(false);
                Playergirl.SetActive(true);
                sexType = 1;
                CreateFigurebtns();
            });
            CreateFigurebtns();
            startgamebtn.onClick.AddListener(PayGame);
            RandomNamebtn.onClick.AddListener(RandomUserName);

            // 玩家名
            if (transform.Find("Player/Inputnametxt") != null)
            {
                playernameinput = transform.Find("Player/Inputnametxt").GetComponent<TMP_InputField>();
                playernameinput.onValueChanged.AddListener((string input) =>
                {
                    playernameinput.text = SensitiveCharacterTool.Filter(input);
                    var maxLength = 10;
                    if (input.Length > maxLength)
                    {
                        FusionContinent.Toast.Show("名称不能超过10个字符");
                        return;
                    }
                });
                RandomUserName();
            }
        }

        private void OnEnable()
        {
            if (confirmbtn != null)
            {
                startgamebtn.gameObject.SetActive(false);
                confirmbtn.onClick.RemoveAllListeners();
                confirmbtn.onClick.AddListener(() =>
                {
                    SavePlayer();
                });
            }

            if (this.gameObject.name == "CreatePlayerMaincityPanel")
            {
                startgamebtn.gameObject.SetActive(false);
            }
        }

        public void InitPlayer()
        {
        }

        private async void SavePlayer(string userName = "", Action callback = null)
        {
            startgamebtn.interactable = false;
            try
            {
                // var sex = (int)sexType;
                // var figure = Global.Player.FigureData;
                // FusionContinent.Toast.Show("操作中....");
                // var response = await WebDataFun.SendPostRequest(ApiKeys.ApiPlayerSavePlayer,
                //     new Dictionary<string, string>()
                //     {
                //         { "UserName", userName },
                //         { "Sex", sex.ToString() },
                //         { "Figures", JsonConvert.SerializeObject(figure) },
                //     });
                //
                // if (!string.IsNullOrEmpty(response))
                // {
                //     var result = JsonConvert.DeserializeObject<SavePlayerResult>(response);
                //     Global.User = result.User;
                //     Global.Player = new UserPlayer(result.Player);
                //
                //     if (callback != null) callback();
                // }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"操作失败：{ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
            }
            finally
            {
                startgamebtn.interactable = true;
            }
        }

        private async void PayGame()
        {
            if (UIFramework.Services.GameServiceManager.GetOrCreate<Game.Team.TeamService>().BuildBattleTeamsForCurrentTalent())
            {
                FusionContinent.Toast.Show("请配置卡组");
                return;
            }

            startgamebtn.interactable = false;
            try
            {
                var guideStageId = DataConfigManager.Tables.TbPlayerConst.GuidesStageId;
                if (guideStageId == 0)
                {
                    throw new Exception("未配置新手引导关卡ID");
                }
                
                var userName = playernameinput.text.Trim();
                if (userName == "")
                {
                    throw new Exception("名称不能为空");
                }
                
                Global.StageId.Value = guideStageId;
                SavePlayer(userName, () =>
                {
                    BattleModel.Start(() =>
                    {
                        LogTool.Log($"战斗开始");
                        UIKit.CloseAllPanel();

                        SceneLoader.Load("GamePlay");
                        Global.ResetProperties();
                        Global.Difficulty.Value = 1;
                        Global.GameReviveCount.Value = 0; // 重置游戏复活次数
                    }, (err) => { FusionContinent.Toast.Show(err); });
                });
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"操作失败：{ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
            }
            finally
            {
                startgamebtn.interactable = true;
            }
        }

        private void RandomUserName()
        {
            playernameinput.text = Utils.RandomTool.RandomUserName(sexType);
        }

        public void SwitchPlayer(bool sex, string decoration)
        {
            GameObject player = sex ? Playergirl : Playerboy;
            player.transform.Find(decoration).localScale = Vector3.one;
            string currentindex = decoration.Split('_')[1];
            if (player.transform.Find(decoration).localScale == Vector3.one)
            {
                for (int i = 0; i < player.transform.childCount; i++)
                {
                    if (player.transform.GetChild(i).name.Split('_')[0] == decoration.Split('_')[0])
                    {
                        if (player.transform.GetChild(i).name.Split('_')[1]!= currentindex)
                        {
                            player.transform.GetChild(i).localScale = Vector3.zero;
                        }
                    }
                }
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }
    }
}