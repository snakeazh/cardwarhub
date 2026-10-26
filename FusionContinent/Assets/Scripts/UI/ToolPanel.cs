using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Cardwar;
using Config;
using DG.Tweening;
using FusionContinent.Attribute;
using FusionContinent.Model;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.U2D;
using TMPro;
using ValueType = Config.ValueType;


namespace FusionContinent
{
    public class ToolPanelData : UIPanelData
    {
    }

    public partial class ToolPanel : UIPanel
    {
        public static ToolPanel Default;
        private ResLoader _resLoader;
        private TbCardBaseConfig _cardBaseConfig;
        private TbItemConfig _itemConfig;

        private Dictionary<string, ValueType> _valueTypes = new Dictionary<string, ValueType>()
        {
            { "数值", ValueType.Number },
            { "百分比", ValueType.Percentage },
        };

        // 道具全量列表缓存（供搜索过滤用）
        private List<ItemConfig> _allItemConfigs;
        // 当前过滤后实际展示的道具列表（Dropdown 下标与之一一对应）
        private readonly List<ItemConfig> _filteredItemConfigs = new List<ItemConfig>();
        private bool _itemDropdownInited;

        // 道具类型下拉：下标与 ItemType 一一对应
        private readonly List<ItemType> _itemTypes = new List<ItemType>();
        private bool _itemTypeDropdownInited;

        private void Awake()
        {
            Default = this;
            _resLoader = ResLoader.Allocate();
            _cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _itemConfig = DataConfigManager.Tables.TbItemConfig;
        }

        protected override void OnInit(IUIData uiData = null)
        {
            if (Global.Debug.Value != 1)
            {
                UIKit.HidePanel<ToolPanel>();
                return;
            }

            CloseBtn.onClick.AddListener(OnClose);
            SendItemBtn.onClick.AddListener(SendItems);
            SendItemAllBtn.onClick.AddListener(SendItemsAll);
            
            GamePauseBtn.onClick.AddListener(GamePause);
            AddAttrBtn.onClick.AddListener(AddAttributes);
            GameSuccessBtn.onClick.AddListener(GameSuccess);
            AddSchoolBtn.onClick.AddListener(AddSchoolCard);
            FullScreenHarmBtn.onClick.AddListener(FullScreenHarm);

        }

        private void ShowContent(string type = "item")
        {
            ItemContent.gameObject.SetActive(type == "item");
            GameContent.gameObject.SetActive(type == "game");
            SchoolContent.gameObject.SetActive(type == "school");
        }

        private readonly List<string> _itemOptions = new List<string> { "请选择道具" };
        public void SetMainInterfacePanel()
        {
            ShowContent("item");

            // 只初始化一次：缓存全量列表 + 绑定搜索框
            if (!_itemDropdownInited)
            {
                _allItemConfigs = DataConfigManager.Tables.TbItemConfig.DataList;
                _itemDropdownInited = true;

                RefreshItemDropdown(string.Empty);

                // 搜索框：输入时实时过滤
                if (ItemSearchInput != null)
                {
                    ItemSearchInput.onValueChanged.RemoveAllListeners();
                    ItemSearchInput.onValueChanged.AddListener(RefreshItemDropdown);
                }
            }

            InitItemTypeDropdown();
        }

        /// <summary>
        /// 初始化道具类型下拉：选中后将该类型下所有道具按 id_数量 拼接到 ItemsInput
        /// </summary>
        private void InitItemTypeDropdown()
        {
            if (_itemTypeDropdownInited) return;
            _itemTypeDropdownInited = true;

            var options = new List<string> { "请选择道具类型" };
            _itemTypes.Clear();
            foreach (ItemType itemType in Enum.GetValues(typeof(ItemType)))
            {
                _itemTypes.Add(itemType);
                options.Add($"{(int)itemType} - {itemType}");
            }

            ItemTypeDropdown.ClearOptions();
            ItemTypeDropdown.AddOptions(options);
            ItemTypeDropdown.value = 0;
            ItemTypeDropdown.RefreshShownValue();
            ItemTypeDropdown.onValueChanged.RemoveAllListeners();
            ItemTypeDropdown.onValueChanged.AddListener(OnItemTypeSelected);
        }

        private void OnItemTypeSelected(int idx)
        {
            if (idx <= 0) return;
            if (idx - 1 >= _itemTypes.Count) return;

            var itemType = _itemTypes[idx - 1];
            var matched = _itemConfig.DataList.Where(c => c.Type == itemType).ToList();
            if (matched.Count == 0)
            {
                LogTool.Log($"道具类型 {itemType} 没有对应道具");
                return;
            }

            var joined = string.Join("|", matched.Select(c => $"{c.Id}_1"));
            LogTool.Log($"选择的道具类型 - {itemType}, 共 {matched.Count} 个");

            if (ItemsInput.text != "")
            {
                ItemsInput.text += "|";
            }
            ItemsInput.text += joined;
        }

