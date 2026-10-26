using System.Collections.Generic;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 管理 GameLandUI/pve/MonsterIconEffects 下的状态图标模板：为怪物克隆并回收。
    /// </summary>
    public class MonsterIconEffectsManager : MonoBehaviour
    {
        public static MonsterIconEffectsManager Instance { get; private set; }

        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);

        private RectTransform _parent;
        private GameObject _template;
        private Canvas _canvas;
        private readonly Stack<MonsterIconEffects> _pool = new Stack<MonsterIconEffects>();

        public Vector3 WorldOffset => worldOffset;

        public static void EnsureInitialized(Transform gameLandUIRoot)
        {
            if (Instance != null)
            {
                return;
            }

            Transform root = BattleArenaVisibility.FindUnderPve(gameLandUIRoot, "MonsterIconEffects");
            if (root == null)
            {
                return;
            }

            MonsterIconEffectsManager manager = root.GetComponent<MonsterIconEffectsManager>();
            if (manager == null)
            {
                manager = root.gameObject.AddComponent<MonsterIconEffectsManager>();
            }

            manager.Setup();
        }

        private void Setup()
        {
            Instance = this;
            _parent = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();

            Transform templateTrans = transform.Find("IconEffects");
            if (templateTrans == null)
            {
                LogTool.LogError("[MonsterIconEffectsManager] 找不到 MonsterIconEffects/IconEffects 模板");
                return;
            }

            _template = templateTrans.gameObject;
            _template.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public MonsterIconEffects Acquire(MonsterCard monster)
        {
            if (_template == null)
            {
                LogTool.LogError("[MonsterIconEffectsManager] 模板未初始化，无法创建状态图标");
                return null;
            }

            MonsterIconEffects effects;
            if (_pool.Count > 0)
            {
                effects = _pool.Pop();
            }
            else
            {
                GameObject go = Instantiate(_template, _parent);
                effects = go.GetComponent<MonsterIconEffects>();
                if (effects == null)
                {
                    effects = go.AddComponent<MonsterIconEffects>();
                }
            }

            effects.Bind(monster, _parent, _canvas, worldOffset);
            return effects;
        }

        public void Release(MonsterIconEffects effects)
        {
            if (effects == null)
            {
                return;
            }

            effects.Unbind();
            _pool.Push(effects);
        }
    }
}
