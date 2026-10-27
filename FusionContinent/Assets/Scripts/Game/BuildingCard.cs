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
    public partial class BuildingCard : ViewController, IPointerDownHandler, IPointerUpHandler, IDragHandler
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

        private int relevanceCard;
        // private SpriteAtlas iconAtlas;
        [HideInInspector] public int bornCard; // 生产卡牌
        [HideInInspector] public float bornTime; // 生产时间
        // [HideInInspector] public BuildingCard specialCard; // 当前关联卡
        [HideInInspector] public Vector3 targetPos = new(-1, -1, -1); // 当前锁定的栏位
        [HideInInspector] public int targetIndex = -1; // 目标栏位
        [HideInInspector] public int selfIndex = -1; // 当前栏位
        [HideInInspector] public bool isDrag;
        [HideInInspector] public bool restrictLeft;
        [HideInInspector] public bool restrictRight;
        [HideInInspector] public bool restrictTop;
        [HideInInspector] public bool restrictBottom;
        private BoxCollider2D _dragBoundary;
        // private Vector3 _boundsExtents; // 存储卡牌的边界范围

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
                LogTool.LogWarning("[BuildingCard] GameLand.CardSlot 未绑定，建筑卡拖拽边界不可用");
            }
        }

        public void Init(int id,  int cardLevel)
        {
            cardId = id;
            _buildConfig = _tbBuildingBattleConfig.GetOrDefault(id);
            selfType = _buildConfig.Type;
            level.Value = cardLevel;
            ProgressBar.gameObject.SetActive(false);
            switch (selfType)
            {
                case BuildingType.Production:
                    break;
                case BuildingType.Special:
                case BuildingType.Function:
                    //SelfShadow.GetComponent<Image>().sprite = GamePlayManager.Default._bgCardIcon.GetSprite("cardorange");
                    break;
                case BuildingType.Population:
                    break;
            }
            Icon.sprite = Global.CardIconSpriteAtlas.GetSpriteExt(_tbCardBaseConfig.GetOrDefault(id).Icon);
            NameText.text = _tbCardBaseConfig.GetOrDefault(id).Name;
            selfScale = transform.localScale;
            HitBox.gameObject.SetActive(true);
            HitBox.Initialize(this);
            // _boundsExtents = HitBox.GetComponent<BoxCollider2D>().bounds.extents;
            level.RegisterWithInitValue(level =>
            {
                LevelText.text = $"lv.{level}";
                BuildingFun();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            
            SetPropertyData();
            RegisterBuildingAI();
        }

        private void BuildingFun()
        {
            relevanceCard = _buildConfig.RelevanceCard;

            if (selfType == BuildingType.Special)
            {
                foreach (var building in GamePlayManager.Default.placedCards)
                {
                    if (relevanceCard == building.cardId)
                    {
                        building.bornCard = _buildConfig.BornCard;
                    }
                }
            }
            else if(selfType == BuildingType.Production)
            {
                if (level.Value > 1)
                {
                    return;
                }
                bornCard = _buildConfig.BornCard;
            }
            else if (selfType == BuildingType.Population)
            {
                // 同时上阵英雄数固定为 1，人口建筑不再提升上限
            }
        }
        
        public void SetPropertyData(float timeValue = 0)
        {
            var buildConfig = _tbBuildingBattleConfig.GetOrDefault(cardId);
            bornTime = buildConfig.BornTime + buildConfig.BornTime *  timeValue;
        }
        
        #region AI

        private void RegisterBuildingAI()
        {
            switch (selfType)
            {
                case BuildingType.Production:
                    ProductionAI();
                    break;
                case BuildingType.Special:
                    SpecialAI();
                    break;
            }
        }

        /// <summary>
        /// 生产建筑AI
        /// </summary>
        private void ProductionAI()
        {
            ProgressBar.gameObject.SetActive(true);
            isProducing = true; // 启动生产
            productionTimer = 0f; // 重置生产计时器
            ActionKit.OnUpdate.Register(() =>
            {
                if (isProducing && !GamePlayManager.Default.IsPaused())
                {
                    // 累加生产计时
                    productionTimer += Time.deltaTime;
                    Progress.fillAmount = productionTimer / bornTime; // 更新进度条

                    // 生产完成
                    if (productionTimer >= bornTime)
                    {
                        if (Global.CityPopulation.Value < Global.MaxCityPopulation.Value)
                        {
                            GamePlayManager.Default.GetCard(bornCard, level.Value, transform.position);
                            productionTimer = 0f; // 重置计时器
                        }
                    }
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        
        /// <summary>
        /// 特殊建筑AI
        /// </summary>
        private void SpecialAI()
        {
            ProgressBar.gameObject.SetActive(false);
            
        }
        
        #endregion

        #region 卡牌拖拽

        public void OnPointerDown(PointerEventData eventData)
        {
            ShowDetails();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (UIKit.GetPanel<GameCardTips>())
            {
                UIKit.HidePanel<GameCardTips>();
            }

            transform.localScale = selfScale;
            transform.rotation = Quaternion.identity;
            SelfShadow.enabled = false;
            isDrag = false;

            CheckOverlapOnMove();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (UIKit.GetPanel<GameCardTips>())
            {
                UIKit.HidePanel<GameCardTips>();
            }

            isDrag = true;
            //放大浮动
            transform.localScale = selfScale * 1.2f;
            SelfShadow.enabled = true;
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(eventData.position);
            newPosition.z = 0;

            // 计算鼠标的偏移量
            Vector3 delta = newPosition - transform.position;

            // 设置物体的旋转角度
            float yAngle = Mathf.Clamp(delta.x * -1000f, -10f, 10f); // 根据 X 方向的移动设置 Y 轴旋转角度，限制在 -10 到 10 度之间
            float xAngle = Mathf.Clamp(delta.y * 1000f, -10f, 10f); // 根据 Y 方向的移动设置 X 轴旋转角度，限制在 -10 到 10 度之间
            transform.rotation = Quaternion.Euler(xAngle, yAngle, 0);
            // 检查新位置是否在多边形边界内
            if (_dragBoundary == null)
            {
                transform.position = newPosition;
                return;
            }

            if (_dragBoundary.OverlapPoint(newPosition))
            {
                // 如果在边界内，允许移动到新位置
                transform.position = newPosition;
            }
            else
            {
                // 如果超出边界，将位置调整到最近的边界点
                Vector2 closestPoint = _dragBoundary.ClosestPoint(newPosition);
                transform.position = closestPoint;
            }

            if (GameLand.Default.CardSlot != null)
            {
                transform.SetSiblingIndex(GameLand.Default.CardSlot.childCount - 1);
            }
        }

        private void CheckOverlapOnMove()
        {
            if (targetPos == new Vector3(-1, -1, -1)) return;
            var buildDic = GamePlayManager.Default.buildingDic;
            if (targetIndex != -1)
            {
                //如果该位置有卡牌
                if (GamePlayManager.Default.buildingDic[targetIndex] != null)
                {
                    if (GameLand.Default.CardSlot == null)
                    {
                        LogTool.LogWarning("[BuildingCard] GameLand.CardSlot 未绑定，无法交换建筑槽位");
                        return;
                    }

                    GamePlayManager.Default.buildingDic[targetIndex].transform.position =
                        GameLand.Default.CardSlot.transform.GetChild(selfIndex).position;
                    transform.position = targetPos;
                    // var card = GamePlayManager.Default.buildingDic[targetIndex];
                    // buildDic[targetIndex] = this;
                    // buildDic[selfIndex] = card;
                    (buildDic[targetIndex], buildDic[selfIndex]) = (buildDic[selfIndex], buildDic[targetIndex]);
                    (selfIndex, buildDic[selfIndex].selfIndex) = (buildDic[selfIndex].selfIndex, selfIndex);
                    // GamePlayManager.Default.buildingDic[targetIndex].selfIndex = selfIndex;
                    // selfIndex = targetIndex;
                }
                else
                {
                    transform.position = targetPos;
                    // 更新槽位状态为占用
                    GamePlayManager.Default.buildingDic[targetIndex] = this;
                    GamePlayManager.Default.buildingDic[selfIndex] = null;
                    selfIndex = targetIndex;
                }
            }
            else
            {
                
            }
        }

        //tips显示
        private void ShowDetails()
        {
            // 实现显示详情的方法逻辑
            GameCardTips tips = UIKit.OpenPanel<GameCardTips>();
            switch (selfType)
            {
                case BuildingType.Production:
                    tips.SetData(bornCard, level.Value,selfType,_tbCardBaseConfig.GetOrDefault(cardId).Name);
                    break;
                case BuildingType.Special:
                    tips.SetData(cardId, level.Value,selfType,_tbCardBaseConfig.GetOrDefault(cardId).Name);
                    break;
                case BuildingType.Function:
                    tips.SetData(cardId, level.Value,selfType,_tbCardBaseConfig.GetOrDefault(cardId).Name);
                    break;
                case BuildingType.Population:
                    tips.SetData(cardId, level.Value,selfType,_tbCardBaseConfig.GetOrDefault(cardId).Name);
                    break;
            }
        }

        #endregion

        private void HandlePause(bool pause)
        {
            // if (pause)
            // {
            //     DOTween.PauseAll();
            // }
            // else
            // {
            //     DOTween.PlayAll();
            // }
        }
        
        private void OnDestroy()
        {
            mResLoader.Recycle2Cache();
            mResLoader = null;
            DOTween.Kill(transform, true);
        }
    }
}