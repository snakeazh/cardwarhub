using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using UnityEngine;

namespace Manager
{
    public class ConditionManager : MonoBehaviour
    {
        public static ConditionManager Instance;
        
        private readonly Dictionary<int, Dictionary<string, Action<int, object>>> _listeners = new Dictionary<int, Dictionary<string, Action<int, object>>>();
        private readonly Dictionary<(ConditionType, string), ConditionProgress> _conditionProgresses = new Dictionary<(ConditionType, string), ConditionProgress>();

        private TbConditionConfig _conditionConfig;
        
        private void Awake()
        {
            _conditionConfig = DataConfigManager.Tables.TbConditionConfig;
            
            if (Instance != null)
            {
                Destroy(Instance);
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public void Init()
        {
            _listeners.Clear();
            _conditionProgresses.Clear();
        }

        public void AddListener(int condId, string listenerName, Action<int, object> callback)
        {
            var cfg = _conditionConfig.GetOrDefault(condId);
            if (cfg == null)
            {
                throw new Exception("ConditionManager AddListener Not Configured condId[" + condId + "]");
            }
            
            var key = (cfg.ConditionType, cfg.ConditionParam);
            _conditionProgresses.TryAdd(key, new ConditionProgress(cfg.Id, cfg));
            
            if (!_listeners.ContainsKey(condId))
            {
                _listeners[condId] = new Dictionary<string, Action<int, object>>();
            }
            
            _listeners[condId][listenerName] = callback;
        }

        public void RemoveListener(int condId, string listenerName)
        {
            if (!_listeners.ContainsKey(condId) || !_listeners[condId].ContainsKey(listenerName))
            {
                return;
            }

            _listeners[condId].Remove(listenerName);
        }


        public void OnTriggerProgress(ConditionType condType, int num, string param = "", object target = null)
        {
            var key = (condType, param);
            if (!_conditionProgresses.TryGetValue(key, out var condProgress))
            {
                return;
            }

            // 添加进度
            condProgress.AddProgress(num);
            
            // 完成轮数
            var roundNum = condProgress.CompletedRound();
            if (roundNum > 0)
            {
                CompleteCondition(condProgress.Id, roundNum, target); // 完成条件调用函数
                condProgress.IsNotAchievedCall = condProgress.ConditionConfig.IsNotAchievedCall > 0;// 生效未达成回调
            }
            else if (condProgress.IsNotAchievedCall)
            {
                CompleteCondition(condProgress.Id, roundNum, target); // 未达成条件调用函数
            }
        }

        private void CompleteCondition(int condId, int completeRound = 0, object target = null)
        {
            if (!_listeners.TryGetValue(condId, out var listenerActions) || listenerActions.Count == 0)
            {
                return;
            }

            foreach (var action in listenerActions.Values)
            {
                action(completeRound, target);
            }
        }

    }
}