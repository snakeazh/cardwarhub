using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.Serialization;

namespace FusionContinent
{
    public class NormalShopList01Data : UIPanelData
    {
    }

    public partial class NormalShopList01 : UIPanel
    {
        private TbShopConfig _shopConfig;
        private TbShopGoodsConfig _goodsConfig;
        private TbItemConfig _itemConfig;

        public ShopItem shopitem;
        public GameObject normalShopTitleList;
        public GameObject normalShop;

        [FormerlySerializedAs("shopitemTitleparent")] public Transform shopitem01Titleparent;
        private Dictionary<QualityType, Color32> _qualityTypeColors = new Dictionary<QualityType, Color32>();
        private Dictionary<QualityType, string> _shopqualityTypeBg = new Dictionary<QualityType, string>();

        private void Awake()
        {
            _shopConfig = DataConfigManager.Tables.TbShopConfig;
            _goodsConfig = DataConfigManager.Tables.TbShopGoodsConfig;
            _itemConfig = DataConfigManager.Tables.TbItemConfig;
            var qualityTypeColorList = DataConfigManager.Tables.TbGameBattleConst.QualityTypeColors;
            var _shopBg = DataConfigManager.Tables.TbGameBattleConst.ShopQualityType;
            foreach (var v in qualityTypeColorList)
            {
                // _qualityTypeColors.Add(v.Type, new Color32(138,146,122, 255));
                _qualityTypeColors.Add(v.Type, new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f));
            }

            foreach (var v in _shopBg)
            {
                // _qualityTypeColors.Add(v.Type, new Color32(138,146,122, 255));
                _shopqualityTypeBg.Add(v.Type, v.ShopBg);
            }
        }

