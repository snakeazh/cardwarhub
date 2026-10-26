using System;
using Game.Shop;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using System.Collections.Generic;
using FusionContinent.Manager;
using FusionContinent.Model;

namespace FusionContinent
{
    /// <summary>
    /// 每日商店列表 - 新框架版本 (MVVM)
    /// 复用原 DailyShopList 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("DailyShopList")]
    public class DailyShopListNew : UIFramework.UIPanel<DailyShopListViewModel>
    {
        private const string ShopItemPoolKey = "ShopItem2";
        private const string DailyShopGroup = "DailyShopItems";

        /// <summary>
        /// 作为商店子界面，不参与UIManager堆栈管理
        /// </summary>
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        // 引用原 Panel，复用其 UI 组件
        private DailyShopList mOldPanel;

        // 当前激活的商店项（仅用于购买后遍历刷新）
        private readonly List<GameObject> mActiveShopItems = new List<GameObject>();

        // 倒计时
        private DateTime mCachedServerTime;
        private float mLocalTimeWhenCached;
        private DateTime mTargetResetTime;
        private bool mCountdownInitialized;
        private float mCountdownTimer;

        void Awake()
        {
            mOldPanel = GetComponent<DailyShopList>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[DailyShopListNew] 找不到 DailyShopList 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            // 隐藏场景中预设的所有 shopitem 子节点（它们只是模板，通过对象池管理）
            if (mOldPanel.shopitemparent != null)
            {
                for (int i = 0; i < mOldPanel.shopitemparent.childCount; i++)
                {
                    mOldPanel.shopitemparent.GetChild(i).gameObject.SetActive(false);
                }
            }

            LogTool.Log("[DailyShopListNew] 已关联 DailyShopList");
        }

        void Update()
        {
            if (!mCountdownInitialized || mOldPanel?.TimeText == null) return;

            mCountdownTimer -= Time.deltaTime;
            if (mCountdownTimer <= 0f)
            {
                mCountdownTimer = 1f;
                RefreshCountdownDisplay();
            }
        }

        protected override void OnBindViewModel(DailyShopListViewModel vm)
        {
            if (mOldPanel == null)
            {
                LogTool.LogError("[DailyShopListNew] OnBindViewModel: mOldPanel 为 null");
                return;
            }

            // 绑定商品列表变化
            vm.GoodsList.OnValueChanged += (oldVal, newVal) =>
            {
                RefreshShopItems(vm);
            };

            // 绑定本地购买计数变化
            vm.LocalBuyCounts.OnValueChanged += (oldVal, newVal) =>
            {
                RefreshAllShopItems(vm);
            };

            // 立即刷新UI
            if (vm.CurrentShopId.Value > 0)
            {
                RefreshShopItems(vm);
            }
        }

        protected override void OnUnbindViewModel(DailyShopListViewModel vm)
        {
            ReturnAllShopItems();
        }

        #region 商店项管理

        /// <summary>
        /// 刷新商店商品列表
        /// </summary>
        public void RefreshShopItems(DailyShopListViewModel vm)
        {
            if (mOldPanel == null)
            {
                LogTool.LogError("[DailyShopListNew] RefreshShopItems: mOldPanel 为 null");
                return;
            }

            var goodsList = vm.GoodsList.Value;
            if (goodsList == null)
            {
                ReturnAllShopItems();
                return;
            }

            ReturnAllShopItems();

            for (int i = 0; i < goodsList.Count; i++)
            {
                var goods = goodsList[i];
                var shopItemObj = GetShopItemObject(i);
                if (shopItemObj == null) continue;

                var shopItem = shopItemObj.GetComponent<ShopItem2>();
                if (shopItem == null) continue;

                // 重置UI
                ResetShopItemUI(shopItem);

                // 更新商品数据
                UpdateShopItem(shopItem, goods, vm);

                // 更新消耗UI
                UpdateConsumeUI(shopItem, goods, vm);

                // 应用限购UI
                ApplyLimitUI(shopItem, goods, vm);

                // 绑定点击事件
                BindShopClick(shopItem, goods, vm);
            }
        }

