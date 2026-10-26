using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using DG.Tweening;
using FusionContinent.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Spine.Unity;
using Spine;
using TMPro;

namespace FusionContinent
{
    public class CreatePlayerMaincityPanelData : UIPanelData
    {
    }

    public partial class CreatePlayerMaincityPanel : UIPanel
    {
        class LandAttributeStruct
        {
            public TMP_Text text;
            public float value;
        }

        public Transform ClothingBgIcon;

        private TbPlayerFigureConfig _playerFigureConfig;
        // private Dictionary<PlayerFigureType, int> playerClothingUp = new Dictionary<PlayerFigureType, int>();
        // private Dictionary<PlayerFigureType, string> playerClothingName = new Dictionary<PlayerFigureType, string>();
        //
        private Dictionary<AttributeType, GameObject> playerAttributes = new Dictionary<AttributeType, GameObject>();
        // private Dictionary<PlayerFigureType, GameObject> playerClothingSpineEffect =
            // new Dictionary<PlayerFigureType, GameObject>();

        private TbAttributeConfig _attributeConfig;

        private TbGameBattleConst _battleConst;

        public SkeletonGraphic skeletonGraphic;
        private string lastBtnStringName;

        // 存储当前激活的插槽附件
        private Dictionary<string, Attachment> activeAttachments = new Dictionary<string, Attachment>();
        
        private Dictionary<QualityType,Color32> _qualityTYpeFrame=new Dictionary<QualityType, Color32>();
        private Dictionary<QualityType,Color32> _qualityTYpeFrameOut=new Dictionary<QualityType, Color32>();
        private UIPoolScope mPoolScope;
        private const string ClothingPoolGroup = "ClothingObj";
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as CreatePlayerMaincityPanelData ?? new CreatePlayerMaincityPanelData();
        }

        private void Awake()
        {
            mPoolScope = UIPoolScope.Get(gameObject);
            mPoolScope.ReleaseOnDisable = true;
            mPoolScope.ReleaseOnDestroy = true;
            _playerFigureConfig = DataConfigManager.Tables.TbPlayerFigureConfig;
            _attributeConfig = DataConfigManager.Tables.TbAttributeConfig;
            _battleConst=DataConfigManager.Tables.TbGameBattleConst;
            _qualityTYpeFrame.Clear();
            _qualityTYpeFrameOut.Clear();
            foreach (var typecolor in _battleConst.QualityTypeFrameColors)
            {
                Color32 qulityColorFrame = new Color32((byte)typecolor.Color.X, (byte)typecolor.Color.Y, (byte)typecolor.Color.Z, 255);
                _qualityTYpeFrame.Add(typecolor.Type, qulityColorFrame);
            }
            foreach (var typecolor in _battleConst.QualityTypeFrameOutColors)
            {
                Color32 qulityColorFrameOut = new Color32((byte)typecolor.Color.X, (byte)typecolor.Color.Y, (byte)typecolor.Color.Z, 255);
                _qualityTYpeFrameOut.Add(typecolor.Type, qulityColorFrameOut);
            }
        }

        private void OnEnable()
        {
            InitPlayer();
        }

        private void InitPlayer()
        {
            InitPlayerClothingTitle();
            ResetSkeletonToSetupPose();
            CloseAllSlots();
            ClothingShow.clothingConfigFunc = SetPlayerClothing;
            // playerClothingSpineEffect.Clear();
            InitPlayerFigures();
        }

        /**
         * 初始玩家佩戴形象
         */
        private void InitPlayerFigures()
        {
            foreach (var figureId in Global.Player.FigureData)
            {
                var figureCfg = _playerFigureConfig.GetOrDefault(figureId);
                if (figureCfg == null) continue;
                SetPlayerClothing(figureCfg.Id);
            }
        }

        private void ResetSkeletonToSetupPose()
        {
            if (skeletonGraphic != null && skeletonGraphic.Skeleton != null)
            {
                skeletonGraphic.Skeleton.SetToSetupPose();
                skeletonGraphic.LateUpdate();
            }
        }

