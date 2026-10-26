using System.Collections.Generic;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 管理 GameLandUI/pve/MonsterHealth 下的血条模板：为精英/Boss 克隆并回收。
    /// </summary>
    public class MonsterHealthBarManager : MonoBehaviour
    {
        public static MonsterHealthBarManager Instance { get; private set; }

        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

        private RectTransform _parent;
        private GameObject _template;
        private Canvas _canvas;
        private readonly Stack<MonsterHealthBar> _pool = new Stack<MonsterHealthBar>();

        public Vector3 WorldOffset => worldOffset;

        public static void EnsureInitialized(Transform gameLandUIRoot)
        {
            if (Instance != null)
            {
                return;
            }

            Transform root = BattleArenaVisibility.FindUnderPve(gameLandUIRoot, "MonsterHealth");
            if (root == null)
            {
                return;
            }

            MonsterHealthBarManager manager = root.GetComponent<MonsterHealthBarManager>();
            if (manager == null)
            {
                manager = root.gameObject.AddComponent<MonsterHealthBarManager>();
            }

            manager.Setup();
        }

        private void Setup()
        {
            Instance = this;
            _parent = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();

            Transform templateTrans = transform.Find("Healtheffect");
            if (templateTrans == null)
            {
                LogTool.LogError("[MonsterHealthBarManager] 找不到 MonsterHealth/Healtheffect 模板");
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

        public MonsterHealthBar Acquire(MonsterCard monster)
        {
            if (_template == null)
            {
                LogTool.LogError("[MonsterHealthBarManager] 模板未初始化，无法创建血条");
                return null;
            }

            MonsterHealthBar bar;
            if (_pool.Count > 0)
            {
                bar = _pool.Pop();
            }
            else
            {
                GameObject go = Instantiate(_template, _parent);
                bar = go.GetComponent<MonsterHealthBar>();
                if (bar == null)
                {
                    bar = go.AddComponent<MonsterHealthBar>();
                }
            }

            bar.Bind(monster, _parent, _canvas, worldOffset);
            return bar;
        }

        public void Release(MonsterHealthBar bar)
        {
            if (bar == null)
            {
                return;
            }

            bar.Unbind();
            _pool.Push(bar);
        }
    }
}
