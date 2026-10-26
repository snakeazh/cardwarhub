using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ParticleClipParent : MonoBehaviour
{
    [SerializeField]
    // 显示在面板上方便调试，看看是不是 Null
    private RectMask2D _mask;
    
    // 隐藏渲染器数组，没必要显示在面板
    private ParticleSystemRenderer[] _renderers;
    private static MaterialPropertyBlock _propBlock;
    private static readonly int ClipRectID = Shader.PropertyToID("_ClipRect");
    
    [SerializeField]
    // 缓存数组
    private Vector3[] _corners = new Vector3[4]; 

    // --- 修改 1: 使用 OnEnable 代替 Start ---
    void OnEnable()
    {
        Init();
    }

    void Init()
    {
        if (_mask == null) _mask = GetComponentInParent<RectMask2D>();
        if (_renderers == null || _renderers.Length == 0) _renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        // --- 修改 2: 编辑器模式下的防丢失补救 ---
        if (_mask == null || _renderers == null)
        {
            Init();
            // 如果尝试 Init 后还是空的，那就真的没办法了，返回
            if (_mask == null) return; 
        }

        // 1. 获取世界坐标 (正确)
        _mask.rectTransform.GetWorldCorners(_corners);

        // 2. 计算 Min/Max (正确)
        float xMin = _corners[0].x;
        float yMin = _corners[0].y;
        float xMax = _corners[0].x;
        float yMax = _corners[0].y;

        for (int j = 1; j < 4; j++)
        {
            if (_corners[j].x < xMin) xMin = _corners[j].x;
            if (_corners[j].y < yMin) yMin = _corners[j].y;
            
            if (_corners[j].x > xMax) xMax = _corners[j].x;
            if (_corners[j].y > yMax) yMax = _corners[j].y;
        }

        // 3. 这里的判断是为了防止还没布局完成时的 0 错误
        // 如果矩形太小(比如还没被布局撑开)，就不裁剪，避免全被切没了
        if (Mathf.Abs(xMax - xMin) < 0.1f) return;

        Vector4 clipRect = new Vector4(xMin, yMin, xMax, yMax);

        // 4. 应用
        for (int i = 0; i < _renderers.Length; i++)
        {
            var ps = _renderers[i];
            if (ps == null) continue;

            ps.GetPropertyBlock(_propBlock);
            _propBlock.SetVector(ClipRectID, clipRect);
            ps.SetPropertyBlock(_propBlock);
        }
    }

    // 提供给外部动态刷新
    public void RefreshParticles()
    {
        _renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
    }
}