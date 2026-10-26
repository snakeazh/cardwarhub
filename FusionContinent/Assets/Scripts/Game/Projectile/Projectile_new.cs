using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using FusionContinent.Skill.Handler;
using QFramework;
using UnityEngine;


public class HitContext
{
    /// <summary>
    /// 所有命中的目标列表
    /// </summary>
    public List<HitResult> Hits = new List<HitResult>();

    /// <summary>
    /// 已命中目标的唯一ID集合（用于防止重复碰撞检测）
    /// Key = MonsterCard.uniqueId
    /// </summary>
    public HashSet<string> HitUniqueIds = new HashSet<string>();

    /// <summary>
    /// 添加命中结果列表
    /// </summary>
    public void AddHits(List<HitResult> hits)
    {
        if (hits == null) return;
        Hits.AddRange(hits);
    }

    /// <summary>
    /// 记录已命中的目标ID
    /// </summary>
    public void AddHitUniqueId(string uniqueId)
    {
        HitUniqueIds.Add(uniqueId);
    }

    /// <summary>
    /// 检查目标是否已命中过
    /// </summary>
    public bool HasHit(string uniqueId)
    {
        return HitUniqueIds.Contains(uniqueId);
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        Hits.Clear();
        HitUniqueIds.Clear();
    }
}


internal static class ProjectileLayerMasks
{
    private static int _sceneOutBoxLayer = -1;

    public static int SceneOutBox
    {
        get
        {
            if (_sceneOutBoxLayer < 0)
            {
                _sceneOutBoxLayer = LayerMask.GetMask("SceneOutBox");
            }

            return _sceneOutBoxLayer;
        }
    }
}


public class Projectile_new : MonoBehaviour
{
    /**
     * 通用投射物子弹
     * 
     */

    private static readonly Collider2D[] OutBoxOverlapResults = new Collider2D[4];
    
    public int id;
    // 运行时数据
    private Vector3 _direction;
    public Vector3 Direction { get => _direction;set => _direction = value; }
    private Vector3 _startPos;
    public Vector3 StartPos { get => _startPos;}

    private ProjectileConfig _config = new ProjectileConfig();
    public ProjectileConfig Config { get => _config; set => _config = value; }

    public SkillContext _skillContext;
    
    private IProjectileMove _move;
    // 分裂相关属性
    private int _splitsAgainNum;
    private float _damageInheritance;
    private string _projectileName;
    public SoldierCard parentCard;
    public ISkill skillCard;
    
    // 命中信息（供 SplitHitModule 使用）
    public Vector3 LastHitPosition { get; private set; }

    /// <summary>
    /// 需要排除的目标ID集合（分裂子弹不应再次命中的目标）
    /// </summary>
    public HashSet<string> ExcludedTargetIds { get; private set; } = new HashSet<string>();

    private List<IProjectileHitModule> _modules = new List<IProjectileHitModule>();
    internal List<IProjectileHitModule> HitModules => _modules;
    
    private float lifeTime;
    private float timer;

    public bool EnableBurnAreaOnMove { get; set; }
    public string BurnAreaTriggerMode { get; set; } = "Distance";
    public float BurnAreaTimeInterval { get; set; } = 0.2f;
    public float BurnAreaDistanceInterval { get; set; } = 1.5f;
    public string BurnAreaEffect { get; set; } = string.Empty;
    public float BurnAreaDuration { get; set; } = 2f;
    public float BurnAreaRadius { get; set; } = 2f;
    public float BurnAreaTickInterval { get; set; } = 1f;
    public float BurnAreaDamage { get; set; } = 10f;
    public bool BurnAreaUseSkillDamage { get; set; }
    public int BurnAreaBuffId { get; set; }
    public float BurnAreaBuffStayTime { get; set; }

    private float _burnAreaSpawnTimer;
    private Vector3 _lastBurnAreaSpawnPos;
    private bool _hasBurnAreaSpawnPoint;
    private GenericObjectPool _burnAreaPool;

