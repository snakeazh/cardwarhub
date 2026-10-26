using Cysharp.Threading.Tasks;
using FusionContinent;
using QFramework;
using UIFramework;
using UnityEngine;
using UILevel = UIFramework.UILevel;
using UIRoot = UIFramework.UIRoot;

public class PlayerEffect : MonoSingleton<PlayerEffect>
{
    const string EffectPrefabName = "dianji_01";
    const float EffectDuration = 1.2f;
    const int PoolMaxCount = 10;
    const int PoolPrewarmCount = 5;

    [SerializeField] float _effectScale = 120f;

    RectTransform _effectRoot;
    ItemGenericObjectPool _pool;
    bool _ready;
    bool _enabled = true;

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    // WebGL 不支持同步加载，改为协程异步初始化
    public System.Collections.IEnumerator InitializeAsync()
    {
        if (_ready)
        {
            yield break;
        }

        var tipsLayer = UIRoot.Instance.Config.GetLevelTransform(UILevel.Tips) as RectTransform;
        _effectRoot = CreateEffectRoot(tipsLayer);

        GameObject prefab = null;
        yield return Global.ResLoader.LoadAsync<GameObject>(EffectPrefabName, p => prefab = p);
        _pool = new ItemGenericObjectPool(EffectPrefabName, prefab, _effectRoot, PoolMaxCount);
        for (int i = 0; i < PoolPrewarmCount; i++)
        {
            _pool.PrewarmOne();
        }

        _ready = true;
    }

    static RectTransform CreateEffectRoot(RectTransform tipsLayer)
    {
        var go = new GameObject("ClickEffectRoot", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(tipsLayer, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();
        return rect;
    }

    void Update()
    {
        if (!_ready || !_enabled)
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    PlayAtScreenPoint(touch.position);
                }
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            PlayAtScreenPoint(Input.mousePosition);
        }
    }

    public void PlayAtScreenPoint(Vector2 screenPoint)
    {
        if (!_ready)
        {
            return;
        }

        var cam = GetUiCamera(_effectRoot);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_effectRoot, screenPoint, cam, out var localPoint))
        {
            return;
        }

        var fx = _pool.Get(_effectRoot);
        var fxTransform = fx.transform;
        fxTransform.localPosition = localPoint;
        fxTransform.localRotation = Quaternion.identity;
        fxTransform.localScale = Vector3.one * _effectScale;

        EnsureSortingAdder(fx);
        RestartParticles(fx);
        ReturnAfterDelay(fx).Forget();
    }

    static Camera GetUiCamera(RectTransform target)
    {
        var canvas = target.GetComponentInParent<Canvas>();
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        if (canvas.worldCamera != null)
        {
            return canvas.worldCamera;
        }

        var rootCanvas = canvas.rootCanvas;
        return rootCanvas != null ? rootCanvas.worldCamera : null;
    }

    static void EnsureSortingAdder(GameObject fx)
    {
        if (fx.GetComponent<ParticleSortingOrderAdder>() == null)
        {
            fx.AddComponent<ParticleSortingOrderAdder>();
        }
    }

    static void RestartParticles(GameObject fx)
    {
        fx.SetActive(true);
        var particleSystems = fx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            var ps = particleSystems[i];
            ps.Clear(true);
            ps.Play(true);
        }
    }

    async UniTaskVoid ReturnAfterDelay(GameObject fx)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(EffectDuration), ignoreTimeScale: true);
        if (_pool != null && fx != null)
        {
            fx.SetActive(false);
            _pool.Return(fx);
        }
    }

    protected override void OnDestroy()
    {
        _pool?.DisposeAll();
        _pool = null;
        base.OnDestroy();
    }
}