        private void OnDisable()
        {
            // CommonItemPoolManager.Instance.GetGameObjectPool("ShopItemTitleObj").ReturnAllEffect();
        }

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as NormalShopList01Data ?? new NormalShopList01Data();
            // please add init code here
        }

        private Dictionary<int, List<ShopGoodsConfig>> _shopGoodsCache = new Dictionary<int, List<ShopGoodsConfig>>();
        private List<GameObject> _activeShopItems = new List<GameObject>();
        private Queue<GameObject> _inactiveShopItems = new Queue<GameObject>();
        private Dictionary<int, ItemConfig> _itemConfigMap = new Dictionary<int, ItemConfig>();


        public int LastShopid;

        /// <summary>
        /// 设置商店物体(商店类型)
        /// </summary>
        /// <param name="shopType"></param>
        /// <param name="isTitle"></param>
        public void SetInitShopItem(ShopType shopType, bool isTitle = false)
        {
            var shopId = 0;
            foreach (var cfg in _shopConfig.DataList)
            {
                if (cfg.Type == shopType)
                {
                    shopId = cfg.Id;
                    break;
                }
            }
            
            if (shopId == 0) return;
            SetInitShopItem(shopId, isTitle);
        }

        /// <summary>
        /// 设置商店物体
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="isTitle"></param>
        public void SetInitShopItem(int shopId, bool isTitle = false)
        {
            this.istitle = isTitle;
            // 回收标题对象
            var titlePool = CommonItemPoolManager.Instance.GetGameObjectPool("ShopItemTitleObj");
            // titlePool.ReturnAllEffect();

// 清空标题父节点
            for (int i = 0; i < shopitemTitleparent.childCount; i++)
            {
                shopitemTitleparent.GetChild(i).gameObject.SetActive(false);
            }

            SwitchShopPanel(shopId);

            var goodsList = GetGoodsList(shopId);
            RecycleExtraShopItems(goodsList.Count);

            string buyitemid = "";
            GameObject shopitemTitleObj = null;

            for (int i = 0; i < goodsList.Count; i++)
            {
                var goods = goodsList[i]; // 局部变量捕获
                var shopitemObj = GetShopItemObject(i);
                var shopitemcs = shopitemObj.GetComponent<ShopItem>();

                // 每次刷新都获取最新玩家商品数据
                var goodsInfo = Global.GetGoodsById(goods.Id);

                // 重置和更新 UI
                ResetShopItemUI(shopitemcs);
                UpdateShopItem(shopitemcs, goods);
                UpdateConsumeUI(shopitemcs, goods);
                ApplyLimitUI(shopitemcs, goods, goodsInfo);
                // 10002 特殊标题逻辑
                if (isTitle)
                {
                    shopitemTitleObj = HandleTitleShop(shopitemcs, goods, buyitemid, shopitemTitleObj);
                    buyitemid = goods.Category;
                }

                // 点击事件，每次点击重新获取 goodsInfo
                {
                    var localGoods = goods; // 局部变量，闭包安全
                    var localShopItem = shopitemcs;
                    BindShopClick(localShopItem, localGoods);
                }
            }

            LastShopid = shopId;
            RefreshTitleLayout();
        }
        
        private void RefreshTitleLayout()
        {
            if (istitle)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(shopitemTitleparent.GetComponent<RectTransform>());
                UIHelper.SafeRefreshLayout(shopitemTitleparent.GetComponent<RectTransform>());
            }
        }

        private bool istitle;

        private void SwitchShopPanel(int shopid)
        {
            bool isTitleShop = istitle;
            normalShop.SetActive(!isTitleShop);
            normalShopTitleList.SetActive(isTitleShop);
        }
        
        /// <summary>
        /// 更新消耗品
        /// </summary>
        /// <param name="item"></param>
        /// <param name="goods"></param>
        private void UpdateConsumeUI(ShopItem item, ShopGoodsConfig goods)
        {
            if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
            {
                var consume = goods.BuyConsumes[0]; // 这里只取第一个消耗品
                var itemCfg = _itemConfig.GetOrDefault(consume.Id);

                // 设置图标
                if (itemCfg != null)
                {
                    item.consumeicon.sprite = Global.ItemIconSpriteAtlas.GetSprite(itemCfg.Icon);
                }

                long playerItemCount = Global.GetItemNumById(consume.Id);
                item.consumenum.text = consume.Num.ToString();
                item.consumenum.color = playerItemCount < consume.Num ? Color.red : Color.white;
                LayoutRebuilder.ForceRebuildLayoutImmediate(item.consumeicon.transform.parent
                    .GetComponent<RectTransform>());
                UIHelper.SafeRefreshLayout(item.consumeicon.transform.parent.GetComponent<RectTransform>());
                // 按钮是否可点
                item.shopitembg.GetComponent<Button>().interactable = true;

                // RMB 特殊处理
                if (consume.Id == 1010005)
                {
                    item.consumeicon.gameObject.SetActive(false);
                    item.consumenum.gameObject.SetActive(false);
                    item.consumebtn.transform.Find("rmbtxt").gameObject.SetActive(true);
                    item.consumebtn.transform.Find("rmbtxt").GetComponent<TMP_Text>().text = consume.Num + "元";
                }
                else
                {
                    item.consumeicon.gameObject.SetActive(true);
                    item.consumenum.gameObject.SetActive(true);
                    item.consumebtn.transform.Find("rmbtxt").gameObject.SetActive(false);
                }
            }
            else
            {
                // 免费物品
                item.consumeicon.gameObject.SetActive(false);
                item.consumenum.gameObject.SetActive(false);
                item.consumebtn.transform.Find("freetxt").gameObject.SetActive(true);
                item.shopitembg.GetComponent<Button>().interactable = true;
            }
        }

        /// <summary>
        /// 获取物品并排序
        /// </summary>
        /// <param name="shopid"></param>
        /// <returns></returns>
        private List<ShopGoodsConfig> GetGoodsList(int shopid)
        {
            var list = _goodsConfig.DataList
                .Where(x => x.ShopId == shopid)
                .OrderBy(x => x.Id)
                .ToList();

            if (shopid == 10005)
            {
                System.Random random = new System.Random();
                list = list.OrderBy(x => random.Next()).Take(9).ToList();
            }

            return list;
        }

        private void RecycleExtraShopItems(int itemCount)
        {
            while (_activeShopItems.Count > itemCount)
            {
                var obj = _activeShopItems[^1];
                obj.SetActive(false);
                _activeShopItems.RemoveAt(_activeShopItems.Count - 1);
                _inactiveShopItems.Enqueue(obj);
            }
        }

        private GameObject GetShopItemObject(int index)
        {
            GameObject obj;
            if (index < _activeShopItems.Count)
            {
                obj = _activeShopItems[index];
            }
            else
            {
                obj = _inactiveShopItems.Count > 0
                    ? _inactiveShopItems.Dequeue()
                    : Instantiate(shopitem.gameObject, shopitemparent.transform);

                _activeShopItems.Add(obj);
            }

            obj.SetActive(true);
            obj.transform.SetParent(shopitemparent.transform, false);
            // obj.transform.localScale = Vector3.one * 0.9f;
            obj.transform.SetSiblingIndex(index + 1);

            // 🔹 固定 Z 坐标
            Vector3 pos = obj.transform.localPosition;
            pos.z = 30f;
            obj.transform.localPosition = pos;

            return obj;
        }
        /// <summary>
        /// 重置商品
        /// </summary>
        /// <param name="item"></param>
        private void ResetShopItemUI(ShopItem item)
        {
            item.shopitembgmask.gameObject.SetActive(false);
            // item.buylimitnum.gameObject.SetActive(false);
            // item.shopItemBuyLImitIcon.gameObject.SetActive(false);
            item.consumenum.gameObject.SetActive(false);
            item.consumenum.gameObject.SetActive(false);
            item.consumenum.color = Color.white;
            item.consumebtn.transform.Find("freetxt").gameObject.SetActive(false);
            item.consumebtn.transform.Find("rmbtxt").gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 更新商品数据
        /// </summary>
        /// <param name="item"></param>
        /// <param name="goods"></param>
        private void UpdateShopItem(ShopItem item, ShopGoodsConfig goods)
        {
            item.SetShopItemId(goods.Id);
            item.shopnametxt.text = goods.Name;
            item.ShopNums.text = goods.BuyItemNum.ToString();

            // 商品图标 & 数量
            item.shopicon.sprite = Global.ShopGoodsIconAtlas.GetSpriteExt(goods.Shopgoodsicon);
            item.shopnum.text = goods.BuyItemNum.ToString();
            item.shopiconbg.color = _qualityTypeColors[goods.Quality];
        }

// 本次登录会话内的“本地购买计数”，用于弥补首次购买 Global 还没数据的问题
        private Dictionary<int, int> _localBuyCounts = new Dictionary<int, int>();

        private void ApplyLimitUI(ShopItem item, ShopGoodsConfig goods, PlayerGoods goodsInfo = null)
        {
            // 强制获取最新服务器端数据（如果外部已传则使用该值）
            goodsInfo ??= Global.GetGoodsById(goods.Id);
            int serverBuyNum = goodsInfo != null ? goodsInfo.BuyNum : 0;

            // 本地会话已买次数（用于第一次购买后立即显示）
            int localBuyNum = 0;
            _localBuyCounts.TryGetValue(goods.Id, out localBuyNum);
            if (serverBuyNum != 0)
            {
                localBuyNum = 0;
            }

            int totalBuyNum = serverBuyNum + localBuyNum;

            bool hasLimit = goods.LimitNum != 0;
            // item.buylimitnum.gameObject.SetActive(hasLimit);
            // item.shopItemBuyLImitIcon.gameObject.SetActive(hasLimit);
            
            if (hasLimit)
            {
                int remain = Mathf.Max(goods.LimitNum - totalBuyNum, 0);
                item.buylimitnum.text = $"限购{remain}/{goods.LimitNum}";
                item.shopitembg.sprite = Global.ShopIconSpriteAtlas.GetSpriteExt($"{goods.Quality}2");
                item.shopicon.transform.localScale = Vector3.one;
                // item.shopicon.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(3.98f, -35f);

            }
            else
            {
                item.shopitembg.sprite = Global.ShopIconSpriteAtlas.GetSpriteExt($"{goods.Quality}3");
                item.shopicon.transform.localScale = Vector3.one * 1.5f;
                // item.shopicon.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(3.98f, -87f);
            }

            // 限购已满显示遮罩
            item.shopitembgmask.gameObject.SetActive(hasLimit && totalBuyNum >= goods.LimitNum);
        }

        private void RefreshShopItemById(ShopItem si)
        {
            if (si == null) return;
            int id = si.shopItemID; // 确保你在初始化时有调用 SetShopItemId(goods.Id)
            if (id == 0) return;

            var g = _goodsConfig.DataList.FirstOrDefault(x => x.Id == id);
            if (g == null) return;

            var info = Global.GetGoodsById(id); // 可能为 null
            UpdateConsumeUI(si, g);
            ApplyLimitUI(si, g, info);
        }

        private void RefreshAllItemsInCurrentPanel()
        {
            if (istitle)
            {
                var all = shopitemTitleparent.GetComponentsInChildren<ShopItem>(true);
                foreach (var si in all) RefreshShopItemById(si);
            }
            else
            {
                var all = shopitemparent.GetComponentsInChildren<ShopItem>(true);
                foreach (var si in all) RefreshShopItemById(si);
            }
        }

        private IEnumerator DelayRefresh(RectTransform rect)
        {
            yield return null; // 等待一帧
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            UIHelper.SafeRefreshLayout(rect);
        }
        /// <summary>
        /// 带标题的
        /// </summary>
        /// <param name="item"></param>
        /// <param name="goods"></param>
        /// <param name="lastshoptype"></param>
        /// <param name="lastTitleObj"></param>
        /// <returns></returns>
        private GameObject HandleTitleShop(ShopItem item, ShopGoodsConfig goods, string lastshoptype,
            GameObject lastTitleObj)
        {
            if (lastshoptype != goods.Category)
            {
                var pool = CommonItemPoolManager.Instance.GetGameObjectPool("ShopItemTitleObj");
                var titleObj = pool.Get();
                if (titleObj == null) return lastTitleObj;

                titleObj.transform.SetParent(shopitemTitleparent);
                titleObj.transform.localScale = Vector3.one;
                StartCoroutine(DelayRefresh(titleObj.GetComponent<RectTransform>()));
                titleObj.transform.Find("Title/TitleName").GetComponent<TMP_Text>().text =
                    goods.Category;
                item.transform.SetParent(titleObj.transform.Find("ShopItemParent"));
                item.transform.localScale = Vector3.one;
                return titleObj;
            }

            if (lastTitleObj != null)
            {
                item.transform.SetParent(lastTitleObj.transform.Find("ShopItemParent"));
                item.transform.localScale = Vector3.one;
            }

            return lastTitleObj;
        }

        // 在类级别添加
        private Dictionary<int, bool> _purchasingStates = new Dictionary<int, bool>();

        private void BindShopClick(ShopItem item, ShopGoodsConfig goods)
        {
            var button = item.shopitembg.GetComponent<Button>();
            //防止重复绑定：如果已经绑定过这个goods.Id，就不再绑定
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // 防止重复点击同一商品
                if (_purchasingStates.ContainsKey(goods.Id) && _purchasingStates[goods.Id])
                    return;

                // 获取最新数据
                var currentGoodsInfo = Global.GetGoodsById(goods.Id);
                int currentBuyNum = currentGoodsInfo?.BuyNum ?? 0;

                _localBuyCounts.TryGetValue(goods.Id, out int localCount);
                int totalBought = currentBuyNum + localCount;

                // 限购检查
                if (goods.LimitNum != 0 && totalBought >= goods.LimitNum)
                {
                    FusionContinent.Toast.Show("购买次数不足");
                    return;
                }

                // 消耗品检查
                if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
                {
                    var consume = goods.BuyConsumes[0];
                    long playerItemCount = Global.GetItemNumById(consume.Id);
                    if (playerItemCount < consume.Num)
                    {
                        string itemName = _itemConfig.GetOrDefault(consume.Id)?.Name ?? "物品";
                        FusionContinent.Toast.Show(itemName + "不足");
                        return;
                    }
                }

                GameMainPanel.Default.ShowConfirm("购买确认", $"是否消耗{Items.GetRewardsStr(goods.BuyConsumes)}", () =>
                {
                    // 设置购买状态锁
                    _purchasingStates[goods.Id] = true;

                    // 更新本地计数和UI
                    if (_localBuyCounts.ContainsKey(goods.Id))
                        _localBuyCounts[goods.Id] += 1;
                    else
                        _localBuyCounts[goods.Id] = 1;

                    RefreshAllItemsInCurrentPanel();

                    // 发送购买请求
                    currentGoodsInfo.BuyGoods(1, () =>
                    {
                        // 购买成功，释放锁
                        _purchasingStates[goods.Id] = false;
                        RefreshAllItemsInCurrentPanel();
                        FusionContinent.Toast.Show("购买成功");
                    });
                });
            });
        }


        private void Start()
        {
            //SetInitShopItem(10003);
        }

        private void OnEnable()
        {
            //SetInitShopItem(10002);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            //SetInitShopItem(10002);
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