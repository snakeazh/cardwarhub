using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;
using DOTween = DG.Tweening.DOTween;

namespace FusionContinent
{
    public partial class CardSellGold : ViewController, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public BuildingType selfType;
        public int cardId;
        private Vector3 selfScale;
        public BindableProperty<int> level = new BindableProperty<int>();
        private int barExp;
        private bool isLocked = false; // 表示当前是否处于锁定状态
        private bool isProducing = false; // 生产开关
        private float productionTimer = 0f; // 生产计时器
        private TbCardBaseConfig _tbCardBaseConfig;
        private TbBuildingBattleConfig _tbBuildingBattleConfig;
        private ResLoader mResLoader;
        private BuildingBattleConfig _buildConfig;
        
        private BoxCollider2D _dragBoundary;
        
        private void Awake()
        {
            mResLoader = ResLoader.Allocate();
            _tbCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _tbBuildingBattleConfig = DataConfigManager.Tables.TbBuildingBattleConfig;
            if (GameLand.Default.CardSlot != null)
            {
                _dragBoundary = GameLand.Default.CardSlot.GetComponent<BoxCollider2D>();
            }
            else
            {
                LogTool.LogWarning("[CardSellGold] GameLand.CardSlot 未绑定，出售拖拽边界不可用");
            }
        }
        
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        #region 卡牌拖拽

        public void OnPointerDown(PointerEventData eventData)
        {
            LogTool.Log(">>>> OnPointerDown");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            LogTool.Log(">>>> OnPointerUp");
        }

        public void OnDrag(PointerEventData eventData)
        {
            LogTool.Log(">>>> OnDrag");
        }

       

        #endregion
    }
}