        /// <summary>
        /// 根据搜索关键字刷新 ItemDropdown 选项列表
        /// 关键字匹配 Id 或 Name（不区分大小写）
        /// </summary>
        private void RefreshItemDropdown(string keyword)
        {
            if (_allItemConfigs == null) return;

            var dropdown = ItemDropdown.GetComponent<Dropdown>();

            // 重建 options
            _itemOptions.Clear();
            _itemOptions.Add("请选择道具");

            // 用于 onValueChanged 里反查当前选中的 cfg
            _filteredItemConfigs.Clear();

            keyword = (keyword ?? string.Empty).Trim().ToLower();
            bool hasKeyword = !string.IsNullOrEmpty(keyword);

            foreach (var cfg in _allItemConfigs)
            {
                if (hasKeyword)
                {
                    bool matchId = cfg.Id.ToString().Contains(keyword);
                    bool matchName = (cfg.Name ?? string.Empty).ToLower().Contains(keyword);
                    if (!matchId && !matchName) continue;
                }

                _filteredItemConfigs.Add(cfg);
                _itemOptions.Add($"{cfg.Id} - {cfg.Name}");
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(_itemOptions);
            dropdown.value = 0;
            dropdown.RefreshShownValue();

            // 绑定/重置选中回调：注意每次过滤后列表变了，回调要基于 _filteredItemConfigs 取值
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener((int idx) =>
            {
                if (idx <= 0) return;
                if (idx - 1 >= _filteredItemConfigs.Count) return;
                var itemCfg = _filteredItemConfigs[idx - 1];
                LogTool.Log($"选择的道具 - Id: {itemCfg.Id}, Name: {itemCfg.Name}");

                if (ItemsInput.text != "")
                {
                    ItemsInput.text += "|";
                }
                ItemsInput.text += $"{itemCfg.Id}_1";
            });
        }
        
        private readonly List<string> _schoolOptions = new List<string> { "请选择流派卡" };
        public void SetSchoolPanel()
        {
            ShowContent("school");
            
            // 初始化列表
            if (_schoolOptions.Count == 1)
            {
                var itemList = DataConfigManager.Tables.TbSchoolCardConfig.DataList;
                foreach (var cfg in itemList)
                {
                    _schoolOptions.Add($"{cfg.Id} - {cfg.Name}");
                }

                SchoolDropdown.GetComponent<Dropdown>().AddOptions(_schoolOptions);
                SchoolDropdown.RefreshShownValue();
                SchoolDropdown.onValueChanged.RemoveAllListeners();
                SchoolDropdown.onValueChanged.AddListener((int idx) =>
                {
                    if (idx <= 0) return;
                    var itemCfg = itemList[idx - 1];
                    LogTool.Log($"选择的流派卡 - idx: {idx - 1}, Name: {itemCfg.Name}");
                
                    if (SchoolInput.text != "")
                    {
                        SchoolInput.text += "|";
                    }
                    SchoolInput.text += $"{itemCfg.Id}";
                });
            }
            
        }
        
        private readonly List<string> _attrOptions = new List<string> { "请选择属性" };
        public void SetGameScenes()
        {
            ShowContent("game");
            
            // 初始化列表
            if (_attrOptions.Count == 1)
            {
                var attrList = DataConfigManager.Tables.TbAttributeConfig.DataList;
                foreach (var cfg in attrList)
                {
                    var attrName = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, cfg.Name, "");
                    _attrOptions.Add($"{attrName}-{cfg.AttributeType}");
                }

                AttrDropdown.GetComponent<Dropdown>().AddOptions(_attrOptions);
                AttrDropdown.RefreshShownValue();
                AttrDropdown.onValueChanged.RemoveAllListeners();
                AttrDropdown.onValueChanged.AddListener((int idx) =>
                {
                    if (idx <= 0) return;
                    var attrCfg = attrList[idx - 1];
                    LogTool.Log($"选择的属性 - idx: {idx - 1}, Name: {attrCfg.Name}");
                
                    if (AttrInput.text != "")
                    {
                        AttrInput.text += "|";
                    }
                    AttrInput.text += $"{attrCfg.AttributeType}:1:数值:0:0";
                });
            }
            
        }
        
        

