using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 场景 UI 血条：挂在 GameLandUI/MonsterHealth 下，每帧跟随绑定的精英/Boss。
    /// </summary>
    public class MonsterHealthBar : MonoBehaviour
    {
        private MonsterCard _target;
        private RectTransform _rectTransform;
        private RectTransform _parentRect;
        private Canvas _canvas;
        private Vector3 _worldOffset;
        private bool _bound;

        public GameObject Root => gameObject;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
        }

        public void Bind(MonsterCard target, RectTransform parentRect, Canvas canvas, Vector3 worldOffset)
        {
            _target = target;
            _parentRect = parentRect;
            _canvas = canvas;
            _worldOffset = worldOffset;
            _bound = true;
            gameObject.SetActive(true);
            SyncPosition();
        }

        public void Unbind()
        {
            _bound = false;
            _target = null;
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