        public GameObject LastBtn;

        private void InitPlayerClothingTitle()
        {
            // var playerFigureTypeList = DataConfigManager.Tables.TbPlayerConst.PlayerFigureTypeList;
            // foreach (var figureTypeStruct in playerFigureTypeList)
            // {
            //     PlayerFigureType currentType = figureTypeStruct.Type;
            //
            //     GameObject playertitleObj = Instantiate(PlayerTitleObj.gameObject, PlayerTitleParent);
            //     playertitleObj.gameObject.SetActive(true);
            //
            //     playertitleObj.transform.Find("Text").GetComponent<TMP_Text>().text = figureTypeStruct.Name;
            //
            //     // 使用枚举作为键
            //     playerClothingUp.TryAdd(currentType, 0);
            //
            //     playertitleObj.GetComponent<Button>().onClick.AddListener(() =>
            //     {
            //         if (LastBtn != null)
            //         {
            //             LastBtn.transform.Find("SelectedImg").localScale = Vector3.zero;
            //         }
            //
            //         LastBtn = playertitleObj;
            //         playertitleObj.transform.Find("SelectedImg").localScale = Vector3.one;
            //         // CommonItemPoolManager.Instance.GetGameObjectPool("ClothingObj").ReturnAllEffect();
            //         // 直接使用枚举值，不需要字符串转换
            //         InitClothing(currentType);
            //     });
            //     playertitleObj.GetComponent<Button>().onClick.Invoke();
            // }
            // 触发第一个按钮的点击事件

            DOTween.To(v => { }, 0, 0, 0.01f).onComplete += () =>
            {
                if (PlayerTitleParent.childCount > 1)
                {
                    PlayerTitleParent.GetChild(1).GetComponent<Button>().onClick.Invoke();
                }
            };
        }
        
        // private void InitClothing(PlayerFigureType playerfigureType)
        // {
        //     mPoolScope?.ReleaseGroup(ClothingPoolGroup);
        //     SetClothIcon(playerfigureType);
        //     for (int i = 0; i < _playerFigureConfig.DataList.Count; i++)
        //     {
        //         PlayerFigureConfig playerFigureConfig = _playerFigureConfig.DataList[i];
        //         if (playerFigureConfig.Type == playerfigureType)
        //         {
        //             var clothingpool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate(ClothingPoolGroup);
        //             GameObject clothingObj = clothingpool?.Get();
        //             if (clothingObj == null)
        //             {
        //                 break;
        //             }
        //             
        //             mPoolScope?.Track(clothingpool, clothingObj, ClothingPoolGroup);
        //             clothingObj.transform.SetParent(ClothingParent);
        //             clothingObj.transform.localScale = Vector3.one;
        //             clothingObj.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        //             string name = playerFigureConfig.Name;
        //             // clothingObj.transform.Find("Frame").GetComponent<Image>().color = _qualityTYpeFrame[playerFigureConfig.Quality];
        //             // clothingObj.transform.Find("FrameOut").GetComponent<Image>().color = _qualityTYpeFrameOut[playerFigureConfig.Quality];
        //             clothingObj.GetComponent<Button>().onClick.RemoveAllListeners();
        //             clothingObj.GetComponent<Button>().onClick.AddListener(() =>
        //             {
        //                 ClothingShow.SetClothing(playerFigureConfig,
        //                     playerClothingUp[playerfigureType] == playerFigureConfig.Id);
        //             });
        //             
        //             if (playerClothingUp[playerfigureType] == playerFigureConfig.Id)
        //             {
        //                 Frame.SetParent(clothingObj.transform);
        //                 Frame.SetSiblingIndex(0);
        //                 Frame.transform.localScale = Vector3.one;
        //                 Frame.transform.localPosition = Vector3.zero + new Vector3(0,-6.5f,0);
        //                 Frame.gameObject.SetActive(true);
        //             }
        //             
        //             // 引导 - 点击选择装扮
        //             // var stepCfg = GuideFlow.Instance.GetCurrentStepCfg(GuideType.DecorativeWear, 1);
        //             // if (stepCfg != null && stepCfg.SkipPath.Count > 0)
        //             // {
        //             //     int paramId = int.Parse(stepCfg.SkipPath.First().Param);
        //             //     if (paramId > 0 && paramId == playerFigureConfig.Id)
        //             //     {
        //             //         GuideFlow.Instance.SetStep(GuideType.DecorativeWear, 1, clothingObj.transform);
        //             //     }
        //             // }
        //
        //             clothingObj.transform.Find("Icon").GetComponent<Image>().sprite = Global
        //                 .PlayerFigureIconSpriteAtlas
        //                 .GetSprite(playerFigureConfig.Icon);
        //         }
        //     }
        //
        //     if (playerClothingUp[playerfigureType] == 0)
        //     {
        //         Frame.transform.SetParent(this.transform);
        //         Frame.gameObject.SetActive(false);
        //     }
        // }

