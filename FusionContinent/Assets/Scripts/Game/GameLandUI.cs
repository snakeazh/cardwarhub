using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using DG.Tweening;
using FusionContinent.Helper;
using FusionContinent.Model;
using Helper;
using UnityEngine;
using QFramework;
using TMPro;
using UnityEngine.UI;
using ValueType = Config.ValueType;
using Game.Battle;

namespace FusionContinent
{
    public partial class GameLandUI : ViewController
    {
        public static GameLandUI Default;
        public List<CityLevelList> CityLevelList;
        private TbAttributeConfig  _tbAttributeConfig;
        private TbGameBattleConst _gameBattleConst;
        private TbCityLevelConfig _tbCityLevelConfig;
        private TbSchoolConfig _tbSchoolConfig;
        private TbCardBaseConfig _tbCardBaseConfig;
        private TbSpecialEffectsConfig _tbSpecialEffectsConfig;
        private Dictionary<int, GameObject> schoolList = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> surpriseObjs = new Dictionary<int, GameObject>();
        private Dictionary<int, Transform> schoolListpos = new Dictionary<int, Transform>();
        public GameObject passiveCardObj;

        private void Awake()
        {
            Default = this;
            
            _tbCityLevelConfig = DataConfigManager.Tables.TbCityLevelConfig;
            _tbSchoolConfig = DataConfigManager.Tables.TbSchoolConfig;
            _gameBattleConst = DataConfigManager.Tables.TbGameBattleConst;
            _tbCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _tbSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
            _tbAttributeConfig = DataConfigManager.Tables.TbAttributeConfig;

            MonsterHealthBarManager.EnsureInitialized(transform);
            MonsterIconEffectsManager.EnsureInitialized(transform);
            InitSkillCardScroll();
        }

        private void Update()
        {
            SyncSkillCardScrollCooldown();
        }

        private CityLevelList GetCityLevelCfg(int level)
        {
            var levelConfig = _tbCityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            if (levelConfig == null || level <= 0 || level > levelConfig.LevelList.Count)
            {
                return null;
            }

            return levelConfig.LevelList[level - 1];
        }

        private int GetRequiredCityExp(int level)
        {
            return GetCityLevelCfg(level)?.CityExp ?? 0;
        }