        /// <summary>
        /// 刷新所有商店项（用于购买后更新）
        /// </summary>
        private void RefreshAllShopItems(DailyShopListViewModel vm)
        {
            foreach (var shopItemObj in mActiveShopItems)
            {
                var shopItem = shopItemObj.GetComponent<ShopItem2>();
                if (shopItem == null) continue;

                int goodsId = shopItem.shopItemID;
                var goods = vm.GetGoodsConfig(goodsId);
                if (goods == null) continue;

                UpdateConsumeUI(shopItem, goods, vm);
                ApplyLimitUI(shopItem, goods, vm);
            }
        }

        /// <summary>
        /// 获取或创建商店项（通过统一对象池）
        /// </summary>
        private GameObject GetShopItemObject(int index)
        {
            if (mOldPanel?.shopitemparent == null)
            {
                LogTool.LogError("[DailyShopListNew] GetShopItemObject: shopitemparent 为空");
                return null;
            }

            // 从 CommonItemPoolManager 获取 ShopItem 池，不存在则用场景模板创建
            var itemPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate(ShopItemPoolKey);
            if (itemPool == null)
            {
                var template = mOldPanel.shopitemparent.childCount > 0
                    ? mOldPanel.shopitemparent.GetChild(0).gameObject
                    : null;
                if (template == null)
                {
                    LogTool.LogError("[DailyShopListNew] GetShopItemObject: 模板为空");
                    return null;
                }
                itemPool = CommonItemPoolManager.Instance.CreateGameObjectPool(ShopItemPoolKey, template);
            }

            if (itemPool == null)
            {
                LogTool.LogError("[DailyShopListNew] GetShopItemObject: ShopItem 对象池创建失败");
                return null;
            }

            var obj = itemPool.Get(mOldPanel.shopitemparent.transform);
            if (obj == null) return null;

            PoolScope.Track(itemPool, obj, DailyShopGroup);
            mActiveShopItems.Add(obj);

            obj.SetActive(true);
            obj.transform.SetParent(mOldPanel.shopitemparent.transform, false);
            obj.transform.SetSiblingIndex(index + 1);

            return obj;
        }

        /// <summary>
        /// 回收所有商店项到对象池
        /// </summary>
        private void ReturnAllShopItems()
        {
            PoolScope.ReleaseGroup(DailyShopGroup);
            mActiveShopItems.Clear();
        }

        #endregion

        #region UI 更新

        private void ResetShopItemUI(ShopItem2 item)
        {
            item.shopitembgmask.gameObject.SetActive(false);
            item.consumenum.gameObject.SetActive(false);
            item.consumenum.color = Color.white;
            item.consumebtn.transform.Find("freetxt")?.gameObject.SetActive(false);
            item.consumebtn.transform.Find("rmbtxt")?.gameObject.SetActive(false);
            var discountNumRoot = item.transform.Find("shopitembg/DiscountNum");
            if (discountNumRoot != null)
            {
                discountNumRoot.gameObject.SetActive(false);
            }
            item.DiscountText?.gameObject.SetActive(false);
            item.consumeNumBefore?.gameObject.SetActive(false);
            item.consumeBtnDisC?.gameObject.SetActive(false);
            item.consumebtn.gameObject.SetActive(true);
            if (item.DisConsumeNum != null)
            {
                item.DisConsumeNum.gameObject.SetActive(false);
            }
        }