        /// <summary>
        /// 设置装备
        /// </summary>
        public void SetPlayerClothing(int figureId)
        {
            // PlayerFigureType part = _playerFigureConfig.GetOrDefault(figureId).Type;
            // PlayerFigureConfig clothing = _playerFigureConfig.GetOrDefault(figureId);
            // List<string> SpineNames = new List<string>();
            //
            // // ========== 先移除旧装备的显示 ==========
            // if (playerClothingUp.ContainsKey(part) && playerClothingUp[part] != 0)
            // {
            //     SpineNames = _playerFigureConfig.GetOrDefault(playerClothingUp[part]).ShowSpine;
            //     HideSlot(SpineNames);
            //
            //     // 移除旧装备的属性
            //     var oldClothing = _playerFigureConfig.GetOrDefault(playerClothingUp[part]);
            //     for (int i = 0; i < oldClothing.Attributes.Count; i++)
            //     {
            //         var attr = oldClothing.Attributes[i];
            //         if (playerAttributes.ContainsKey(attr.Type))
            //         {
            //             Destroy(playerAttributes[attr.Type].gameObject);
            //             playerAttributes.Remove(attr.Type);
            //         }
            //     }
            //
            //     // 清空旧特效
            //     if (playerClothingSpineEffect.ContainsKey(part))
            //     {
            //         for (int i = playerClothingSpineEffect[part].transform.childCount - 1; i >= 0; i--)
            //         {
            //             Destroy(playerClothingSpineEffect[part].transform.GetChild(i).gameObject);
            //         }
            //     }
            //     playerClothingSpineEffect.Clear();
            // }
            //
            // // ========== 判断是否卸下装备 ==========
            // if (figureId == playerClothingUp[part])
            // {
            //     playerClothingUp[part] = 0;
            // }
            // else
            // {
            //     // 设置新装备
            //     playerClothingUp[part] = clothing.Id;
            //     SpineNames = clothing.ShowSpine;
            //     ShowSlot(SpineNames);
            //
            //     // 添加新特效
            //     if (clothing.SpineEffect.Count > 0)
            //     {
            //         if (!playerClothingSpineEffect.ContainsKey(part))
            //         {
            //             GameObject playerClothingSpine = new GameObject();
            //             playerClothingSpine.transform.SetParent(EffectParent);
            //             playerClothingSpine.name = part.ToString();
            //             playerClothingSpine.transform.localPosition=Vector3.zero;
            //             playerClothingSpine.transform.localScale=Vector3.one;
            //             playerClothingSpineEffect.Add(part, playerClothingSpine);
            //         }
            //
            //         for (int i = 0; i < clothing.SpineEffect.Count; i++)
            //         {
            //             GameObject effect = Global.ResLoader
            //                 .LoadSync<GameObject>(clothing.SpineEffect[i].Name).Instantiate();
            //             effect.transform.SetParent(playerClothingSpineEffect[part].transform);
            //             effect.transform.localScale = Vector3.one*250;
            //             effect.transform.localPosition = new Vector3(clothing.SpineEffect[i].Value.X,
            //                 clothing.SpineEffect[i].Value.Y);
            //         }
            //     }
            //
            //     // ========== 添加新装备的属性 ==========
            //     for (int i = 0; i < clothing.Attributes.Count; i++)
            //     {
            //         var attr = clothing.Attributes[i];
            //         if (!playerAttributes.ContainsKey(attr.Type))
            //         {
            //             // GameObject attribute = i <= AttributeParent.childCount ? AttributeParent.GetChild(i).gameObject : Instantiate(AttributeParent.GetChild(0).gameObject, AttributeParent.transform);
            //             GameObject attribute = Instantiate(AttributeParent.GetChild(0).gameObject, AttributeParent.transform);
            //             attribute.transform.localScale = Vector3.one;
            //             attribute.gameObject.SetActive(true);
            //             if (attr.Type != AttributeType.SoldierCriticalRate)
            //                 attribute.GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts,_attributeConfig.GetOrDefault(attr.Type).Name,
            //                 $":+{attr.Value}");
            //             else
            //             {
            //                 attribute.GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts,_attributeConfig.GetOrDefault(attr.Type).Name,
            //                     $":+{attr.Value * 100f}%");
            //             }
            //             playerAttributes.Add(attr.Type, attribute);
            //         }
            //     }
            // }
            //
            // // CommonItemPoolManager.Instance.GetGameObjectPool("ClothingObj").ReturnAllEffect();
            // InitClothing(part);
        }


