using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using Game.Guide;
using UIFramework;
using UnityEngine;
using UIManager = UIFramework.UIManager;

namespace FusionContinent.Guide
{
    public class GuideManager : MonoBehaviour
    {
        private GuidePanel2New mInWarPanel;
        private GuidePanel2New mInUIPanel;

        private GuidePanel2New mCurrentUI;
        private static GuideManager _instance;
        Dictionary<GuideType,GameObject> mGuidePanels = new Dictionary<GuideType, GameObject>();
        public static GuideManager Instance
        {
            get
            {
                return _instance;
            }
        }
        private Dictionary<int, GuideConfig> _stepById;
        private List<GuidesStepListStruct> _steps;
        private IGuideMethod _current;
        private GuideContext _ctx;

        // 目标节点未就绪时已告警过的步骤，避免每帧刷日志
        private readonly HashSet<GuideType> _waitTargetWarned = new HashSet<GuideType>();
        
        private int _index = -1;

        private int _lastSelectCardId = -1;
        private void Awake()
        {
            _instance = this;
            // DontDestroyOnLoad(gameObject);
            CollectSteps();
            
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_SELECT_CARD,OnSelectCard);
        }

        private void OnSelectCard(object obj)
        {
            _lastSelectCardId =  (int)obj;
        }

        public void SetGuideNode(GuideType guideType,GameObject target)
        {
            if (mGuidePanels.ContainsKey(guideType))
            {
                mGuidePanels[guideType] = target;
            }
            else
            {
                mGuidePanels.Add(guideType,target);
            }
        }

        public GameObject FindGuideNode(GuideType guideType)
        {
            if (mGuidePanels.ContainsKey(guideType))
            {
                return mGuidePanels[guideType];
            }

            return null;
        }

        public void SetInWarPanel(GuidePanel2New inWarPanel)
        {
            mInWarPanel = inWarPanel;
            // 场景内实例不走 UIManager，这里手动 Init 绑定 ViewModel，步骤数据在 StartCurrent 写入
            inWarPanel.Init(ViewModelData.Create(new GuidePanel2ViewModel()));
        }

        public void SetInUIPanel(GuidePanel2New inUIPanel)
        {
            mInUIPanel = inUIPanel;
        }
        void CollectSteps()
        {
            
            // 构造索引，便于通过 Id 查找
            _stepById = DataConfigManager.Tables.TbGuideConfig.DataList.ToDictionary(x => x.Id, x => x);

            // 如果有分组/阶段字段（例如 Group/StageId），可以在此过滤并排序成序列
            // _steps = all.Where(v => v.Group == _guideConfig.Group).OrderBy(v => v.Order).ToList();


        }
        private HashSet<GuideType> successSteps = new HashSet<GuideType>();
        private Queue<GuideType> stepQueue = new Queue<GuideType>();
        private GuideType currentStep = default(GuideType);
        private GuidesStepListStruct currentStepList;
        public void Trigger(GuideType guideType)
        {
            if (_stepById == null || !_stepById.TryGetValue(2, out var step))
                return;
            ///未激活，激活生成
            if (_index == -1)
            {
                // 生成本次要执行的序列：链式或顺序式
                _steps = BuildSequenceFromEntry(step);
                _index = 0;
            }
            var currentStepConfig = _steps.Find((ele) => { return ele.Type == guideType; });
            if (currentStepConfig == null)
            {
                return;   
            }
            if ( currentStepConfig.ConditionStepDone > 0)
            {
                var upStepConfig = _steps.Find((ele) => { return ele.Type == currentStepConfig.ConditionStepDone; });
                if (!successSteps.Contains(upStepConfig.Type))
                {
                    return;
                }
            }
            if (successSteps.Contains(guideType))
            {
                return;
            }
            
            if (stepQueue.Contains(guideType))
            {
                return;
            }

            // if (_current != null)
            // {
            //     LogTool.E("xxxx");
            //     return;
            // }
            stepQueue.Enqueue(guideType);
            // return;
         
            if (_current != null)
            {
                ///运行中，不允许跳过
                return;
            }
           
            if (checkBattleMain)
            {
                return;
            }
            // else if (_index+1 != id)
            // {
            //     //不是这个激活的引导id 不进行引导
            //     return;
            // }
            // else
            
            // {
            //     //不是当前id的下一步不操作
            //     if (_steps[_index].Step != id)
            //     {
            //         return;
            //     }
            // }
       
            RunStart();
        }