        private void UpdateShopItem(ShopItem2 item, ShopGoodsConfig goods, DailyShopListViewModel vm)
        {
            item.SetShopItemId(goods.Id);
            item.shopnametxt.text = goods.Name;
            item.ShopNums.text = goods.BuyItemNum.ToString();

            // 商品图标
            item.shopicon.sprite = Global.ShopGoodsIconAtlas.GetSpriteExt(goods.Shopgoodsicon);
            // item.shopicon.SetNativeSize();
            item.shopnum.text = goods.BuyItemNum.ToString();
            item.shopiconbg.color = vm.GetQualityColor(goods.Quality);

            // 折扣处理（每日商店专用）
            var discountRate = vm.DiscountRate.Value;
            var hasDiscount = discountRate > 0 && discountRate < 100;

            if (hasDiscount)
            {
                // 折扣标签
                if (item.DiscountText != null)
                {
                    item.transform.Find("shopitembg/DiscountNum").gameObject.SetActive(true);
                    item.DiscountText.gameObject.SetActive(true);
                    item.DiscountText.text = $"{discountRate / 10}折";
                }

                // 按钮切换：隐藏原价按钮，显示折扣按钮
                item.consumebtn.gameObject.SetActive(false);
                if (item.consumeBtnDisC != null)
                    item.consumeBtnDisC.gameObject.SetActive(true);

                // 价格：显示原价和折后价（使用折扣按钮下的专属字段）
                if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
                {
                    var consume = goods.BuyConsumes[0];
                    var discountedPrice = vm.GetDiscountedPrice(consume.Num, discountRate);

                    if (item.DisConsumeNum != null)
                    {
                        item.DisConsumeNum.text = discountedPrice.ToString();
                        item.DisConsumeNum.gameObject.SetActive(true);
                    }

                    if (item.consumeNumBefore != null)
                    {
                        item.consumeNumBefore.gameObject.SetActive(true);
                        item.consumeNumBefore.text = consume.Num.ToString();
                    }
                }
            }
            else
            {
                item.consumebtn.gameObject.SetActive(true);
                if (item.consumeBtnDisC != null)
                    item.consumeBtnDisC.gameObject.SetActive(false);
            }
        }

        private void UpdateConsumeUI(ShopItem2 item, ShopGoodsConfig goods, DailyShopListViewModel vm)
        {
            if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
            {
                var consume = goods.BuyConsumes[0];
                var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(consume.Id);

                var discountRate = vm.DiscountRate.Value;
                var hasDiscount = discountRate > 0 && discountRate < 100;
                int displayPrice = hasDiscount ? vm.GetDiscountedPrice(consume.Num, discountRate) : consume.Num;

                // 折扣激活时操作 consumeBtnDisC 下的专属字段，否则操作 consumebtn 下的字段
                var targetIcon = hasDiscount && item.DisConsumeIcon != null ? item.DisConsumeIcon : item.consumeicon;
                var targetNum = hasDiscount && item.DisConsumeNum != null ? item.DisConsumeNum : item.consumenum;

                if (itemCfg != null)
                {
                    targetIcon.sprite = Global.ItemIconSpriteAtlas.GetSprite(itemCfg.Icon);
                }

                long playerItemCount = Global.GetItemNumById(consume.Id);
                targetNum.text = displayPrice.ToString();
                targetNum.color = playerItemCount < displayPrice ? Color.red : Color.white;

                LayoutRebuilder.ForceRebuildLayoutImmediate(targetIcon.transform.parent.GetComponent<RectTransform>());

                item.shopitembg.GetComponent<Button>().interactable = true;

                // RMB 特殊处理
                if (consume.Id == 1010005)
                {
                    targetIcon.gameObject.SetActive(false);
                    targetNum.gameObject.SetActive(false);
                    var srcBtn = hasDiscount ? item.consumeBtnDisC : item.consumebtn;
                    var rmbTxt = srcBtn != null ? srcBtn.transform.Find("rmbtxt") : null;
                    if (rmbTxt != null)
                    {
                        rmbTxt.gameObject.SetActive(true);
                        rmbTxt.GetComponent<TMP_Text>().text = displayPrice + "元";
                    }
                }
                else
                {
                    targetIcon.gameObject.SetActive(true);
                    targetNum.gameObject.SetActive(true);
                    var srcBtn = hasDiscount ? item.consumeBtnDisC : item.consumebtn;
                    var rmbTxt = srcBtn != null ? srcBtn.transform.Find("rmbtxt") : null;
                    if (rmbTxt != null) rmbTxt.gameObject.SetActive(false);
                }
            }
            else
            {
                // 免费物品
                item.consumeicon.gameObject.SetActive(false);
                item.consumenum.gameObject.SetActive(false);
                var freeTxt = item.consumebtn.transform.Find("freetxt");
                if (freeTxt != null) freeTxt.gameObject.SetActive(true);
                item.shopitembg.GetComponent<Button>().interactable = true;
            }
        }