        /// <summary>
        /// 关闭所有的插槽
        /// </summary>
        private void CloseAllSlots()
        {
            for (int i = 0; i < _playerFigureConfig.DataList.Count; i++)
            {
                PlayerFigureConfig playerFigureConfig = _playerFigureConfig.DataList[i];

                // HideSlot(playerFigureConfig.ShowSpine);
            }

            //ResetSkeletonToSetupPose();
        }

        /// <summary>
        /// 隐藏插槽
        /// </summary>
        private void HideSlot(List<string> slotNames)
        {
            if (slotNames == null || skeletonGraphic == null || skeletonGraphic.Skeleton == null) return;

            var skeleton = skeletonGraphic.Skeleton;
            foreach (var slotName in slotNames)
            {
                var slot = skeleton.FindSlot(slotName);
                if (slot != null)
                {
                    // 保存当前附件状态（如果需要恢复）
                    if (!activeAttachments.ContainsKey(slotName))
                    {
                        activeAttachments[slotName] = slot.Attachment;
                    }

                    slot.Attachment = null;
                }
                else
                {
                    LogTool.LogWarning($"尝试隐藏不存在的插槽: {slotName}");
                }
            }

            skeletonGraphic.LateUpdate();
        }

        /// <summary>
        /// 显示插槽
        /// </summary>
        private void ShowSlot(List<string> slotNames)
        {
            if (slotNames == null || slotNames.Count == 0 ||
                skeletonGraphic == null || skeletonGraphic.Skeleton == null)
            {
                return;
            }

            var skeleton = skeletonGraphic.Skeleton;
            bool anySlotUpdated = false;

            foreach (var slotName in slotNames)
            {
                var slot = skeleton.FindSlot(slotName);
                if (slot == null)
                {
                    continue;
                }

                // 方法1: 尝试获取默认附件
                var attachment = skeleton.GetAttachment(slot.Data.Index, slotName);

                // 方法2: 如果方法1失败，尝试从皮肤获取
                if (attachment == null && skeleton.Skin != null)
                {
                    attachment = skeleton.Skin.GetAttachment(slot.Data.Index, slotName);
                }

                // 方法3: 如果方法2失败，尝试从默认皮肤获取
                if (attachment == null && skeleton.Data.DefaultSkin != null)
                {
                    attachment = skeleton.Data.DefaultSkin.GetAttachment(slot.Data.Index, slotName);
                }

                if (attachment != null)
                {
                    slot.Attachment = attachment;
                    activeAttachments[slotName] = attachment;
                    anySlotUpdated = true;
                }
                else
                {
                    // 尝试恢复之前保存的附件
                    if (activeAttachments.TryGetValue(slotName, out var previousAttachment))
                    {
                        slot.Attachment = previousAttachment;
                        anySlotUpdated = true;
                    }
                }
            }

            if (anySlotUpdated)
            {
                skeletonGraphic.LateUpdate();
            }
        }