        /// <summary>
        /// 引导开始（内部调用）
        /// </summary>
        public void OnGuideStart(GuideType stepTypeId, GuideType guideType)
        {
            LogTool.Log($"[GuideManager] Guide Start: {guideType}, stepTypeId={stepTypeId}");
            var guideStep = _steps.Find((e) => e.Type == guideType);
            if (CheckPauseGame(guideStep))
            {
                GamePlayManager.Default?.PauseGame();
            }

     
        }


        public bool CheckPauseGame(GuidesStepListStruct  guideStep)
        {
            if (guideStep.Type == GuideType.SkillGuide || guideStep.Type == GuideType.KillTheFirstMonster|| guideStep.Type == GuideType.BreakingToughnessWordTip)
            {
                return true;
            }
            if (guideStep.Type == GuideType.FirstComboGuide)
            {
                return true;
            }
            if (guideStep.Type == GuideType.NewSoldierDebut)
            {
                return true;
            }
            if (guideStep.Type == GuideType.NewSoldierDebutSkill)
            {
                return true;
            }
            if (guideStep.Type == GuideType.CanBeUpSchool)
            {
                return true;
            }
            if (guideStep.Type == GuideType.PressSchoolShowDetails)
            {
                return true;
            }
            if (guideStep.Type == GuideType.FirstReleaseSkill)
            {
                return true;
            }
            
            if (guideStep.Type == GuideType.WeaknessWordTip)
            {
                return true;
            }
            return false;
            // if (guideStep == null) return false;
            // if (guideStep.TipType == GuideTipType.WeakGuidance || guideStep.Type == GuideType.FirstChooseCard)
            // {
            //     return false;
            // }
            //
            // return true;
        }

        /// <summary>
        /// 引导结束（内部调用）
        /// </summary>
        public void OnGuideEnd(GuideType stepTypeId, GuideType guideType)
        {
            LogTool.Log($"[GuideManager] Guide End: {guideType}, stepTypeId={stepTypeId}");
            var guideStep = _steps.Find((e) => e.Type == guideType);
            SetGuideNode(guideType, null);
            if (guideStep.Type == GuideType.FirstChooseCard||guideStep.Type == GuideType.RefreshTip)
            {
                var newPanel = UIFramework.UIManager.GetPanel<SchoolChooseCardPanelNew>();
                newPanel?.SetPause(false);
            }
            
            if (!CheckPauseGame(guideStep) || guideType == GuideType.CanBeUpgraded )
            {
                return;
            }
            GamePlayManager.Default.StartGame();

        }

        void RunStart()
        {
            if (currentStep != default(GuideType))
            {
                return;   
            }

            if (stepQueue.Count == 0)
            {
                return;
            }
            
            StartCurrent();
        }

        private HashSet<GuideType> finishGuide = new HashSet<GuideType>();
        public bool IsRunningSpe()
        {
            if (_current != null)
            {
                if (currentStepList.TipType == GuideTipType.WeakGuidance)
                {
                    return false;
                }
                return true;
            }

            return false;
        }

        private bool checkBattleMain = true;
        private BattlePanelNew battlePanelNew;
        UIFramework.UIPanel waitPanel = null;        
        private void Update()
        {
            if (checkBattleMain)
            {
                battlePanelNew = UIManager.GetPanel<BattlePanelNew>();
                if (battlePanelNew != null && battlePanelNew.AllSubPanelOpened)
                {
                    checkBattleMain = false;
                    RunStart();
                    return;
                }
                else
                {
                    return;
                }
            }
            
            if (waitPanel!= null && waitPanel.State == UIFramework.PanelState.Opening)
            {
                return;
            }
            if (waitPanel!= null && waitPanel.State != UIFramework.PanelState.Opening )
            {
                waitPanel = null;
                if (stepQueue.Count > 0)
                {
                    RunStart();
                }
                return;
            }
            
            if (_current == null)
            {
                // 上一步因目标节点未就绪被 StartCurrent 跳过时，这里每帧重试直到目标出现
                if (stepQueue.Count > 0)
                {
                    RunStart();
                }
                return;
            }
            _current.Tick();
            if (_current.IsDone())
            {
                _current.Exit();
                this.mCurrentUI?.Hide();
                _current = null;

                OnExitState();
               
                // StartCurrent();
            }
        }