        private void SetPassiveCard()
        {
            Transform passiveCardParen = passiveCardObj.transform.parent;
            for (int i = passiveCardParen.childCount - 1; i >= 1; i--)
            {
                Destroy(passiveCardParen.GetChild(i).gameObject);
            }
            
            var team = Team.GetTeamByTeamId(Talent.GetUseTalent().TalentId);
            var passiveCardList = team.Cards.Where(t => _tbCardBaseConfig.GetOrDefault(t).Type == CardType.GeneralSkill).ToArray();
            foreach (var cardId in passiveCardList)
            {
                var playerCard = Global.GetCardById(cardId);
                if (playerCard == null) continue; 
                
                var cardCfg = _tbCardBaseConfig.GetOrDefault(cardId);
                var skillCfg = _tbSpecialEffectsConfig.GetOrDefault(cardId);
                var skillLevelCfg = skillCfg.LevelPropertyList[playerCard.Lv - 1];
                
                GameObject cardObj = Instantiate(passiveCardObj, passiveCardParen);
                cardObj.transform.localScale = Vector3.one;
                cardObj.SetActive(true);
                cardObj.transform.Find("Bg").GetComponent<Image>().sprite = Global.GeneralSkillFrameIconSpriteAtlas.GetSprite(skillLevelCfg.Quality.ToString());
                SpriteHelper.TryLoadPaintAsync(cardCfg.Icon, cardObj.transform.Find("Icon").GetComponent<Image>(), groupName: "GeneralSkill");
                
                // 监听点击事件
                cardObj.GetComponent<Button>().onClick.RemoveAllListeners();
                cardObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS,GuideType.PressArcaneShowDetails);
                    
                    PassiveCardDetail.gameObject.SetActive(true);
                    var tempX = PassiveCardDetail.anchoredPosition.x;
                    PassiveCardDetail.transform.position = new Vector3(PassiveCardDetail.transform.position.x,
                        cardObj.transform.position.y, PassiveCardDetail.transform.position.z);
                    PassiveCardDetail.anchoredPosition = new Vector2(35,
                        PassiveCardDetail.anchoredPosition.y - 100f); 
                    
                    var quitBtn = PassiveCardDetail.Find("Quit").GetComponent<Button>();
                    quitBtn.onClick.RemoveAllListeners();
                    quitBtn.onClick.AddListener(() =>
                    {
                        PassiveCardDetail.gameObject.SetActive(false);
                    });

                    // 设置信息
                    var cardInfo = PassiveCardDetail.Find("CardInfo/Canvas");
                    cardInfo.Find("night").localScale = Vector3.zero;
                    cardInfo.Find("cooling").localScale = Vector3.zero;
                    cardInfo.Find("CardName").GetComponent<TMP_Text>().text = $"{cardCfg.Name}";
                    SpriteHelper.TryLoadPaintAsync(cardCfg.Icon, cardInfo.Find("Painting").GetComponent<Image>(), groupName: "GeneralSkill");
                    SpriteHelper.TryLoadPaintAsync(cardCfg.Icon, cardInfo.Find("PaintingBg").GetComponent<Image>(), groupName: "GeneralSkill");
                    cardInfo.Find("Frame").GetComponent<Image>().sprite = Global.CarFrameAtlas.GetSprite($"{skillLevelCfg.Quality}Card");
                    cardInfo.Find("Level/LevelText").GetComponent<Text>().text = $"Lv {playerCard.Lv}";
                    PassiveCardDetail.Find("CardName").GetComponent<TMP_Text>().text = cardCfg.Name;
                    PassiveCardDetail.Find("Desc/DescText").GetComponent<TMP_Text>().text = cardCfg.Desc;
                    SpriteHelper.TryLoadTextureAsync($"{skillLevelCfg.Quality}ArcaneCardFrame", PassiveCardDetail.Find("Bg").GetComponent<Image>());
                });
            }
        }

        void Start()
        {
            Invoke(nameof(RetryInitSkillCardScroll), 0.5f);
            SetPassiveCard();
            MainCitySkill.onClick.AddListener(() =>
            {
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                GameLand.Default.wall.ApplySkillEffects(Global.CitySkill);
                MainCitySkill.gameObject.SetActive(false);
            });

            Global.GameDifficultyLevel.RegisterWithInitValue(difficulty =>
            {
                CityLevelList = _tbCityLevelConfig.GetOrDefault(difficulty).LevelList;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            Global.CityLevel.RegisterWithInitValue(level =>
            {
                var cfg = CityLevelList[Mathf.Max(level, 1) - 1];
                Global.RotaryTableOpenMaxNum.Value = cfg.RotaryTableOpenNum;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.Date.RegisterWithInitValue(date =>
            {
                if (date > 1 && GameLand.Default != null)
                {
                    GameLand.Default.wall.EveryDayHpRestoration();
                }
                Global.EveryDayDamageUp += Global.GemEveryDayDamageUp;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.BuildUnLock.RegisterWithInitValue(value =>
            {
                // 遍历 buildingLock 中的元素，按照顺序隐藏
                for (int i = 0; i < GamePlayManager.Default.buildingLock.Count; i++)
                {
                    if (i < value)
                    {
                        // 显示元素
                        if (GamePlayManager.Default.buildingLock[i] != null)
                        {
                            GamePlayManager.Default.buildingLock[i].parent.GetComponent<BoxCollider2D>().enabled = true;
                            GamePlayManager.Default.buildingLock[i].gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        // 隐藏元素
                        if (GamePlayManager.Default.buildingLock[i] != null)
                        {
                            GamePlayManager.Default.buildingLock[i].parent.GetComponent<BoxCollider2D>().enabled =
                                false;
                            GamePlayManager.Default.buildingLock[i].gameObject.SetActive(true);
                        }
                    }
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            Global.SpeedMultiplier.RegisterWithInitValue(speed => { DOTween.timeScale = speed; })
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            // 重新判断场上存活人数
            Global.CityPopulation.RegisterWithInitValue(cityPopulation =>
            {
                if (!GamePlayManager.Default.IsPaused() && cityPopulation <= 0)
                {
                    GamePlayManager.Default.GameFailure(); // 战斗失败
                    return;
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            SetCitySkillCardScroll();
        }

        /// <summary>
        /// 使用 DOTween 沿贝塞尔曲线移动（自动控制点 + 回调）
        /// </summary>
        public Camera uiTargetCamera; // ⬅ 目标UI所在Canvas所使用的摄像机（你需要赋值）

        public Camera worldCamera; // ⬅ 飞行动画使用的主摄像机（通常是 Camera.main）

        public void DelaySetScaleZeroWhenReady(int schoolId)
        {
            StartCoroutine(WaitForSchoolAndSetScale(schoolId));
        }

        private IEnumerator WaitForSchoolAndSetScale(int schoolId)
        {
            GameObject target = null;

            // 等待 schoolList 中目标 UI 存在并激活
            while (!schoolList.TryGetValue(schoolId, out target) || target == null || !target.activeInHierarchy)
            {
                yield return null; // 每帧检查一次
            }

            // 等待 UI 内部元素加载完毕（可以再多检查一帧）
            yield return null;

            // 安全访问并执行判断逻辑
            var countText = target.transform.Find("CardCount").Find("CardCountText").GetComponent<Text>();
            if (int.TryParse(countText.text, out int cardCount) && cardCount == 1)
            {
                target.transform.localScale = Vector3.zero;
            }
        }
/**
        public void MoveAlongBezier(Transform obj, Vector3 startA, int schoolId, float duration,
            Action onComplete = null)
        {
            DelaySetScaleZeroWhenReady(schoolId);

            DOTween.To(v => { }, 0, 0, 0.3f).onComplete = () =>
            {
                Vector3 start = GameLandUI.Default.GetComponent<RectTransform>().InverseTransformPoint(startA);
                // ① 获取目标 UI 世界坐标
                Vector3 uiWorldPos = GameLandUI.Default.GetComponent<RectTransform>().InverseTransformPoint(schoolListpos[schoolId].position);
                
                // uiWorldPos
                // start.z = uiWorldPos.z;
                Vector3 end = uiWorldPos;
                // UIUtils.ConvertToLocal(star)
                // ④ 控制点
                Vector3 control = (start + end) / 2f + Vector3.up * 2f;

                float t = 0f;
                DOTween.To(() => t, x =>
                    {
                        t = x;
                        obj.localPosition = GetQuadraticBezierPoint(t, start, control, end);
                    }, 1f, duration/1.2f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        obj.localPosition = end;
                        onComplete?.Invoke();

                        int haveCount = SchoolManager.Instance.GetSchool(schoolId).HaveCardCount;
                        if (haveCount <= 1) 
                        {
                            schoolList[schoolId].transform.localScale = Vector3.one;
                        }

                        bool hasExactMatch = _tbSchoolConfig
                            .GetOrDefault(schoolId)
                            .CardCountList
                            .Any(t => t.CardCount == haveCount);
                        if (hasExactMatch)
                        {
                            var BondEffectGet03pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("BondEffectGet03");
                            GameObject BondEffectGet03 = BondEffectGet03pool.Get();
                            if (BondEffectGet03 == null) return;
                            BondEffectGet03.transform.position = end;
                            BondEffectGet03.transform.localScale = Vector3.one * 0.4f;
                            DOTween.To(v => { }, 0, 0, 2f).onComplete = () =>
                            {
                                BondEffectGet03pool.Return(BondEffectGet03);
                            };
                        }
                        else
                        {
                            var BondEffectGet02pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("BondEffectGet02");
                            GameObject BondEffectGet02 = BondEffectGet02pool.Get();
                            if (BondEffectGet02 == null) return;
                            BondEffectGet02.transform.position = end;
                            BondEffectGet02.transform.localScale = Vector3.one * 0.4f;
                            DOTween.To(v => { }, 0, 0, 2f).onComplete = () =>
                            {
                                BondEffectGet02pool.Return(BondEffectGet02);
                            };
                        }
                    });
            };
        }
*/

        /// <summary>
        /// 计算二阶贝塞尔曲线点
        /// </summary>
        private Vector3 GetQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }
        
        // 惊喜卡
        public void AddSurpriseCardView(int surpriseId, int deBuffId)
        {
            var surpriseCfg = DataConfigManager.Tables.TbBattleSurpriseConfig.GetOrDefault(surpriseId);
            if (surpriseCfg == null) return;
            
            var scrollCont = SurpriseCards.Find("Viewport/Content");
            if (!surpriseObjs.TryGetValue(surpriseId, out var cardObj))
            {
                cardObj = Instantiate(scrollCont.Find("Card").gameObject, scrollCont);
                surpriseObjs.Add(surpriseId, cardObj);
                
                
            }

            cardObj.SetActive(true);
            cardObj.transform.Find("Icon").GetComponent<Image>().sprite = Global.SurpriseChooseCardIconSpriteAtlas.GetSpriteExt(surpriseCfg.Icon);
            
            // 点击事件
            var chooseBtn = cardObj.GetComponent<Button>();
            chooseBtn.onClick.RemoveAllListeners();
            chooseBtn.onClick.AddListener(() =>
            {
                var quitBtn = SurpriseDetail.Find("Quit").GetComponent<Button>();
                quitBtn.onClick.RemoveAllListeners();
                quitBtn.onClick.AddListener(() =>
                {
                    SurpriseDetail.gameObject.SetActive(false);
                });
             
                LogTool.Log($">>>>>>>>>>>> AddSurpriseCardView: {surpriseId}");
                SurpriseDetail.gameObject.SetActive(true);
                SurpriseDetail.GetComponent<SurpriseCardSlot>().SetData(surpriseId, deBuffId, null, null, 1);
            });
        }

        /*
        // 流派
        public void AddSchoolView(int schoolId)
        {
            var school = SchoolManager.Instance.GetSchool(schoolId);
            var schoolCfg = school._schoolCfg;
            if (schoolCfg.ShowJudgment == 0) return;
            
            var schoolSlotList = Schools.transform.Find("Viewport/Content"); 
            var schoolCardCountCfg = school.CurrentCardCountCfg;
            var childPos = schoolSlotList.GetChild(0);
            // LogTool.Log(">>>>> 刷新流派显示 ("+school.SchoolId+")AddSchoolView - 是否解锁：" + school.CheckUnlock());
            if (!schoolList.TryGetValue(schoolId, out var cardSlot))
            {
                cardSlot = Instantiate(childPos.gameObject, schoolSlotList.transform);
                schoolList.Add(schoolId, cardSlot);
                childPos.SetSiblingIndex(0);
                cardSlot.transform.localPosition = new Vector3(childPos.localPosition.x,-1*schoolList.Count * (childPos.GetComponent<RectTransform>().rect.height-44.87f),0);

            }

            
            // 加载并实例化卡牌对象
            // cardSlot.transform.position = transform.position;
            cardSlot.SetActive(true);
            cardSlot.LocalScale(Vector3.one);
            cardSlot.transform.Find("CardCount").transform.Find("CardCountText").GetComponent<Text>().text =
                $"{school.HaveCardCount}";
            cardSlot.transform.Find("Icon").GetComponent<Image>().sprite =
                Global.SchoolIconAtlas.GetSprite(schoolCardCountCfg.Icon);

            cardSlot.GetComponent<SorterItemData>().level = school.HaveCardCount;
            
            int haveCount = school.HaveCardCount;
            var matchingProperty = _tbSchoolConfig
                .GetOrDefault(schoolId)
                .CardCountList
                .FirstOrDefault(t => t.CardCount == haveCount); // 使用FirstOrDefault直接获取匹配项

            if (matchingProperty != null) // 如果找到了匹配项
            {
                LogTool.Log($"找到匹配项: CardCount = {matchingProperty.CardCount}, 其他属性...");
                DOTween.To(v => { }, 0, 0, 0.7f).onComplete = () => { CardDoScale(cardSlot.transform); };
                DOTween.To(v => { }, 0, 0, 1.9f).onComplete = () =>
                {
                    cardSlot.transform.Find("Iconbg").GetComponent<Image>().sprite =
                        Global.SchoolIconAtlas.GetSprite(matchingProperty.Quality.ToString()); // 使用匹配项的属性
                    // 如果需要索引位置
                    switch (matchingProperty.Quality)
                    {
                        case QualityType.Legend:
                        {
                            var BondEffectGetColor01pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("BondEffectGetColor01");
                            GameObject BondEffectGetColor01 = BondEffectGetColor01pool.Get();
                            if (BondEffectGetColor01 != null)
                            {
                                BondEffectGetColor01.transform.SetParent(cardSlot.transform);
                                BondEffectGetColor01.transform.localScale = Vector3.one*100;
                                BondEffectGetColor01.transform.position = cardSlot.transform.position;
                                DOTween.To(v => { }, 0, 0, 2f).onComplete = () =>
                                {
                                    BondEffectGetColor01pool.Return(BondEffectGetColor01);
                                };
                            }
                            var BondEffectGetColor03pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("BondEffectGetColor03");
                            GameObject BondEffectGetColor03 = BondEffectGetColor03pool.Get();
                            if (BondEffectGetColor03 != null)
                            {
                                BondEffectGetColor03.transform.SetParent(cardSlot.transform);
                                BondEffectGetColor03.transform.localScale = Vector3.one*100;
                                BondEffectGetColor03.transform.position = cardSlot.transform.position;
                            }
                            var BondEffectGetColor02pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("BondEffectGetColor02");
                            GameObject BondEffectGetColor02 = BondEffectGetColor02pool.Get();
                            if (BondEffectGetColor02 != null)
                            {
                                BondEffectGetColor02.transform.localScale = Vector3.one*0.5f;
                                BondEffectGetColor02.transform.position = cardSlot.transform.position;
                                DOTween.To(v => { }, 0, 0, 2f).onComplete = () =>
                                {
                                    BondEffectGetColor02pool.Return(BondEffectGetColor02);
                                };
                            }
                            break;
                        }
                        case QualityType.Epic:
                        {
                            // cardSlot.transform.Find("Icon").GetComponent<Image>().sprite =
                            //     Global.SchoolIconAtlas.GetSprite(schoolCardCountCfg.Icon);
                            var BondEffectGetGolden01pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("BondEffectGetGolden01");
                            GameObject BondEffectGetGolden01 = BondEffectGetGolden01pool.Get();
                            if (BondEffectGetGolden01 != null)
                            {
                                BondEffectGetGolden01.transform.SetParent(cardSlot.transform);
                                BondEffectGetGolden01.transform.localScale = Vector3.one*100;
                                BondEffectGetGolden01.transform.position = cardSlot.transform.position;
                                DOTween.To(v => { }, 0, 0, 2f).onComplete = () =>
                                {
                                    BondEffectGetGolden01pool.Return(BondEffectGetGolden01);
                                };
                            }
                            var BondEffectGetGolden02pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("BondEffectGetGolden02");
                            GameObject BondEffectGetGolden02 = BondEffectGetGolden02pool.Get();
                            if (BondEffectGetGolden02 != null)
                            {
                                BondEffectGetGolden02.transform.localScale = Vector3.one*0.5f;
                                BondEffectGetGolden02.transform.position = cardSlot.transform.position;
                                DOTween.To(v => { }, 0, 0, 2f).onComplete = () =>
                                {
                                    BondEffectGetGolden02pool.Return(BondEffectGetGolden02);
                                };
                            }
                            break;
                        }
                        default:
                        {
                            // cardSlot.transform.Find("Icon").GetComponent<Image>().sprite =
                            //     Global.SchoolIconAtlas.GetSprite(schoolCardCountCfg.Icon);
                            var BondEffectGetSilvery01pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("BondEffectGetSilvery01");
                            GameObject BondEffectGetSilvery01 = BondEffectGetSilvery01pool.Get();
                            if (BondEffectGetSilvery01 != null)
                            {
                                BondEffectGetSilvery01.transform.SetParent(cardSlot.transform);
                                BondEffectGetSilvery01.transform.localScale = Vector3.one*100;
                                BondEffectGetSilvery01.transform.position = cardSlot.transform.position;
                                DOTween.To(v => { }, 0, 0, 2f).onComplete = () =>
                                {
                                    BondEffectGetSilvery01pool.Return(BondEffectGetSilvery01);
                                };
                            }
                            break;
                        }
                    }
                    int index = _tbSchoolConfig.GetOrDefault(schoolId).CardCountList.IndexOf(matchingProperty);
                    if (matchingProperty.Quality != QualityType.Legend)
                    {
                        var match = _gameBattleConst.SchoolQualityTypeColors
                            .FirstOrDefault(t => t.Type == matchingProperty.Quality);

                        if (match != null)
                        {
                            float3 schoolcolor = match.Color;
                            var icon = cardSlot.transform.Find("Icon")?.GetComponent<Image>();
                            if (icon != null)
                            {
                                icon.color = new Color32((byte)schoolcolor.X, (byte)schoolcolor.Y, (byte)schoolcolor.Z, 255);
                            }
                        }
                    }
                    

                };
            }
            else if (haveCount < _tbSchoolConfig.GetOrDefault(schoolId).CardCountList[0].CardCount)
            {
                // cardSlot.transform.Find("Icon").GetComponent<Image>().sprite =
                //     Global.SchoolIconAtlas.GetSprite(schoolCardCountCfg.Icon);
                cardSlot.transform.Find("Iconbg").GetComponent<Image>().sprite =
                    Global.SchoolIconAtlas.GetSprite("Default"); // 使用匹配项的属性
                cardSlot.transform.Find("Icon").GetComponent<Image>().color = new Color32(255, 255, 255, 102);
            }
            else
            {
            }

            // if (haveCount == 1)
            // {
            //     cardSlot.transform.localScale =Vector3.zero;
            // }

            // 监听点击事件
            cardSlot.GetComponent<Button>().onClick.AddListener(() =>
            {
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                
                SchoolTipDetail.gameObject.SetActive(true);
                cardSlot.transform.Find("Option").gameObject.SetActive(true);
                SchoolTipDetail.transform.position = new Vector3(SchoolTipDetail.transform.position.x,
                    cardSlot.transform.position.y - 3.5f, SchoolTipDetail.transform.position.z);

                var cardDataTip = SchoolTipDetail.Find("Canvas");
                // 点击背景关闭
                cardDataTip.transform.Find("Quit").GetComponent<Button>().onClick.AddListener(() =>
                {
                    SchoolTipDetail.gameObject.SetActive(false);
                    cardSlot.transform.Find("Option").gameObject.SetActive(false);
                });
                
                cardDataTip.Find("NameText").GetComponent<TMP_Text>().text = schoolCfg.Name;
                cardDataTip.Find("LevelText").GetComponent<TMP_Text>().text =
                    $"<color=#CA8C00>{school.HaveCardCount}</color>/{schoolCfg.TotalCardCount}";
                cardDataTip.Find("DescText").GetComponent<TMP_Text>().text = schoolCfg.Desc;

                // 属性
                var cardCountBox = cardDataTip.Find("CardCountBox/Viewport/Content");
                var cardDetail = cardCountBox.Find("CardCountDetails").gameObject;
                for (int i = 0; i < cardCountBox.transform.childCount; i++)
                {
                    if (i == 0)
                    {
                        cardCountBox.transform.GetChild(i).gameObject.SetActive(false);
                        continue;
                    }

                    Destroy(cardCountBox.transform.GetChild(i).gameObject);
                }

                //临时先这样写 这样不用动到之前之前的逻辑
                int index = 1;
                //这是所有的流派卡牌
                foreach ( var schoolCard in SchoolManager.Instance.GetSchoolCardAll())
                {
                    var schoolCardCfg = SchoolManager.Instance.SchoolCardConfig.GetOrDefault(schoolCard.Key);
                    // 流派卡
                    var _schoolCard = SchoolManager.Instance.GetSchoolCard(schoolCard.Key);
                    var categorySkill = schoolCardCfg.Category == SchoolCategoryType.Skill;// 技能类别
                    LogTool.Log(_schoolCard.CardId);
                    LogTool.Log(_schoolCard.CardStar);
                    // 描述
                    var starCfg = schoolCardCfg.StarList[_schoolCard.CardStar-1 >= 0 ? _schoolCard.CardStar-1 : 0];
                    var desc = starCfg.Desc2;
                    for (int i = 1; i <= starCfg.Attributes.Count; i++)
                    {
                        var attr = starCfg.Attributes[i - 1];
                        var val = attr.ValueType == ValueType.Percentage ? attr.Value * 100 : attr.Value;
                        if (attr.Type is AttributeType.SoldierCriticalRate or AttributeType.SoldierCriticalDamage) {
                            val = attr.Value * 100;
                        }
                        desc = desc.Replace("{Attribute" + i + "}", $"{val}");
                    }

                    if (starCfg.Param1 > 0) desc = desc.Replace("{Param1}", $"{starCfg.Param1}");
                    if (starCfg.Param2 > 0) desc = desc.Replace("{Param2}", $"{starCfg.Param2}");
                    
                    
                    GameObject cardObj = Instantiate(cardDetail, cardCountBox, true);
                    cardObj.transform.position = transform.position;
                    cardObj.LocalScale(Vector3.one);
                    cardObj.SetActive(true);
                    
                    var cEnabled = true;
                    var matchColor = _gameBattleConst.SchoolCardLockedDescColor.First().Color;
                    var matchColorAlpha = _gameBattleConst.SchoolCardLockedDescTransparency;
                    Color textColor = new Color(matchColor.X/255f, matchColor.Y/255f, matchColor.Z/255f, 1);
                    cardObj.GetComponent<Image>().enabled = cEnabled;
                    cardObj.transform.Find("CardCountText/CardCountText").GetComponent<TMP_Text>().text = $"({index})";
                    cardObj.transform.Find("CardCountText/CardCountText").GetComponent<TMP_Text>().color = textColor;
                    cardObj.transform.Find("Property/DescText").GetComponent<LocalizationaAutoHeightText>().SetData(LocalizationModule.UITexts,desc);
                    cardObj.transform.Find("Property/DescText").GetComponent<TMP_Text>().color = textColor;
                    LogTool.Log(cardObj.transform.Find("CardCountText/CardCountText").GetComponent<TMP_Text>().color + " " + textColor);
                    UIHelper.SafeRefreshLayout(cardObj.GetComponent<RectTransform>());
                    UIHelper.SafeRefreshLayout(cardObj.transform.Find("Property").GetComponent<RectTransform>());
                    UIHelper.SafeRefreshLayout(cardObj.transform.Find("Property/DescText").GetComponent<RectTransform>());

                    ++index;
                }
                
                /**
                foreach (var cfg in schoolCfg.CardCountList)
                {
                    GameObject cardObj = Instantiate(cardDetail, cardCountBox, true);
                    cardObj.transform.position = transform.position;
                    cardObj.LocalScale(Vector3.one);
                    cardObj.SetActive(true);

                    var cEnabled = false;
                    var matchColor = _gameBattleConst.SchoolCardLockedDescColor.First().Color;
                    var matchColorAlpha = _gameBattleConst.SchoolCardLockedDescTransparency;
                    Color textColor = new Color(matchColor.X/255f, matchColor.Y/255f, matchColor.Z/255f, matchColorAlpha/255f);
                    if (school.GetCurrentCardCount() >= cfg.CardCount)
                    {
                        cEnabled = true;
                        textColor = new Color(matchColor.X/255f, matchColor.Y/255f, matchColor.Z/255f, 1);
                    }
                    cardObj.GetComponent<Image>().enabled = cEnabled;
                    cardObj.transform.Find("CardCountText/CardCountText").GetComponent<TMP_Text>().text = $"({cfg.CardCount})";
                    cardObj.transform.Find("CardCountText/CardCountText").GetComponent<TMP_Text>().color = textColor;
                    cardObj.transform.Find("Property/DescText").GetComponent<LocalizationaAutoHeightText>().SetData(LocalizationModule.UITexts,cfg.Desc);
                    cardObj.transform.Find("Property/DescText").GetComponent<TMP_Text>().color = textColor;
                    LogTool.Log(cardObj.transform.Find("CardCountText/CardCountText").GetComponent<TMP_Text>().color + " " + textColor);
                    UIHelper.SafeRefreshLayout(cardObj.GetComponent<RectTransform>());
                    UIHelper.SafeRefreshLayout(cardObj.transform.Find("Property").GetComponent<RectTransform>());
                    UIHelper.SafeRefreshLayout(cardObj.transform.Find("Property/DescText").GetComponent<RectTransform>());
                }
                #1#
            });

            // 排序动画
            // UIContentSorter.SortWithTween(schoolSlotList.GetComponent<RectTransform>(), 
            //     x => x.GetComponent<SorterItemData>().level, 0.5f, false);
            
            DOTween.To(v => { }, 0, 0, 0.7f).onComplete = () => {                     
                GamePlayManager.Default.TriggerGuide(GuideType.PressSchoolShowDetails);
            };
            // Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS,GuideType.PressSchoolShowDetails);
            // 强制更新布局
            
            //UIHelper.SafeRefreshLayout(schoolSlotList.GetComponent<RectTransform>());
            // LayoutRebuilder.ForceRebuildLayoutImmediate(schoolSlotList.GetComponent<RectTransform>());
            // UIHelper.SafeRefreshLayout(schoolSlotList.GetComponent<RectTransform>());
            schoolListpos[schoolId] = cardSlot.transform; // 添加这一行
            // LogTool.LogError(schoolListpos[schoolId]);
        }
        */

        private void CardDoScale(Transform cardSlot)
        {
            if (cardSlot == null) return;

            // 确保初始缩放和位置
            cardSlot.localScale = Vector3.one;
            Vector3 originalPos = cardSlot.localPosition;

            // 缩放到 0.3 并添加左右抖动
            Sequence seq = DOTween.Sequence();

            // 同时播放缩放和位置抖动
            seq.Append(cardSlot.DOScale(0.3f, 0.6f).SetEase(Ease.InQuad));
            seq.Join(cardSlot.DOShakePosition(0.6f, new Vector3(10f, 0f, 0f), 20, 90, false, false)
                .SetRelative(true));

            // 缩放回弹
            seq.Append(cardSlot.DOScale(Vector3.one * 1.2f, 0.5f).SetEase(Ease.OutBack));

            // 恢复正常大小
            seq.Append(cardSlot.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack));
        }


        private void OnDestroy()
        {
            UnbindBuyButtons();
            DestroySkillCardScroll();
            Default = null;
        }

        private float getRevisePosition(int schoolId)
        {
            // var slotIndex = schoolList.IndexOf(schoolId);
            //
            var revisePosition = 0f;
            // if (slotIndex == 0)
            // {
            //     revisePosition = 1.5f;
            // }
            // if (slotIndex == schoolList.Count - 1)
            // {
            //     revisePosition = -1.5f;
            // }
            //
            return revisePosition;
        }

        /**
         * 设置主城技能列表
         **/
        private Dictionary<int, Transform> _citySkills = new Dictionary<int, Transform>();
        public void SetCitySkillCardScroll()
        {
            foreach (var skill in Global.CitySkills.Values)
            {
                var cardCfg = _tbCardBaseConfig.GetOrDefault(skill.Id);
                if (cardCfg == null) continue;

                if (!_citySkills.TryGetValue(skill.Id, out Transform cardTran))
                {
                    cardTran = Instantiate(CitySkillCardScroll.content.GetChild(0), CitySkillCardScroll.content);
                    _citySkills.Add(skill.Id, cardTran);
                    
                    // 初始化cdTime
                    SetCardCooling(cardTran, skill, true);
                    
                    // 点击事件
                    var cardBtn = cardTran.GetComponent<Button>();
                    cardBtn.onClick.RemoveAllListeners();
                    cardBtn.onClick.AddListener(() =>
                    {
                        if (skill.IsCool || skill.InCast) return;
                        SetCardCooling(cardTran, skill);// 重置cd
                        PlayEffect(cardTran, "TalentEffectLearnable02", 160f, skill.BaseSkill.Attributes.GetFinal(AttributeType.SkillDuration));
                        skill.BaseSkill.Cast(Vector3.zero);
                        skill.InCast = true;
                    });
                }

                cardTran.gameObject.SetActive(true);
                cardTran.Find("Icon").GetComponent<Image>().sprite = Global.GeneralSkillFrameIconSpriteAtlas.GetSprite(cardCfg.Icon);;
            }
            
        }

        public void SetCardCoolingById(int skillId)
        {
            if (!_citySkills.ContainsKey(skillId)) return;

            SetCardCooling(_citySkills[skillId], Global.CitySkills[skillId]);
        }

        private void SetCardCooling(Transform cardTran, CitySkill skill, bool isInit = false)
        {
            if (skill.BaseSkill == null) return; 
            
            
            // Cd时间清除原有 tween
            if (skill.CoolTween != null && skill.CoolTween.IsActive())
            {
                skill.CoolTween.Kill();
            }

            // 获取技能cd时间
            var cdTime = isInit ? 0.1f : skill.BaseSkill.Attributes.GetFinal(AttributeType.SkillCdTime) + 0.85f;
            if (!skill.IsCool)
            {
                skill.LastTime0 = cdTime;
            }
            
            // 创建剩余时间变量（可随时修改）
            skill.RemainTime = cdTime;
            
            // 开始冷却动画
            skill.CoolTween = DOTween.To(
                () => skill.RemainTime,
                v =>
                {
                    skill.RemainTime = v;
                    skill.IsCool = v > 0;
                    skill.LastTime = v;

                    // 冷却UI显示更新
                    UpdateCardCooldownUI(cardTran, v, skill.LastTime0);
                },
                0, // 目标值是 0（剩余时间减到 0）
                cdTime // 总时长
            ).OnComplete(() =>
            {
                // 冷却完毕，强制刷新状态
                skill.IsCool = false;
                skill.InCast = false;
                skill.LastTime = 0;
                
                // 冷却UI显示更新
                UpdateCardCooldownUI(cardTran, 0, 0);
                PlayEffect(cardTran, "BondEffectGetColor01", 100f, 2f);
                PlayEffect(cardTran, "BondEffectGetColor02", 0.5f, 2f);
            }).SetEase(Ease.Linear);
        }


        private void UpdateCardCooldownUI(Transform cardInfo, float remainTime, float totalTime)
        {
            float fillPercent = totalTime > 0 ? Mathf.Clamp01(remainTime / totalTime) : 0;
            cardInfo.transform.Find("Cooling").GetComponent<Image>().fillAmount = fillPercent;

            var second = Mathf.FloorToInt(remainTime);
            var ms = ((int)(remainTime * 10)) % 10;

            cardInfo.transform.Find("Time").localScale = totalTime > 0f ? Vector3.one : Vector3.zero;
            cardInfo.transform.Find("Time/Second").localScale = Vector3.one;
            cardInfo.transform.Find("Time/Millisecond").localScale = Vector3.one;
            cardInfo.transform.Find("Time/Second").GetComponent<TMP_Text>().text = second.ToString();
            cardInfo.transform.Find("Time/Millisecond").GetComponent<TMP_Text>().text = "." + ms;
        }

        private void PlayEffect(Transform cardInfo, string effectName, float localScale = 100f, float duration = 2f)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(effectName);
            GameObject effectObj = pool.Get();
            if (effectObj != null)
            {
                effectObj.transform.SetParent(cardInfo.transform);
                effectObj.transform.localScale = Vector3.one * localScale;
                effectObj.transform.position = cardInfo.transform.position;
                if (duration > 0f) DOTween.To(v => { }, 0, 0, duration).onComplete = () =>
                {
                    pool.Return(effectObj);
                };
            }
        }

    }
}