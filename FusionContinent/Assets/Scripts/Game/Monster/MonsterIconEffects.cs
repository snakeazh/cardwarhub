using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 场景 UI 状态图标：挂在 GameLandUI/MonsterIconEffects 下，每帧跟随绑定的怪物。
    /// </summary>
    public class MonsterIconEffects : MonoBehaviour
    {
        private MonsterCard _target;
        private RectTransform _rectTransform;
        private RectTransform _parentRect;
        private Canvas _canvas;
        private Vector3 _worldOffset;
        private bool _bound;

        public GameObject Root => gameObject;
        public GameObject DisarmIcon { get; private set; }
        public GameObject ExecutionIcon { get; private set; }
        public GameObject ShieldBreakIcon { get; private set; }

        private void Awake()
        {
            CacheChildren();
        }

        public void Bind(MonsterCard target, RectTransform parentRect, Canvas canvas, Vector3 worldOffset)
        {
            CacheChildren();
            _target = target;
            _parentRect = parentRect;
            _canvas = canvas;
            _worldOffset = worldOffset;
            _bound = true;
            ResetIcons();
            gameObject.SetActive(true);
            SyncPosition();
        }

        public void Unbind()
        {
            _bound = false;
            _target = null;
            ResetIcons();
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_bound)
            {
                return;
            }

            SyncPosition();
        }

        private void CacheChildren()
        {
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            if (DisarmIcon == null)
            {
                Transform disarm = transform.Find("DisarmIcon");
                DisarmIcon = disarm.gameObject;
            }

            if (ExecutionIcon == null)
            {
                Transform execution = transform.Find("ExecutionIcon");
                ExecutionIcon = execution.gameObject;
            }

            if (ShieldBreakIcon == null)
            {
                Transform shieldBreak = transform.Find("ShieldBreakIcon");
                ShieldBreakIcon = shieldBreak.gameObject;
            }
        }

        private void ResetIcons()
        {
            DisarmIcon.SetActive(false);
            ExecutionIcon.SetActive(false);
            ShieldBreakIcon.SetActive(false);
        }

        private void SyncPosition()
        {
            if (_target == null || _parentRect == null)
            {
                return;
            }

            Camera worldCam = Camera.main;
            if (worldCam == null)
            {
                return;
            }

            Vector3 worldPos = _target.transform.position + _worldOffset;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCam, worldPos);

            Camera uiCam = null;
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCam = _canvas.worldCamera;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentRect, screenPoint, uiCam, out Vector2 localPoint))
            {
                return;
            }

            _rectTransform.anchoredPosition = localPoint;
        }
    }
}
