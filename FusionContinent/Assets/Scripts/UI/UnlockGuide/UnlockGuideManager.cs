using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FusionContinent
{

    public class UnlockGuideMessageData
    {
        public ModuleFunctionType ModuleFunctionType;
        public int Step;
    }
    public class UnlockGuideManager : MonoBehaviour
    {
        public static UnlockGuideManager Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(gameObject);
                Instance = null;
                return;
                // return;
            }
            Instance = this;
            lastLevel = 0;
            DontDestroyOnLoad(gameObject);
            Initialized();
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_UNLOCK_GUIDE_CHANGE,OnGuideChange);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO,OnLevelChange);
        }

        private void OnDestroy()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_UNLOCK_GUIDE_CHANGE,OnGuideChange);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO,OnLevelChange);
        }

        private int lastLevel = 0;

        private void OnLevelChange(object obj)
        {
            UpdateLevelTrigger();
        }


        private void OnGuideChange(object obj)
        {
            var guideData = obj as UnlockGuideMessageData;
            if (guideData != null)
            {
                if (guideData.ModuleFunctionType != CurrentModuleFunction)
                {
                    return;
                }
                
                

                if (mapConfigs.TryGetValue(CurrentModuleFunction,out var value))
                {
                    // 面板异步打开，加载完成前 _panel 为空，步骤消息跳过
                    if (_panel == null)
                    {
                        return;
                    }
                    if (guideData.Step < _panel.CurrentStep )
                    {
                        return;
                    }
                    _panel?.ToNextStep();
                    if (guideData.Step >= value.StepList.Last().StepId)
                    {
                        UnlockGuideDone(CurrentModuleFunction);
                    }
                    else
                    {
                        _panel?.RunNextStep();
                    }
                }
                // guideData.Step
                
            }
        }
        public void ForceCompleteGuide()
        {
            if (CurrentModuleFunction == ModuleFunctionType.None)
            {
                return;
            }

            var doneType = CurrentModuleFunction;
            _panel?.ToNextStep();
            FinishGuideInternal(doneType, true);
        }

        ModuleFunctionConfig _deferredContinueCfg;

        /// <summary>
        /// 结束当前引导并落档，但不立刻接续 NextGuide / 队列（用于等业务 UI 关闭后再 Flush）。
        /// </summary>
        public void CompleteCurrentGuideDeferContinue()
        {
            if (CurrentModuleFunction == ModuleFunctionType.None)
            {
                return;
            }

            if (!mapConfigs.TryGetValue(CurrentModuleFunction, out var doneCfg))
            {
                FinishGuideInternal(CurrentModuleFunction, false);
                return;
            }

            _panel?.ToNextStep();
            FinishGuideInternal(CurrentModuleFunction, false);
            _deferredContinueCfg = doneCfg;
        }

        /// <summary>
        /// 执行 CompleteCurrentGuideDeferContinue 时挂起的接续。
        /// </summary>
        public void FlushDeferredContinue()
        {
            if (_deferredContinueCfg == null)
            {
                return;
            }

            var cfg = _deferredContinueCfg;
            _deferredContinueCfg = null;
            TryContinueAfterGuideDone(cfg);
        }

        /// <summary>
        /// 丢弃挂起的接续（例如抽卡失败时不再按成功路径拉起下一步）。
        /// 前置引导已落档，后续模块可在下次 RefreshPendingGuides / StartGuide 时入队。
        /// </summary>
        public void ClearDeferredContinue()
        {
            _deferredContinueCfg = null;
        }

        public void CloseGuide()
        {
            // 面板还在异步加载：标记打开后立即关闭，避免孤儿面板
            if (_panel == null && _panelOpening)
            {
                _closeAfterOpen = true;
                return;
            }
            UIFramework.UIManager.ClosePanel<UnlockGuidePanelNew>();
            _panel = null;
        }
        
        
        

        Dictionary<ModuleFunctionType,ModuleFunctionConfig> mapConfigs = new Dictionary<ModuleFunctionType, ModuleFunctionConfig>();
        public void Initialized()
        {
            mapConfigs.Clear();
            foreach (var item in DataConfigManager.Tables.TbModuleFunctionConfig.DataMap)
            {
                if (item.Value.TipType == ModuleTipType.NoGuide)
                {
                    continue;
                }
                mapConfigs.Add(item.Value.ModuleType, item.Value);
            }
            
            foreach (var item in mapConfigs)
            {
                bool unlock = ModuleFunction.CheckOpen(item.Value.ModuleType, false);
                if (unlock)
                {
                    Trigger(item.Value.ModuleType);
                }
            }
            SortUnlockQueue();

            lastLevel = Global.User.Level;
        }

        public void UpdateLevelTrigger()
        {
            int currentLevel = Global.User.Level;
            foreach (var item in mapConfigs)
            {
                bool unlock = ModuleFunction.CheckOpen(item.Value.ModuleType, false);
                // 本段升级区间新解锁的模块才入队
                if (unlock
                    && item.Value.UnlockUserLv > lastLevel
                    && item.Value.UnlockUserLv <= currentLevel)
                {
                    Trigger(item.Value.ModuleType);
                }
            }
            SortUnlockQueue();
            lastLevel = currentLevel;
        }

        /// <summary>
        /// 重新扫描：已解锁且未完成的强引导补入队列（避免登录时序/配置热更导致漏入队）。
        /// </summary>
        public void RefreshPendingGuides()
        {
            // 清掉本应等前置接续、却已进队的模块（如锻造等装备完成后再开）
            unlockQueue.RemoveAll(IsWaitingForPrerequisiteGuide);

            foreach (var item in mapConfigs)
            {
                if (ModuleFunction.CheckOpen(item.Value.ModuleType, false))
                {
                    Trigger(item.Value.ModuleType);
                }
            }
            SortUnlockQueue();
        }

        readonly List<ModuleFunctionType> unlockQueue = new List<ModuleFunctionType>();
        public int PendingStartStep { get; private set; } = 1;

        public void Trigger(ModuleFunctionType moduleFunctionType)
        {
            if (CheckGuideUsed(moduleFunctionType))
            {
                return;
            }

            // 若该引导由其他未完成引导的 NextGuideModule 接续，则不提前入队
            if (IsWaitingForPrerequisiteGuide(moduleFunctionType))
            {
                return;
            }

            if (IsInUnlockQueue(moduleFunctionType))
            {
                return;
            }

            unlockQueue.Add(moduleFunctionType);
            SortUnlockQueue();
        }

        /// <summary>
        /// 是否存在「未完成的前置引导」通过 NextGuideModule 指向本模块。
        /// 有则本模块等前置完成后由接续逻辑拉起，避免与前置同时入队。
        /// </summary>
        bool IsWaitingForPrerequisiteGuide(ModuleFunctionType moduleFunctionType)
        {
            foreach (var cfg in mapConfigs.Values)
            {
                if (cfg.NextGuideModule != moduleFunctionType)
                {
                    continue;
                }

                if (!CheckGuideUsed(cfg.ModuleType))
                {
                    return true;
                }
            }

            return false;
        }

        void SortUnlockQueue()
        {
            unlockQueue.Sort(CompareGuideOrder);
        }

        int CompareGuideOrder(ModuleFunctionType a, ModuleFunctionType b)
        {
            mapConfigs.TryGetValue(a, out var cfgA);
            mapConfigs.TryGetValue(b, out var cfgB);
            int priorityA = cfgA != null ? cfgA.GuidePriority : 0;
            int priorityB = cfgB != null ? cfgB.GuidePriority : 0;
            int cmp = priorityA.CompareTo(priorityB);
            if (cmp != 0)
            {
                return cmp;
            }

            int lvA = cfgA != null ? cfgA.UnlockUserLv : 0;
            int lvB = cfgB != null ? cfgB.UnlockUserLv : 0;
            cmp = lvA.CompareTo(lvB);
            if (cmp != 0)
            {
                return cmp;
            }

            int idA = cfgA != null ? cfgA.Id : 0;
            int idB = cfgB != null ? cfgB.Id : 0;
            return idA.CompareTo(idB);
        }

        public ModuleFunctionType CurrentModuleFunction = ModuleFunctionType.None;
        private UnlockGuidePanelNew _panel;
        private bool _panelOpening;
        private bool _closeAfterOpen;
        public void StartGuide()
        {
            StartGuide(1);
        }

        public void StartGuide(int startStep)
        {
            if (CurrentModuleFunction !=  ModuleFunctionType.None)
            {
                LogTool.Log("当前正在引导不能继续触发");
                return;
            }

            // 主界面弹出前再扫一遍，避免登录时等级/数据未就绪导致漏入队
            RefreshPendingGuides();

            if (unlockQueue.Count == 0)
            {
                LogTool.Log("当前没有引导队列不能继续触发");
                CurrentModuleFunction = ModuleFunctionType.None;
                PendingStartStep = 1;
                return;
            }
            
            CurrentModuleFunction = unlockQueue[0];
            unlockQueue.RemoveAt(0);
            if (CheckGuideUsed(CurrentModuleFunction))
            {
                CurrentModuleFunction = ModuleFunctionType.None;
                StartGuide(startStep);
                return;
            }

            PendingStartStep = NormalizeStartStep(CurrentModuleFunction, startStep);
        }

        /// <summary>
        /// 从指定模块与 StepId 启动引导（用于配置接续）。
        /// </summary>
        public void StartGuideFrom(ModuleFunctionType moduleFunctionType, int startStep)
        {
            if (CurrentModuleFunction != ModuleFunctionType.None)
            {
                LogTool.Log("当前正在引导不能继续触发");
                return;
            }

            if (!mapConfigs.ContainsKey(moduleFunctionType))
            {
                return;
            }

            if (CheckGuideUsed(moduleFunctionType))
            {
                return;
            }

            RemoveFromUnlockQueue(moduleFunctionType);
            CurrentModuleFunction = moduleFunctionType;
            PendingStartStep = NormalizeStartStep(moduleFunctionType, startStep);
        }

        int NormalizeStartStep(ModuleFunctionType moduleFunctionType, int startStep)
        {
            if (!mapConfigs.TryGetValue(moduleFunctionType, out var cfg) || cfg.StepList == null || cfg.StepList.Count == 0)
            {
                return 1;
            }

            if (startStep <= 0)
            {
                startStep = 1;
            }

            var matched = cfg.StepList.Find(ele => ele.StepId == startStep);
            if (matched != null)
            {
                return startStep;
            }

            return cfg.StepList[0].StepId;
        }

        /// <summary>
        /// 尝试触发指定模块的解锁引导：须尚未完成引导。
        /// </summary>
        public void TryTriggerGuide(ModuleFunctionType moduleFunctionType)
        {
            if (!mapConfigs.ContainsKey(moduleFunctionType))
            {
                return ;
            }

            if (CheckGuideUsed(moduleFunctionType))
            {
                return ;
            }

            if (CurrentModuleFunction == moduleFunctionType)
            {
                return ;
            }

            if (CurrentModuleFunction != ModuleFunctionType.None)
            {
                return ;
            }

            StartGuideFrom(moduleFunctionType, 1);
            if (CurrentModuleFunction != ModuleFunctionType.None)
            {
                ShowGuide();
            }
        }

        bool IsInUnlockQueue(ModuleFunctionType moduleFunctionType)
        {
            return unlockQueue.Contains(moduleFunctionType);
        }

        void RemoveFromUnlockQueue(ModuleFunctionType moduleFunctionType)
        {
            unlockQueue.RemoveAll(item => item == moduleFunctionType);
        }

        public static void Send(ModuleFunctionType moduleFunctionType,int step)
        {
            UnlockGuideMessageData data= new UnlockGuideMessageData();
            data.ModuleFunctionType =moduleFunctionType;
            data.Step = step;
            Global.Publisher.Publish(MessagePublisher.CHANGE_UNLOCK_GUIDE_CHANGE,data);
        }
        
        
        public static void SendNavigation(int step)
        {
            UnlockGuideMessageData data= new UnlockGuideMessageData();
            data.ModuleFunctionType =Instance.CurrentModuleFunction;
            data.Step = step;
            Global.Publisher.Publish(MessagePublisher.CHANGE_UNLOCK_GUIDE_CHANGE,data);
        }
        
        public async void ShowGuide()
        {
            if (_panelOpening)
            {
                return;
            }

            // 已存在（如被 ChangePanel 隐藏）时先关掉再重开，
            // 保证 OnOpen 重跑、按 PendingStartStep 重新开始引导（对齐原 OpenPanel 重开语义）
            if (_panel != null)
            {
                UIFramework.UIManager.ClosePanel<UnlockGuidePanelNew>();
                _panel = null;
            }

            _panelOpening = true;
            var vm = new Game.UnlockGuide.UnlockGuidePanelViewModel();
            _panel = await UIFramework.UIManager.OpenPanelAsync<UnlockGuidePanelNew, Game.UnlockGuide.UnlockGuidePanelViewModel>(
                vm, UIFramework.UILevel.Guide);
            _panelOpening = false;
            if (_closeAfterOpen)
            {
                _closeAfterOpen = false;
                CloseGuide();
            }
        }

        bool CheckGuideUsed(ModuleFunctionType  moduleFunctionType)
        {
            return PlayerModuleModel.CheckModuleStatus(this.mapConfigs[moduleFunctionType].Id);
        }

        public bool CheckUnlockGuideRunning(ModuleFunctionType  moduleFunctionType)
        {
            if (CurrentModuleFunction == moduleFunctionType)
            {
                return true;
            }

            return false;
        }
        
        public void UnlockGuideDone(ModuleFunctionType  moduleFunctionType)
        {
            FinishGuideInternal(moduleFunctionType, true);
        }

        void FinishGuideInternal(ModuleFunctionType moduleFunctionType, bool tryContinue)
        {
            if (!mapConfigs.TryGetValue(moduleFunctionType, out var doneCfg))
            {
                CurrentModuleFunction = ModuleFunctionType.None;
                PendingStartStep = 1;
                CloseGuide();
                return;
            }

            stepMap.Remove(moduleFunctionType);
            CurrentModuleFunction = ModuleFunctionType.None;
            PendingStartStep = 1;
            CloseGuide();
            PlayerModuleModel.SaveModuleStatus(doneCfg.Id, () => { });

            if (!tryContinue)
            {
                return;
            }

            TryContinueAfterGuideDone(doneCfg);
        }

        void TryContinueAfterGuideDone(ModuleFunctionConfig doneCfg)
        {
            if (doneCfg.NextGuideModule != ModuleFunctionType.None
                && mapConfigs.ContainsKey(doneCfg.NextGuideModule)
                && !CheckGuideUsed(doneCfg.NextGuideModule))
            {
                int startStep = doneCfg.NextGuideStartStep > 0 ? doneCfg.NextGuideStartStep : 1;
                StartGuideFrom(doneCfg.NextGuideModule, startStep);
                if (CurrentModuleFunction != ModuleFunctionType.None)
                {
                    ShowGuide();
                    return;
                }
            }

            if (unlockQueue.Count > 0)
            {
                StartGuide();
                if (CurrentModuleFunction != ModuleFunctionType.None)
                {
                    ShowGuide();
                }
            }
        }
        
        readonly Dictionary<ModuleFunctionType, Dictionary<int, RectTransform>> stepMap =
            new Dictionary<ModuleFunctionType, Dictionary<int, RectTransform>>();

        /// <summary>
        /// 获取步骤对应的节点
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public RectTransform GetStepTarget(int step)
        {
            return GetStepTarget(CurrentModuleFunction, step);
        }

        public Config.ModuleStepListStruct GetModuleStep(int step)
        {
            if (CurrentModuleFunction != ModuleFunctionType.None)
            {
                return mapConfigs[CurrentModuleFunction].StepList.Find(ele=>ele.StepId == step);
            }

            return null;
        }

        public RectTransform GetStepTarget(ModuleFunctionType moduleFunctionType, int step)
        {
            if (!stepMap.TryGetValue(moduleFunctionType, out var stepTargets))
            {
                return null;
            }

            stepTargets.TryGetValue(step, out var target);
            return target;
        }
        /// <summary>
        /// 设置引导对应步骤的引导节点
        /// </summary>
        /// <param name="moduleFunctionType"></param>
        /// <param name="stepId"></param>
        /// <param name="guideTarget"></param>
        public void SetUnlockTypeStep(ModuleFunctionType moduleFunctionType,int stepId,RectTransform guideTarget)
        {
            if ( guideTarget == null)
            {
                return;
            }

            if (!stepMap.TryGetValue(moduleFunctionType, out var stepTargets))
            {
                stepTargets = new Dictionary<int, RectTransform>();
                stepMap[moduleFunctionType] = stepTargets;
            }

            stepTargets[stepId] = guideTarget;

            if (CurrentModuleFunction == moduleFunctionType
                && _panel != null
                && _panel.CurrentStep == stepId)
            {
                _panel.RefreshCurrentTarget();
            }
        }
        /// <summary>
        /// 设置导航栏引导
        /// </summary>
        /// <param name="moduleFunctionType"></param>
        /// <param name="stepId"></param>
        /// <param name="navigationKey"></param>
        public void SetUnlockTypeStep(ModuleFunctionType moduleFunctionType,int stepId,GameMainButtonType navigationKey)
        {
            SetUnlockTypeStep(moduleFunctionType, stepId, GetNavigation(navigationKey));
        }
        public void SetErrorTypeStep(ModuleFunctionType moduleFunctionType,int stepId)
        {
            if (moduleFunctionType !=  CurrentModuleFunction)
            {
                return;
            }

            UnlockGuideDone(moduleFunctionType);
        }
        
        
        Dictionary<GameMainButtonType , RectTransform> navigationCache = new Dictionary<GameMainButtonType , RectTransform>();


        public void SetNavigation(GameMainButtonType key, RectTransform target)
        {
            navigationCache.TryAdd(key, target);
        }


        public RectTransform GetNavigation(GameMainButtonType key)
        {
            var panel = UIFramework.UIManager.GetPanel<GameMainPanelNew>();
            if (panel == null)
            {
                return null;
            }
            
            
            return panel.mButtonTypeMap[key].GetComponent<RectTransform>();
        }

 

        public void ClearAllSave()
        {
        }
    }
}