        void OnExitState()
        {
            if (currentStepList != null)
            {
                OnGuideEnd(currentStep, currentStepList.Type);
            }

            // finishGuide.Add(this._steps[currentStep].Type);
            // Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_STATUS, false);
            OnStepDone(currentStepList);

        }

        public bool GuideIsSuccess(GuideType guideType)
        {
            return successSteps.Contains(guideType);
        }
        
        void OnStepDone(GuidesStepListStruct stepList)
        {
            stepQueue.Dequeue();
            successSteps.Add(currentStep);
            if (stepList.CompleteSetIdList != null && stepList.CompleteSetIdList.Length >0)
            {
                foreach (var id in stepList.CompleteSetIdList)
                {
                    successSteps.Add(id);
                }
            }
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS_DONE,currentStep);
            currentStep = default(GuideType);
            currentStepList = null;
            
            if (stepList.CompleteNextStep >0)
            {
                var nextStep = _steps.Find((ele)=>ele.Type==stepList.CompleteNextStep);
                if (nextStep!= null)
                {
                    Trigger(nextStep.Type);
                    return;
                }
            }
            else
            {
                // if (stepList.Type != GuideType.AutoBattleWordTip)
                // {
                //     bool allDone = true;
                //     foreach (var item in _steps)
                //     {
                //         if (item.Type == GuideType.AutoBattleWordTip)
                //         {
                //             continue;
                //         }
                //         if (!successSteps.Contains(item.Type))
                //         {
                //             allDone = false;
                //             break;
                //         }
                //     }
                //
                //     if (allDone)
                //     {
                //         Trigger(GuideType.AutoBattleWordTip);
                //     }
                // }
                //
            }
            
            
            if (stepList.Type != GuideType.CanBeUpSchool && stepList.Type != GuideType.FirstChooseCard)
            {
                
                waitPanel = UIFramework.UIManager.GetPanel<SchoolChooseCardPanelNew>();
                if (waitPanel != null)
                {
                    return;
                }
            }
            else
            {
                waitPanel = null;
            }
            

            if (stepQueue.Count > 0)
            {
                RunStart();
            }
          
