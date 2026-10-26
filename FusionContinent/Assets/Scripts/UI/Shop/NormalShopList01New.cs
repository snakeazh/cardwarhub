using Game.Shop;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace FusionContinent
{
    /// <summary>
    /// 普通商店列表 - 新框架版本 (MVVM)
    /// 复用原 NormalShopList01 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("NormalShopList01")]
    public class NormalShopList01New : UIFramework.UIPanel<NormalShopList01ViewModel>
    {
        private const string ShopItemPoolKey = "ShopItem";
        private const string ShopItemGroup = "NormalShopList01ShopItems";
        private const string ShopTitleGroup = "NormalShopList01ShopTitles";

        /// <summary>
        /// 作为商店子界面，不参与UIManager堆栈管理
        /// </summary>
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;
        // 引用原 Panel，复用其 UI 组件
        private NormalShopList01 mOldPanel;
        
        // 当前激活的商店项
        private readonly List<GameObject> mActiveShopItems = new List<GameObject>();
        private GameObject mLastTitleObj;
        private string mLastCategory;
        private bool mPendingLayoutRefresh;

        void OnEnable()
        {
            if (mPendingLayoutRefresh)
            {
                mPendingLayoutRefresh = false;
                if (mOldPanel?.shopitemTitleparent != null)
                    StartCoroutine(DelayRefreshLayout(mOldPanel.shopitemTitleparent.GetComponent<RectTransform>()));
            }
        }

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<NormalShopList01>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[NormalShopList01New] 找不到 NormalShopList01 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            LogTool.Log($"[NormalShopList01New] 已关联 NormalShopList01，normalShop={mOldPanel.normalShop != null}, normalShopTitleList={mOldPanel.normalShopTitleList != null}, shopitemparent={mOldPanel.shopitemparent != null}");
        }

        protected override void OnBindViewModel(NormalShopList01ViewModel vm)
        {
            if (mOldPanel == null)
            {
                LogTool.LogError("[NormalShopList01New] OnBindViewModel: mOldPanel 为 null");
                return;
            }

            LogTool.Log($"[NormalShopList01New] OnBindViewModel 开始，CurrentShopId={vm.CurrentShopId.Value}");

            // 绑定属性
            BindProperties(vm);

            // 立即刷新UI（如果数据已设置）
            if (vm.CurrentShopId.Value > 0)
            {
                LogTool.Log($"[NormalShopList01New] OnBindViewModel 立即刷新UI，GoodsList count={vm.GoodsList.Value?.Count ?? 0}");
                RefreshShopItems(vm);
            }
            else
            {
                LogTool.LogWarning("[NormalShopList01New] OnBindViewModel: CurrentShopId <= 0，不刷新UI");
            }
        }

        protected override void OnUnbindViewModel(NormalShopList01ViewModel vm)
        {
            ReturnAllTitleObjects();
            ReturnAllShopItems();
        }

        #region 属性绑定

        private void BindProperties(NormalShopList01ViewModel vm)
        {
            if (mOldPanel == null) return;

            // 商品列表变化
            vm.GoodsList.OnValueChanged += (oldVal, newVal) =>
            {
                RefreshShopItems(vm);
            };

            // 本地购买计数变化
            vm.LocalBuyCounts.OnValueChanged += (oldVal, newVal) =>
            {
                RefreshAllShopItems(vm);
            };
        }

        #endregion

        #region 商店项管理

        /// <summary>
        /// 刷新商店商品列表
        /// </summary>
        public void RefreshShopItems(NormalShopList01ViewModel vm)
        {
            if (mOldPanel == null)
            {
                LogTool.LogError("[NormalShopList01New] RefreshShopItems: mOldPanel 为 null");
                return;
            }

            LogTool.Log($"[NormalShopList01New] RefreshShopItems 开始，IsTitleMode={vm.IsTitleMode.Value}，GoodsList count={vm.GoodsList.Value?.Count ?? 0}");

            // 切换商店面板（普通/标题模式）
            SwitchShopPanel(vm.IsTitleMode.Value);

            var goodsList = vm.GoodsList.Value;
            if (goodsList == null)
            {
                LogTool.LogWarning("[NormalShopList01New] RefreshShopItems: goodsList 为 null");
                ReturnAllTitleObjects();
                ReturnAllShopItems();
                return;
            }

            ReturnAllTitleObjects();
            ReturnAllShopItems();

            // 重置标题追踪
            mLastTitleObj = null;
            mLastCategory = "";

            // 创建或更新商店项
            for (int i = 0; i < goodsList.Count; i++)
            {
                var goods = goodsList[i];
                var shopItemObj = GetShopItemObject(i);
                if (shopItemObj == null)
                {
                    continue;
                }

                var shopItem = shopItemObj.GetComponent<ShopItem>();
                if (shopItem == null) continue;

                // 重置UI
                ResetShopItemUI(shopItem);

                // 更新商品数据
                UpdateShopItem(shopItem, goods, vm);

                // 更新消耗UI
                UpdateConsumeUI(shopItem, goods, vm);

                // 应用限购UI
                ApplyLimitUI(shopItem, goods, vm);

                // 处理标题模式
                if (vm.IsTitleMode.Value)
                {
                    HandleTitleShop(shopItem, goods, vm);
                }

                // 绑定点击事件
                BindShopClick(shopItem, goods, vm);
            }

            // 刷新布局
            RefreshTitleLayout();
            LogTool.Log($"[NormalShopList01New] RefreshShopItems 完成，创建了 {goodsList.Count} 个商品项");
        }

        /// <summary>
        /// 切换商店面板（普通/标题模式）
        /// </summary>
        private void SwitchShopPanel(bool isTitleMode)
        {
            if (mOldPanel == null) return;
            
            if (mOldPanel.normalShop != null)
                mOldPanel.normalShop.SetActive(!isTitleMode);
            
            if (mOldPanel.normalShopTitleList != null)
                mOldPanel.normalShopTitleList.SetActive(isTitleMode);
            
            LogTool.Log($"[NormalShopList01New] SwitchShopPanel: normalShop active={!isTitleMode}, normalShopTitleList active={isTitleMode}");
        }

        /// <summary>
        /// 刷新所有商店项（用于购买后更新）
        /// </summary>
        private void RefreshAllShopItems(NormalShopList01ViewModel vm)
        {
            foreach (var shopItemObj in mActiveShopItems)
            {
                var shopItem = shopItemObj.GetComponent<ShopItem>();
                if (shopItem == null) continue;

                int goodsId = shopItem.shopItemID;
                var goods = vm.GetGoodsConfig(goodsId);
                if (goods == null) continue;

                UpdateConsumeUI(shopItem, goods, vm);
                ApplyLimitUI(shopItem, goods, vm);
            }
        }

        /// <summary>
        /// 获取或创建商店项
        /// </summary>
        private GameObject GetShopItemObject(int index)
        {
            if (mOldPanel?.shopitemparent == null)
            {
                LogTool.LogError("[NormalShopList01New] GetShopItemObject: shopitemparent 为空");
                return null;
            }

            // 优先用 serialized 字段，类改名导致为 null 时回退到子节点模板
            var template = mOldPanel.shopitem != null
                ? mOldPanel.shopitem.gameObject
                : (mOldPanel.shopitemparent.childCount > 0 ? mOldPanel.shopitemparent.GetChild(0).gameObject : null);

            if (template == null)
            {
                LogTool.LogError("[NormalShopList01New] GetShopItemObject: 模板为空");
                return null;
            }

            var itemPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate(ShopItemPoolKey);
            if (itemPool == null)
            {
                itemPool = CommonItemPoolManager.Instance.CreateGameObjectPool(ShopItemPoolKey, template);
            }

            if (itemPool == null)
            {
                LogTool.LogError("[NormalShopList01New] GetShopItemObject: ShopItem 对象池创建失败");
                return null;
            }

            var obj = itemPool.Get(mOldPanel.shopitemparent.transform);
            if (obj == null)
            {
                return null;
            }

            PoolScope.Track(itemPool, obj, ShopItemGroup);
            mActiveShopItems.Add(obj);

            obj.SetActive(true);
            obj.transform.SetParent(mOldPanel.shopitemparent.transform, false);
            obj.transform.SetSiblingIndex(index + 1);

            // 固定 Z 坐标
            Vector3 pos = obj.transform.localPosition;
            pos.z = 30f;
            obj.transform.localPosition = pos;

            return obj;
        }

        /// <summary>
        /// 回收多余的商店项
        /// </summary>
        private void RecycleExtraShopItems(int itemCount)
        {
            if (itemCount <= 0)
            {
                ReturnAllShopItems();
            }
        }

        /// <summary>
        /// 回收所有商店项
        /// </summary>
        private void ReturnAllShopItems()
        {
            PoolScope.ReleaseGroup(ShopItemGroup);
            mActiveShopItems.Clear();
        }

        /// <summary>
        /// 回收所有标题对象
        /// </summary>
        private void ReturnAllTitleObjects()
        {
            if (mOldPanel?.shopitemTitleparent == null) return;

            PoolScope.ReleaseGroup(ShopTitleGroup);

            for (int i = 0; i < mOldPanel.shopitemTitleparent.childCount; i++)
            {
                mOldPanel.shopitemTitleparent.GetChild(i).gameObject.SetActive(false);
            }

            mLastTitleObj = null;
            mLastCategory = "";
        }

        #endregion

        #region UI 更新

        /// <summary>
        /// 重置商店项UI
        /// </summary>
        private void ResetShopItemUI(ShopItem item)
        {
            item.shopitembgmask.gameObject.SetActive(false);
            // item.shopItemBuyLImitIcon.gameObject.SetActive(false);
            item.consumenum.gameObject.SetActive(false);
            item.consumenum.color = Color.white;
            item.consumebtn.transform.Find("freetxt")?.gameObject.SetActive(false);
            item.consumebtn.transform.Find("rmbtxt")?.gameObject.SetActive(false);
        }

        /// <summary>
        /// 更新商店项数据
        /// </summary>
        private void UpdateShopItem(ShopItem item, ShopGoodsConfig goods, NormalShopList01ViewModel vm)
        {
            item.SetShopItemId(goods.Id);
            item.shopnametxt.text = goods.Name;
            item.ShopNums.text = goods.BuyItemNum.ToString();

            // 商品图标 & 数量
            item.shopicon.sprite = Global.ShopGoodsIconAtlas.GetSpriteExt(goods.Shopgoodsicon);
            item.shopnum.text = goods.BuyItemNum.ToString();
            item.shopiconbg.color = vm.GetQualityColor(goods.Quality);
        }

        /// <summary>
        /// 更新消耗UI
        /// </summary>
        private void UpdateConsumeUI(ShopItem item, ShopGoodsConfig goods, NormalShopList01ViewModel vm)
        {
            if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
            {
                var consume = goods.BuyConsumes[0];
                var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(consume.Id);

                // 设置图标
                if (itemCfg != null)
                {
                    item.consumeicon.sprite = Global.ItemIconSpriteAtlas.GetSprite(itemCfg.Icon);
                }

                long playerItemCount = Global.GetItemNumById(consume.Id);
                item.consumenum.text = consume.Num.ToString();
                item.consumenum.color = playerItemCount < consume.Num ? Color.red : Color.white;

                // 刷新布局
                LayoutRebuilder.ForceRebuildLayoutImmediate(item.consumeicon.transform.parent.GetComponent<RectTransform>());

                // 按钮是否可点
                item.shopitembg.GetComponent<Button>().interactable = true;

                // RMB 特殊处理
                if (consume.Id == 1010005)
                {
                    item.consumeicon.gameObject.SetActive(false);
                    item.consumenum.gameObject.SetActive(false);
                    var rmbTxt = item.consumebtn.transform.Find("rmbtxt");
                    if (rmbTxt != null)
                    {
                        rmbTxt.gameObject.SetActive(true);
                        rmbTxt.GetComponent<TMP_Text>().text = consume.Num + "元";
                    }
                }
                else
                {
                    item.consumeicon.gameObject.SetActive(true);
                    item.consumenum.gameObject.SetActive(true);
                    var rmbTxt = item.consumebtn.transform.Find("rmbtxt");
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

        /// <summary>
        /// 应用限购UI
        /// </summary>
        private void ApplyLimitUI(ShopItem item, ShopGoodsConfig goods, NormalShopList01ViewModel vm)
        {
            var goodsInfo = vm.GetPlayerGoods(goods.Id);
            int serverBuyNum = goodsInfo?.BuyNum ?? 0;

            // 本地会话已买次数
            int localBuyNum = 0;
            if (vm.LocalBuyCounts.Value != null && vm.LocalBuyCounts.Value.ContainsKey(goods.Id))
            {
                localBuyNum = vm.LocalBuyCounts.Value[goods.Id];
            }
            if (serverBuyNum != 0) localBuyNum = 0;

            int totalBuyNum = serverBuyNum + localBuyNum;

            bool hasLimit = goods.LimitNum != 0;
            // item.shopItemBuyLImitIcon.gameObject.SetActive(hasLimit);

            if (hasLimit)
            {
                int remain = Mathf.Max(goods.LimitNum - totalBuyNum, 0);
                // item.buylimitnum.text = $"限购{remain}/{goods.LimitNum}";
                item.shopitembg.sprite = Global.ShopItemAtlas.GetSprite($"{goods.Quality}");
                if(!Global.ShopItemAtlas.GetSprite($"{goods.Quality}"))
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
                item.shopicon.transform.localScale = Vector3.one;
            }

            // 限购已满显示遮罩
            item.shopitembgmask.gameObject.SetActive(hasLimit && totalBuyNum >= goods.LimitNum);
        }

        /// <summary>
        /// 处理带标题的商店
        /// </summary>
        private void HandleTitleShop(ShopItem item, ShopGoodsConfig goods, NormalShopList01ViewModel vm)
        {
            if (mOldPanel?.shopitemTitleparent == null) return;

            if (mLastCategory != goods.Category)
            {
                var pool = UIPanelItemPoolManager.Instance.GetGameObjectPoolOrCreate("ShopItemTitleObj");
                if (pool == null)
                {
                    LogTool.LogError("[NormalShopList01New] HandleTitleShop: ShopItemTitleObj 对象池获取失败");
                    return;
                }

                var titleObj = pool.Get(mOldPanel.shopitemTitleparent);
                if (titleObj == null) return;

                PoolScope.Track(pool, titleObj, ShopTitleGroup);

                titleObj.transform.SetParent(mOldPanel.shopitemTitleparent);
                titleObj.transform.localScale = Vector3.one;
                // 对象未激活时不能开启协程，改为下一帧在 OnEnable 中刷新
                if (gameObject.activeInHierarchy)
                    StartCoroutine(DelayRefreshLayout(titleObj.GetComponent<RectTransform>()));
                else
                    mPendingLayoutRefresh = true;

                var titleName = titleObj.transform.Find("Title/TitleName");
                if (titleName != null)
                {
                    titleName.GetComponent<TMP_Text>().text = goods.Category;
                }

                var shopItemParent = titleObj.transform.Find("ShopItemParent");
                if (shopItemParent != null)
                {
                    item.transform.SetParent(shopItemParent);
                    item.transform.localScale = Vector3.one;
                }

                mLastTitleObj = titleObj;
                mLastCategory = goods.Category;
            }
            else if (mLastTitleObj != null)
            {
                var shopItemParent = mLastTitleObj.transform.Find("ShopItemParent");
                if (shopItemParent != null)
                {
                    item.transform.SetParent(shopItemParent);
                    item.transform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// 刷新标题布局
        /// </summary>
        private void RefreshTitleLayout()
        {
            if (VM?.IsTitleMode.Value == true && mOldPanel?.shopitemTitleparent != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(mOldPanel.shopitemTitleparent.GetComponent<RectTransform>());
            }
        }

        /// <summary>
        /// 延迟刷新布局
        /// </summary>
        private IEnumerator DelayRefreshLayout(RectTransform rect)
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        #endregion

        #region 事件绑定

        /// <summary>
        /// 绑定商店项点击事件
        /// </summary>
        private void BindShopClick(ShopItem item, ShopGoodsConfig goods, NormalShopList01ViewModel vm)
        {
            var button = item.shopitembg.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // 检查是否可以购买
                if (!vm.CanBuy(goods.Id))
                {
                    FusionContinent.Toast.Show("购买条件不足");
                    return;
                }

                // 显示确认对话框
                string consumeStr = vm.GetConsumeString(goods.BuyConsumes);
                GameMainPanel.Default.ShowConfirm("购买确认", $"是否消耗{consumeStr}?", () =>
                {
                    // 执行购买
                    vm.OnBuyGoodsCommand.Execute(goods.Id);
                });
            });
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {

        }

        protected override void OnShow()
        {
            LogTool.Log($"[NormalShopList01New] OnShow 被调用，CurrentShopId={VM?.CurrentShopId.Value ?? -1}");
            
            // 刷新显示
            if (VM != null && VM.CurrentShopId.Value > 0 && mOldPanel != null)
            {
                LogTool.Log($"[NormalShopList01New] OnShow 刷新UI，GoodsList count={VM.GoodsList.Value?.Count ?? 0}");
                RefreshShopItems(VM);
            }
        }

        protected override void OnHide()
        {
            ReturnAllTitleObjects();
            ReturnAllShopItems();
        }

        #endregion
    }
}