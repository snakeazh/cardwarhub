using System.Collections;
using UnityEngine;
using QFramework;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Sounds;
using Manager;

/// <summary>
/// 单例金币生成与动画控制器
/// 支持高并发创建、自动分帧生成、防止掉帧。
/// 使用方式：GoldCoinManager.Instance.CreateMana(数量, 位置);
/// </summary>
public class GoldCoinManager : MonoSingleton<GoldCoinManager>
{
    [Header("金币目标点（吸附的UI位置）")]
    public Transform target;

    [Header("每帧生成的金币数量（防止性能波动）")]
    public int batchPerFrame = 10;

    [Header("金币飞出角度范围 (弧度)")]
    public float randomAngle = 2f;

    [Header("金币飞出力度范围（半径）")]
    public float randomForce = 1.5f;

    [Header("金币初始缩放")]
    public float localScale = 0.3f;

    [Header("金币飞出时间")]
    public float fallDuration = 0.3f;

    [Header("金币落地等待时间")]
    public float waitDuration = 0.2f;

    [Header("金币吸附回收时间")]
    public float recycleDuration = 0.5f;

    // 对象池引用（从对象池系统中获取）
    private GenericObjectPool _pool;

    // 待生成金币总数（任务队列）
    private int _pendingCount = 0;

    // 协程是否正在执行
    private bool _isProcessing = false;

    // 最近一次金币生成的位置（支持连续调用）
    private Vector3 _lastSpawnPos;

    // 基础金币数量
    private int _goldBaseNum;

    protected void Awake()
    {
        // 初始化对象池（如果不存在则自动创建）
        _pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("GoldCoin");
        if (_pool == null)
            LogTool.LogError("[GoldCoinManager] 未找到或创建 GoldCoin 对象池！");
        
        // 获取基础金币数量
        _goldBaseNum = DataConfigManager.Tables.TbGameBattleConst.DorpGoldBaseNumber;
    }

    /// <summary>
    /// 请求生成金币（可被频繁调用，不会丢失任务）
    /// </summary>
    /// <param name="count">金币数量</param>
    /// <param name="pos">生成位置</param>
    public void CreateCoin(int count, Vector3 pos)
    {
        // 累加到任务队列
        _pendingCount += count;
        _lastSpawnPos = pos;

        // 若协程未运行，则启动创建协程
        if (!_isProcessing)
            StartCoroutine(ProcessCreateQueue());
    }

    /// <summary>
    /// 分帧执行金币创建，避免瞬间生成大量对象造成卡顿。
    /// </summary>
    private IEnumerator ProcessCreateQueue()
    {
        _isProcessing = true;

        // 金币生成延迟 0.5s
        yield return new WaitForSeconds(0.5f);

        while (_pendingCount > 0)
        {
            // 每帧生成的数量（取最小值）
            int createNum = Mathf.Min(batchPerFrame, _pendingCount);

            for (int i = 0; i < createNum; i++)
            {
                CreateOneCoin(_lastSpawnPos, _goldBaseNum);     
                _pendingCount--;
            }

            // 分帧等待（防止主线程阻塞）
            yield return null;
        }

        _isProcessing = false;
    }

    /// <summary>
    /// 创建单个金币对象并执行动画（飞出 → 停顿 → 吸回）。
    /// </summary>
    private void CreateOneCoin(Vector3 pos, int addNum)
    {
        if (_pool == null || GameLand.Default == null) return;

        // 从对象池中获取金币实例
        GameObject coin = _pool.Get();
        coin.SetActive(true);

        // 设置父节点（建议放到一个UI或场景容器下）
        coin.transform.SetParent(GameLand.Default.GenerateEffect);
        coin.transform.position = pos;
        coin.transform.localScale = Vector3.one * localScale;

        // 随机旋转角度
        coin.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-180f, 180f));

        // 随机飞出方向与距离
        float angle = Random.Range(0f, randomAngle * Mathf.PI);
        // float distance = Random.Range(0f, randomForce);
        float distance = 0;
        float x = pos.x + distance * Mathf.Cos(angle);
        float y = pos.y + distance * Mathf.Sin(angle);
        Vector3 midPos = new Vector3(x, y, 0f);

        // 动画逻辑：飞出 → 等待 → 吸附 → 回收
        coin.transform
            .DOMove(midPos, fallDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 使用 DOTween 延迟，无GC，性能好
                DOVirtual.DelayedCall(waitDuration, () =>
                {
                    coin.transform
                        .DOMove(target.position, recycleDuration)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() =>
                        {
                            SoundManager.Instance.PlayVoice(SoundType.SoundBattleCoinFly);
                            
                            // 偿还透支后再加钱
                            int actualAdded = GeneralSkillManager.Instance.OnGoldGained(addNum);
                            if (actualAdded < addNum)
                            {
                                LogTool.Log($"[GoldCoinManager] 贪婪预支：偿还透支{addNum - actualAdded}，实际到账{actualAdded}");
                            }

                            // 回收金币对象
                            _pool.Return(coin);
                        });
                });
            });
        
        // 播放声音 - 金币掉落
        // AudioKit.PlayMusic("resources://Audio/fx_goldDrop", false);
    }
    
}