            // stepList.
            // if (guideType == GuideType.KillTheFirstMonster)
            // {
            //     GamePlayManager.Default.TriggerGuide(GuideType.SkillGuide); // 引导技能
            // }
            // else if (guideType == GuideType.CanBeUpSchool)
            // {
            // }
        }
        
        
        private IGuideMethod CreateMethod(GuidesStepListStruct step)
        {
            // 依据配置里的展示/交互类型创建具体方法
            // 假设存在枚举 GuideMethodType
            switch (step.TipType)
            {
                // case GuideTipType.StoryImage:
                //     return new GuideDialogMethod();
                case GuideTipType.Text:
                    return new GuideDialogMethod();
                case GuideTipType.StrongGuidance:
                    return new GuideDialogClickMethod();
                case GuideTipType.StrongGuidanceNoInter:
                    return new GuideDialogClickNoInterMethod();
                case GuideTipType.StrongGuidanceLongPress:
                    return new GuideDialogLongPressMethod();
                    break;
                case GuideTipType.StrongGuidanceBoss:
                    return new GuideDialogBossClickMethod();
                    break;
                case GuideTipType.WeakGuidanceCard:
                case GuideTipType.WeakGuidanceTalk:
                    ///弱引导卡牌下面所有的
                    return new  NewGuideWeakCardMethod();
                case GuideTipType.WeakGuidance:
                    //弱引导
                    return new GuideFingerWeakMethod();
                    break;
                
                case GuideTipType.SpecifyFinger:
                    return new NewGuideSpecifyFingerMethod(_lastSelectCardId);
                // case GuideMethodType.ClickHighlight: return new GuideClickHighlightMethod();
                // case GuideMethodType.ArrowHint: return new GuideArrowHintMethod();
                default:
                    return new GuideDialogMethod(); // 兜底
            }
        }
        
        private async void StartCurrent()
        {
            try
            {
                var peekType = stepQueue.Peek();
                var step = _steps.Find(ele => ele.Type == peekType);

                // 目标节点可能因界面异步加载尚未就绪：本帧跳过，由 Update 持续重试直到目标出现，
                // 否则 GuideMethod.Enter 里 GameObject.Find 拿到 null 直接空引用
                if (!string.IsNullOrEmpty(step.GuideUI)
                    && FindGuideNode(step.Type) == null
                    && GameObject.Find(step.GuideUI) == null)
                {
                    if (_waitTargetWarned.Add(step.Type))
                    {
                        LogTool.LogWarning($"[GuideManager] 引导目标节点未找到，等待就绪后重试: {step.Type}, GuideUI: {step.GuideUI}");
                    }
                    return;
                }

                currentStep = peekType;
                if (step.Type == GuideType.CanBeUpgraded)
                {
                    var panel = UIFramework.UIManager.GetPanel<SchoolChooseCardPanelNew>();
                    if (panel != null && panel.gameObject.activeSelf)
                    {
                        return;
                    }
                }
                currentStepList = step;
                switch (step.Step)
                {
                    case (int)GuideType.SkillGuide:
                    case (int)GuideType.FirstSpinPopUp:
                        break;
                    default:
                        break;
                }

                _current = CreateMethod(step);

                if (step.SceneType == GuideSceneType.UIScene)
                {
                    if (mInUIPanel == null)
                    {
                        var vm = new GuidePanel2ViewModel();
                        vm.Step = step;
                        vm.Current = _current;
                        vm.CardId = _lastSelectCardId;
                        mInUIPanel = await UIManager.OpenPanelAsync<GuidePanel2New, GuidePanel2ViewModel>(vm, UIFramework.UILevel.Guide);
                    }

                    mCurrentUI = mInUIPanel;
                }
                else
                {
                    mCurrentUI = mInWarPanel;
                }

                if (mCurrentUI == null || mCurrentUI.VM == null)
                {
                    LogTool.LogError($"[GuideManager] 引导面板为空，无法开始步骤 {step.Type}（GuidePanel2.prefab / InGameGuidePanel 需挂 GuidePanel2New 组件）");
                    return;
                }

                // 面板跨步骤复用，每次开始前刷新步骤数据（OnOpen 与各 GuideMethod 从 VM 读取）
                mCurrentUI.VM.Step = step;
                mCurrentUI.VM.Current = _current;
                mCurrentUI.VM.CardId = _lastSelectCardId;

                OnGuideStart(currentStep, step.Type);
                _current.Init(new GuideContext()
                {
                    Panel = mCurrentUI,
                }, step);
                mCurrentUI.Open();
                _current.Enter();
                mCurrentUI.Show();
            }
            catch (Exception e)
            {
                LogTool.E(e);
            }
        }
        private void FinishGuide()
        {
            ///标记 状态完成，关闭引导UI
            // Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_STATUS, false);
            // 标记完成、持久化状态等
            // Global.Player.SetGuideStatus(_guideConfig.Id, true);
        }
        
        private List<GuidesStepListStruct> BuildSequenceFromEntry(GuideConfig entry)
        {
            // 若没有 NextId，可改为按 Group/Stage 排序：
            return entry.StepList;
        }
        
        

        private void OnDestroy()
        {
            if (_current != null)
            {
                try
                {
                    _current.Exit();
                    _current = null;
                }
                catch (Exception e)
                {
                    LogTool.E(e);
                }
           
            }
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GUIDE_SELECT_CARD,OnSelectCard);
        }
    }

    public enum EGuideMethodType
    {
        DialogClickBtn = 1,
    }
}