    public bool destoryself = false;
    public bool DisableHitDetection { get; set; }
    public float HitDetectionDeltaTime { get; private set; }
    public float MaxHitCheckInterval { get; set; } = 0.05f;
    public float HitCheckDistanceThreshold { get; set; }
    public Vector3 HitCheckSegmentStart { get; private set; }
    public bool ForceHitAtCurrentPosition { get; private set; }

    private float _hitCheckElapsed;
    private Vector3 _lastHitCheckPosition;
    private bool _hasLastHitCheckPosition;
    
    public void Init(IProjectileMove move, List<IProjectileHitModule> modules,SkillContext skillContext,Vector3 startPos,Vector3 direction)
    {
        this._move = move;
        this._modules = modules;
        this._skillContext = skillContext;
        lifeTime = _config.lifeTime;
        timer = 0;
        _startPos = startPos;
        _direction = direction;
        transform.position = startPos;
        destoryself = false;
        DisableHitDetection = false;
        _burnAreaSpawnTimer = 0f;
        _lastBurnAreaSpawnPos = startPos;
        _hasBurnAreaSpawnPoint = false;
        _burnAreaPool = null;
        HitDetectionDeltaTime = 0f;
        _hitCheckElapsed = 0f;
        _lastHitCheckPosition = startPos;
        _hasLastHitCheckPosition = false;
        
        // 清空排除目标列表（子弹重用时需要重置）
        ExcludedTargetIds.Clear();
        
        // 重置所有命中模块的状态
        for (int i = 0; i < _modules.Count; i++)
        {
            _modules[i].ResetHitTarget(this);
        }
        // 从 SkillContext 中获取技能卡牌和施法者
        if (skillContext?.Caster != null)
        {
            var soldierCard = skillContext.Caster.GetComponent<SoldierCard>();
            if (soldierCard != null)
            {
                this.parentCard = soldierCard;
                this.skillCard = soldierCard.GetSkillById(skillContext.SkillId);
            }
        }
        
        // 固定朝向
        if (_config.fixedRotation >= 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, _config.fixedRotation);
            float rad = _config.fixedRotation * Mathf.Deg2Rad;

            // direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
        }
        else
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

    }

    /// <summary>
    /// 分裂子弹启动方法 - 用于创建分裂子弹
    /// </summary>
    // public void LaunchSplit(
    //     Vector3 direction,
    //     float speed,
    //     ISkill skillCard,
    //     SoldierCard parentCard,
    //     float damageInheritance,
    //     int splitsAgainNum)
    // {
    //     _direction = direction.normalized;
    //     this.skillCard = skillCard;
    //     this.parentCard = parentCard;
    //     _damageInheritance = damageInheritance;
    //     _splitsAgainNum = splitsAgainNum;
    //     _projectileName = skillCard?.Config?.ProjectileName ?? "ArcherProjectile";
    //
    //     // 初始化 Config（供 SplitHitModule.OnHit 访问）
    //     _config.speed = speed;
    //
    //     // 分裂子弹位置设置
    //     transform.position = parentCard?.transform.position ?? Vector3.zero;
    //
    //     // 创建移动方式（直线移动）
    //     var move = new StraightMove(_direction, speed);
    //     
    //     // 创建分裂子弹专用的命中模块列表
    //     var modules = CreateSplitModules(splitsAgainNum, damageInheritance);
    //     
    //     // 创建假 SkillContext（用于分裂子弹的伤害检测）
    //     var context = new SkillContext
    //     {
    //         Caster = parentCard,
    //         SkillId = skillCard?.Id ?? 0
    //     };
    //     
    //     // 调用 Init 方法初始化
    //     Init(move, modules, context, transform.position, _direction);
    //     
    //     // 覆盖生命周期为较长的时间
    //     lifeTime = 10f;
    // }
    
    /// <summary>
    /// 为分裂子弹创建命中模块列表
    /// </summary>
    private List<IProjectileHitModule> CreateSplitModules(int splitsAgainNum, float damageInheritance)
    {
        var modules = new List<IProjectileHitModule>();
        
        // 1. 添加穿透模块（用于伤害检测）
        modules.Add(new PenetrateHitModule 
        { 
            MaxCount = 3, 
            curCount = 0, 
            _hitTargetDuration = 0f 
        });
        
        // 2. 如果可以再次分裂，添加分裂模块
        if (splitsAgainNum > 0)
        {
            var splitModule = new SplitHitModule
            {
                SplitsNum = 2,
                SplitsDamage = damageInheritance,
                SplitsDamageAgain = damageInheritance * 0.5f,
                SplitsAgainCount = splitsAgainNum
            };
            modules.Add(splitModule);
        }
        
        return modules;
    }

    /// <summary>
    /// 分裂子弹启动方法（带自定义分裂模块）
    /// </summary>
    public void LaunchSplitWithModules(
        Vector3 direction,
        float speed,
        ISkill skillCard,
        SoldierCard parentCard,
        float damageInheritance,
        int splitsAgainNum,
        SplitHitModule splitModule,
        HashSet<string> excludedTargetIds = null)
    {
        _direction = direction.normalized;
        this.skillCard = skillCard;
        this.parentCard = parentCard;
        _damageInheritance = damageInheritance;
        _splitsAgainNum = splitsAgainNum;
        _projectileName = skillCard?.Config?.ProjectileName ?? "ArcherProjectile";

        // 初始化 Config（供 SplitHitModule.OnHit 访问）
        _config.speed = speed;
        _config.checkRadius = 0.1f;

        // 创建移动方式（直线移动）
        var move = new StraightMove(_direction, speed);
        
        // 创建命中模块列表
        var modules = new List<IProjectileHitModule>();
        
        // 1. 添加穿透模块
        modules.Add(new PenetrateHitModule 
        { 
            MaxCount = 1, 
            curCount = 0, 
            _hitTargetDuration = 0f 
        });
        
        // 2. 添加分裂模块（检查模块的分裂次数配置）
        if (splitModule != null && splitModule.SplitsAgainCount > 0)
        {
            modules.Add(splitModule);
        }

        // 创建假 SkillContext
        var context = new SkillContext
        {
            Caster = parentCard,
            SkillId = skillCard?.Id ?? 0
        };
        
        // 调用 Init 方法初始化
        Init(move, modules, context, transform.position, _direction);
        ProjectileManager.Instance.Add(this);
        // 覆盖生命周期
        lifeTime = 10f;
        
        // Init 之后设置排除目标（因为 Init 会清空）
        if (excludedTargetIds != null)
        {
            foreach (var id in excludedTargetIds)
            {
                ExcludedTargetIds.Add(id);
            }
        }
    }

    private HitContext _hitContext = new HitContext();

    public void TriggerHitModulesAtCurrentPosition()
    {
        HitCheckSegmentStart = transform.position;
        ProcessHitModules(forceAtCurrentPosition: true);
        _lastHitCheckPosition = transform.position;
    }

    public void RequestMissionOver()
    {
        _pendingMissionOver = true;
    }

    public void CancelMissionOver()
    {
        if (!destoryself)
        {
            return;
        }

        destoryself = false;
        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.CancelRecycle(this);
        }
    }

    public void RedirectToDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        _direction = direction.normalized;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        if (_move is StraightMove straightMove)
        {
            straightMove.SetDirection(_direction);
        }

        ResetHitCheckAfterRedirect();
    }

    private void ResetHitCheckAfterRedirect()
    {
        _lastHitCheckPosition = transform.position;
        _hitCheckElapsed = 0f;
        HitDetectionDeltaTime = 0f;
    }

    private bool _pendingMissionOver;

    private void ProcessHitModules(bool forceAtCurrentPosition)
    {
        _hitContext.Clear();
        ForceHitAtCurrentPosition = forceAtCurrentPosition;
        _pendingMissionOver = false;

        for (int i = 0; i < _modules.Count; i++)
        {
            _modules[i].CollectHits(this, _hitContext);
        }

        ForceHitAtCurrentPosition = false;

        if (_hitContext.Hits.Count == 0 && !forceAtCurrentPosition)
        {
            return;
        }

        LastHitPosition = _hitContext.Hits.Count > 0
            ? _hitContext.Hits[0].hitPoint
            : transform.position;

        for (int i = 0; i < _modules.Count; i++)
        {
            _modules[i].OnHit(this, _hitContext);
        }

        if (_pendingMissionOver)
        {
            MissionOver();
        }
    }

    public void Tick(float dt)
    {
        Vector3 posBeforeMove = transform.position;
        _move?.Move(this, dt);

        if (IsOutsideScreen(posBeforeMove, transform.position))
        {
            MissionOver();
            return;
        }

        TrySpawnBurnArea(dt);

        bool shouldDetectHit = !DisableHitDetection && ShouldRunHitDetection(dt);
        if (shouldDetectHit)
        {
            HitCheckSegmentStart = _lastHitCheckPosition;
            ProcessHitModules(forceAtCurrentPosition: false);
            _lastHitCheckPosition = transform.position;
        }

        // 🟢 3. 生命周期
        timer += dt;
        if (timer >= lifeTime)
        {
            MissionOver();
        }
    }

    private bool IsOutsideScreen(Vector2 from, Vector2 to)
    {
        float checkRadius = _config.checkRadius > 0f ? _config.checkRadius : 0.1f;

        int overlapCount = Physics2D.OverlapCircleNonAlloc(to, checkRadius, OutBoxOverlapResults, ProjectileLayerMasks.SceneOutBox);
        if (overlapCount > 0)
        {
            return true;
        }

        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.001f)
        {
            return false;
        }

        return HitSystem2D.Ray(from, delta / distance, distance, ProjectileLayerMasks.SceneOutBox).Count > 0;
    }

    private bool ShouldRunHitDetection(float dt)
    {
        _hitCheckElapsed += dt;

        // 避免空场景下持续做碰撞查询
        if (MonsterManager.Instance != null && MonsterManager.Instance.enemies.Count == 0)
        {
            return false;
        }

        if (!_hasLastHitCheckPosition)
        {
            _lastHitCheckPosition = transform.position;
            _hasLastHitCheckPosition = true;
            HitDetectionDeltaTime = Mathf.Max(_hitCheckElapsed, dt);
            _hitCheckElapsed = 0f;
            return true;
        }

        float elapsedSinceLastCheck = _hitCheckElapsed;
        float maxInterval = MaxHitCheckInterval > 0f ? MaxHitCheckInterval : 0.05f;
        bool reachMaxInterval = elapsedSinceLastCheck >= maxInterval;

        float distanceThreshold = HitCheckDistanceThreshold > 0f
            ? HitCheckDistanceThreshold
            : Mathf.Max(0.15f, 0.1f);
        float movedSqr = (transform.position - _lastHitCheckPosition).sqrMagnitude;
        bool movedEnough = movedSqr >= distanceThreshold * distanceThreshold;

        if (!reachMaxInterval && !movedEnough)
        {
            return false;
        }

        HitDetectionDeltaTime = Mathf.Max(elapsedSinceLastCheck, dt);
        _hitCheckElapsed = 0f;
        return true;
    }
    
    // public void Tick(float dt)
    // {
    //     _move?.Move(this, dt);
    //     
    //     for (int i = 0; i < _modules.Count; i++)
    //     {
    //         if (_modules[i].OnCheckHit(this))
    //         {
    //             // 保存命中信息供其他模块使用（如 SplitHitModule）
    //             var hitModule = _modules[i] as PenetrateHitModule;
    //             if (hitModule != null)
    //             {
    //                 LastHits = hitModule.hits;
    //                 if (LastHits != null && LastHits.Count > 0)
    //                 {
    //                     LastHitPosition = LastHits[0].hitPoint;
    //                 }
    //             }
    //             
    //             var aoeModule = _modules[i] as AoeHitModule;
    //             if (aoeModule != null)
    //             {
    //                 LastHits = aoeModule.hits;
    //             }
    //             
    //             _modules[i].OnHit(this);
    //         }
    //     }
    //
    //     timer += dt;
    //     if (timer >= lifeTime)
    //     {
    //         MissionOver();
    //     }
    // }

    private void TrySpawnBurnArea(float dt)
    {
        if (!EnableBurnAreaOnMove)
        {
            return;
        }

        if (string.Equals(BurnAreaTriggerMode, "Time", System.StringComparison.OrdinalIgnoreCase))
        {
            if (BurnAreaTimeInterval <= 0f)
            {
                return;
            }

            _burnAreaSpawnTimer += dt;
            while (_burnAreaSpawnTimer >= BurnAreaTimeInterval)
            {
                _burnAreaSpawnTimer -= BurnAreaTimeInterval;
                SpawnBurnArea(transform.position);
            }
            return;
        }

        if (BurnAreaDistanceInterval <= 0f)
        {
            return;
        }

        if (!_hasBurnAreaSpawnPoint)
        {
            _lastBurnAreaSpawnPos = transform.position;
            _hasBurnAreaSpawnPoint = true;
            SpawnBurnArea(_lastBurnAreaSpawnPos);
            return;
        }

        var currentPosition = transform.position;
        var delta = currentPosition - _lastBurnAreaSpawnPos;
        var distance = delta.magnitude;
        if (distance < BurnAreaDistanceInterval)
        {
            return;
        }

        var direction = delta / distance;
        while (distance >= BurnAreaDistanceInterval)
        {
            _lastBurnAreaSpawnPos += direction * BurnAreaDistanceInterval;
            SpawnBurnArea(_lastBurnAreaSpawnPos);
            distance -= BurnAreaDistanceInterval;
        }
    }

    private void SpawnBurnArea(Vector3 position)
    {
        if (_burnAreaPool == null)
        {
            var poolRoot = GameLand.Default != null ? GameLand.Default.Bullet : null;
            _burnAreaPool = ObjectPoolManager.Instance?.CreateGameObjectPool(BurnArea.PoolKey, null, poolRoot);
        }

        if (_burnAreaPool == null)
        {
            LogTool.LogWarning("[Projectile_new] BurnArea 对象池创建失败");
            return;
        }

        var areaObject = _burnAreaPool.Get();
        areaObject.name = "BurnArea";
        areaObject.transform.SetParent(GameLand.Default.Bullet);
        areaObject.transform.position = position;
        areaObject.transform.rotation = Quaternion.identity;
        areaObject.transform.localScale = Vector3.one;

        var burnArea = areaObject.GetComponent<BurnArea>();
        if (burnArea == null)
        {
            burnArea = areaObject.AddComponent<BurnArea>();
        }

        burnArea.Init(
            _skillContext,
            BurnAreaEffect,
            BurnAreaDuration,
            BurnAreaRadius,
            BurnAreaTickInterval,
            BurnAreaDamage,
            BurnAreaUseSkillDamage,
            BurnAreaBuffId,
            BurnAreaBuffStayTime,
            _burnAreaPool);
    }

    //特殊移动规则可能会清空击中逻辑
    public void ResetHitTarget()
    {
        ExcludedTargetIds.Clear();
        for (int i = 0; i < _modules.Count; i++)
        {
            _modules[i].ResetHitTarget(this);
        }
    }

    //使命结束
    public void MissionOver()
    {
        if(destoryself)
            return;
        destoryself = true;
        // 播放结束特效
        if (!string.IsNullOrEmpty(_config.endEffect))
        {
            EffectManager.Instance.PlayEffect(
                _config.endEffect,
                transform.position,
                scale: transform.lossyScale,
                rotation: transform.rotation);
        }

        ProjectileManager.Instance.Recycle(this);
    }
}