        private async void SendItems()
        {
            try
            {
                string itemsStr = ItemsInput.text.Trim();
                if (itemsStr == "")
                {
                    throw new Exception("发送的道具不能为空");
                }

                var arr = itemsStr.Split("|");
                var items = new Dictionary<int, int>();
                foreach (var str in arr)
                {
                    var v = str.Split("_");
                    if (v.Length != 2)
                    {
                        throw new Exception("道具格式错误，请输入：道具id_数量");
                    }

                    var (itemId, num) = (int.Parse(v[0]), int.Parse(v[1]));
                    var cfg = _itemConfig.GetOrDefault(itemId);
                    if (cfg == null)
                    {
                        throw new Exception($"参数错误，itemId[ {itemId} ]配置不存在");
                    }

                    items.TryAdd(itemId, 0);
                    items[itemId] += num;
                }

                LogTool.Log(items);
                FusionContinent.Toast.Show("操作中....");
                SendItemBtn.GetComponent<Button>().interactable = false;
                
                var request = new AddItemsRequest();
                foreach (var kv in items)
                {
                    request.Items[kv.Key] = kv.Value;
                }
                var proto = await WebDataFun.SendProtoPostRequest<AddItemsResponse>(
                    ApiKeys.ApiAddItems, request);

                // 同步奖励
                Items.Update(proto.Items);
                
                FusionContinent.Toast.Show("操作成功");
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"操作失败：{ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
            }
            finally
            {
                SendItemBtn.GetComponent<Button>().interactable = true;
                CloseSelf();
            }
        }
        
        private async void SendItemsAll()
        {
            try
            {
                FusionContinent.Toast.Show("操作中....");
                SendItemAllBtn.GetComponent<Button>().interactable = false;

                var request = new AddItemsAllRequest();
                var proto = await WebDataFun.SendProtoPostRequest<AddItemsAllResponse>(
                    ApiKeys.ApiAddItemsAll, request);

                // 同步奖励
                Items.Update(proto.Items);
                
                FusionContinent.Toast.Show("操作成功");
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"操作失败：{ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
            }
            finally
            {
                SendItemAllBtn.GetComponent<Button>().interactable = true;
            }
        }


        private void AddAttributes()
        {
            AddAttrBtn.interactable = false;
            try
            {
                string attrStr = AttrInput.text.Trim();
                if (attrStr == "")
                {
                    throw new Exception("发送的道具不能为空");
                }

                var addMod = new AttributeModifier
                {
                    SourceKey = "",
                    SourceType = AttributeSourceType.ToolPanel,
                };
                foreach (var str in attrStr.Split("|"))
                {
                    var v = str.Split(":");
                    if (v.Length != 5)
                    {
                        throw new Exception("属性格式错误，请输入: 属性类型:数值:数值类型:生效参数:生效概率");
                    }
                    
                    LogTool.Log($">>>> TEST: AddAttributes.AttrData: {v}");
                    if (!_valueTypes.TryGetValue(v[2], out var valueType))
                    {
                        throw new Exception($"数值类型错误，不存在的: {v[2]}");
                    }

                    if (!Enum.TryParse(v[0], out AttributeType attrType))
                    {
                        throw new Exception($"属性类型错误，不存在的: {v[0]}");
                    }

                    // 添加属性逻辑
                    AttributeManager.SetAttributeLogic(addMod, new AttributeData()
                    {
                        Type = attrType,
                        Value = float.Parse(v[1]),
                        ValueType = valueType,
                        EffectParam = float.Parse(v[3]),
                        ProbabilityTrigger = float.Parse(v[4])
                    });
                }

                FusionContinent.Toast.Show("操作成功");
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"操作失败：{ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
            }
            finally
            {
                AddAttrBtn.interactable = true;
            }
        }
        
        private void AddSchoolCard()
        {
            AddSchoolBtn.interactable = false;
            try
            {
                string attrStr = SchoolInput.text.Trim();
                if (attrStr == "")
                {
                    throw new Exception("发送的SchoolCard不能为空");
                }

                if (!DataConfigManager.Tables.TbSchoolCardConfig.DataMap.Keys.Contains(int.Parse(attrStr)))
                {
                    throw new Exception("不存在这张流派卡");
                }

                SchoolManager.Instance.AddSchoolCard(int.Parse(attrStr));
                FusionContinent.Toast.Show("操作成功");
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"操作失败：{ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
            }
            finally
            {
                AddSchoolBtn.interactable = true;
            }
        }

        private void GamePause()
        {
            if (GamePlayManager.Default == null) return;

            if (GamePlayManager.Default.IsPaused())
            {
                GamePlayManager.Default.StartGame();
            }
            else
            {
                GamePlayManager.Default.PauseGame();
            }
        }

        private void FullScreenHarm()
        {
            AttributeManager.SetAttributeLogic(new AttributeModifier
            {
                SourceKey = "",
                SourceType = AttributeSourceType.ToolPanel,
            }, new AttributeData()
            {
                Type = AttributeType.TestFullScreenHarm,
                ValueType = ValueType.Number,
                Value = 1,
            });
        }

        void GameSuccess()
        {
            var btnText = GameSuccessBtn.transform.Find("Text").GetComponent<TMP_Text>();
            btnText.text = "结算中......";
            GameSuccessBtn.interactable = false;
            
            if (GamePlayManager.Default != null) 
                GamePlayManager.Default.GameSuccess();
            
            // 延时
            ActionKit.Delay(3f, () =>
            {
                btnText.text = "跳过战斗";
                GameSuccessBtn.interactable = true;
            });
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
            UnlockGuideManager.Instance.ClearAllSave();
            gameObject.SetActive(false);
        }

    }
}