        // private void SetClothIcon(PlayerFigureType playerfigureType)
        // {
        //     string name = "";
        //     switch (playerfigureType)
        //     {
        //         case PlayerFigureType.Wing:
        //             name = "WingEmblem";
        //             break;
        //         case PlayerFigureType.Clothes:
        //             name = "ClothingLabel";
        //             break;
        //         case PlayerFigureType.Facing:
        //             name = "FaceMaskLogo";
        //             break;
        //         case PlayerFigureType.Hair:
        //             name = "HairEmblem";
        //             break;
        //         case PlayerFigureType.Hat:
        //             name = "HatEmblem";
        //             break;
        //         case PlayerFigureType.Shoes:
        //             name = "ShoeLogo";
        //             break;
        //         case PlayerFigureType.Trousers:
        //             name = "TrouserLabel";
        //             break;
        //         case PlayerFigureType.Weapon:
        //             name = "WeaponEmblem"; 
        //             break;
        //         default:
        //             throw new ArgumentOutOfRangeException(nameof(playerfigureType), playerfigureType, null);
        //     }
        //     ClothingBgIcon.GetComponent<Image>().sprite = Global.BigWorldIconAtlas.GetSprite(name);
        // }
        //
        /// <summary>
        /// 调试函数：打印所有可用插槽
        /// </summary>
        private void DebugAllSlots()
        {
            if (skeletonGraphic == null || skeletonGraphic.Skeleton == null) return;

            var skeleton = skeletonGraphic.Skeleton;
            foreach (var slot in skeleton.Slots)
            {
                LogTool.Log($"插槽: {slot.Data.Name}, 当前附件: {(slot.Attachment != null ? slot.Attachment.Name : "null")}");
            }
        }

        /// <summary>
        /// 调试函数：打印所有皮肤和附件
        /// </summary>
        private void DebugAllAttachments()
        {
            if (skeletonGraphic == null || skeletonGraphic.Skeleton == null) return;

            var skeleton = skeletonGraphic.Skeleton;
            LogTool.Log("=== 所有皮肤 ===");
            foreach (var skin in skeleton.Data.Skins)
            {
                LogTool.Log($"皮肤: {skin.Name}");
                foreach (var attachment in skin.Attachments)
                {
                    LogTool.Log($"  插槽索引: {attachment.SlotIndex}, 附件: {attachment.Name}");
                }
            }
        }


        public string CapitalizeFirstSmallLetter(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToLower(input[0]) + input.Substring(1);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
            mPoolScope?.ReleaseAll();
        }

        protected override void OnClose()
        {
            mPoolScope?.ReleaseAll();
        }

        private async void SaveFigure()
        {
            try
            {
                // var figures = playerClothingUp.Values.Where(figureId => figureId > 0).ToList();
                // if (string.Join(",", figures) == string.Join(",", Global.Player.FigureData)) return;
                //
                // var response = await WebDataFun.SendPostRequest(ApiKeys.ApiPlayerSavePlayer,
                //     new Dictionary<string, string>
                //     {
                //         { "Figures", JsonConvert.SerializeObject(figures) },
                //     });
                //
                // if (!string.IsNullOrEmpty(response))
                // {
                //     var result = JsonConvert.DeserializeObject<SavePlayerResult>(response);
                //     Global.User = result.User;
                //     Global.Player = new UserPlayer(result.Player);
                //
                //     FusionContinent.Toast.Show("保存形象成功", 0.5f);
                //     
                //     // 完成引导
                //     GuideFlow.Instance.SetStepComplete(GuideType.DecorativeWear);
                // }
            }
            catch (System.Exception ex)
            {
                FusionContinent.Toast.Show(ex.Message);
                LogTool.LogError($"保存编队失败：{ex.Message}");
            }
        }

        private void OnDisable()
        {
            SaveFigure();
        }
    }
}