        private void ApplyLimitUI(ShopItem2 item, ShopGoodsConfig goods, DailyShopListViewModel vm)
        {
            var goodsInfo = vm.GetPlayerGoods(goods.Id);
            int serverBuyNum = goodsInfo?.BuyNum ?? 0;

            int localBuyNum = 0;
            if (vm.LocalBuyCounts.Value != null && vm.LocalBuyCounts.Value.ContainsKey(goods.Id))
            {
                localBuyNum = vm.LocalBuyCounts.Value[goods.Id];
            }
            if (serverBuyNum != 0) localBuyNum = 0;

            int totalBuyNum = serverBuyNum + localBuyNum;
            bool hasLimit = goods.LimitNum != 0;

            if (hasLimit)
            {
                item.shopitembg.sprite = Global.ShopItemAtlas.GetSprite($"{goods.Quality}");
                if (!Global.ShopItemAtlas.GetSprite($"{goods.Quality}"))
                    LogTool.LogError($"{goods.Quality} is null");
                item.shopicon.transform.localScale = Vector3.one;
            }
            else
            {
                var sp = Global.ShopItemAtlas.GetSprite($"{goods.Quality}");
                if (sp != null)
                {
                    item.shopitembg.sprite = sp;
                }
                else
                {
                    LogTool.LogError($"{goods.Quality} is null");
                }
                item.shopicon.transform.localScale = Vector3.one * 1.5f;
            }

            // 限购已满显示遮罩
            item.shopitembgmask.gameObject.SetActive(hasLimit && totalBuyNum >= goods.LimitNum);
        }

        #endregion

        #region 事件绑定

        private void BindShopClick(ShopItem2 item, ShopGoodsConfig goods, DailyShopListViewModel vm)
        {
            var button = item.shopitembg.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (!vm.CanBuy(goods.Id))
                {
                    FusionContinent.Toast.Show("购买条件不足");
                    return;
                }

                // 弹窗显示折后价
                string consumeStr;
                var discountRate = vm.DiscountRate.Value;
                if (discountRate > 0 && discountRate < 100 && goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
                {
                    var consume = goods.BuyConsumes[0];
                    var discountedNum = vm.GetDiscountedPrice(consume.Num, discountRate);
                    consumeStr = Items.GetRewardStr(consume.Id, discountedNum);
                }
                else
                {
                    consumeStr = vm.GetConsumeString(goods.BuyConsumes);
                }

                GameMainPanel.Default.ShowConfirm("购买确认", $"是否消耗{consumeStr}?", () =>
                {
                    vm.OnBuyGoodsCommand.Execute(goods.Id);
                });
            });
        }

        #endregion

        #region 刷新倒计时

        private void InitCountdown()
        {
            if (mOldPanel?.TimeText == null) return;

            if (ServerTimeManager.Instance == null)
            {
                mOldPanel.TimeText.text = "刷新倒计时: --:--:--";
                return;
            }

            mCachedServerTime = ServerTimeManager.Instance.GetDataTime();
            mLocalTimeWhenCached = Time.realtimeSinceStartup;

            int resetHour = DataConfigManager.Tables.TbGameConst.GameResetHour;
            mTargetResetTime = new DateTime(mCachedServerTime.Year, mCachedServerTime.Month, mCachedServerTime.Day,
                resetHour, 0, 0);

            if (mCachedServerTime >= mTargetResetTime)
            {
                mTargetResetTime = mTargetResetTime.AddDays(1);
            }

            mCountdownInitialized = true;
            mCountdownTimer = 0f;
        }

        private void RefreshCountdownDisplay()
        {
            float elapsed = Time.realtimeSinceStartup - mLocalTimeWhenCached;
            DateTime currentServerTime = mCachedServerTime.AddSeconds(elapsed);
            TimeSpan remaining = mTargetResetTime - currentServerTime;

            if (remaining.TotalSeconds <= 0)
            {
                // 跨天了，重新初始化并刷新商店商品
                InitCountdown();
                VM?.RefreshGoodsList();
                VM?.RefreshDiscountRate();
                RefreshShopItems(VM);
                return;
            }

            mOldPanel.TimeText.text = string.Format("刷新倒计时: {0:D2}:{1:D2}:{2:D2}",
                remaining.Hours, remaining.Minutes, remaining.Seconds);
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            InitCountdown();
        }

        protected override void OnShow()
        {
            if (VM != null && VM.CurrentShopId.Value > 0 && mOldPanel != null)
            {
                RefreshShopItems(VM);
            }
            InitCountdown();
        }

        protected override void OnHide()
        {
            mCountdownInitialized = false;
            ReturnAllShopItems();
        }

        #endregion
    }
}
