using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent.Skill;
using UnityEngine;
using QFramework;
using Random = UnityEngine.Random;

namespace FusionContinent
{
    public partial class Projectile : ViewController
    {
        private float speed;
        private float damageInheritance;
        private Tween pTween;
        private Vector3 direction;
        private int PenetrateNum;
        private int baseAtk;

        public GenericObjectPool pool;

        public SpecialEffectType SpecialEffectType;

        // 新增逻辑
        public SoldierCard parentCard;
        private HashSet<string> hitTargets = new HashSet<string>(); // 用于防止对同一个目标造成多次伤害
        private Sequence damageSequence;
        private bool isDestroyed = false;
        private bool isPendingDestroy = false;
        private bool isSplits = false;
        private ISkill _skillCard;
        private string _projectileName;
       

        private bool hasTriggered = false; // 标志位，用于记录是否已经触发过

        // 回旋状态
        private RevolvingType _revolvingType;
        private float _returnRange;
        private Tweener rotationTweener;
        private BoxCollider2D detectionCollider;
        
        // 分裂效果
        
        private int _splitsNum;
        private int _splitsAgainNum;// 再次分裂数量
        private int _canSplitsNumAgain;// 能否再次分裂数量
        private float _splitsDamage;
        private float _splitsDamageAgain;
        
        
        
        private float rangeSize;

        public void Launch(Vector3 direction, float missileSpeed, ISkill skillCard, SoldierCard parent,
            float damInheritance = 1, HashSet<string> hitTarget = null, int splitsAgainNum = 1)
        {
            pTween?.Kill();

            this.direction = direction.normalized;
            this._skillCard = skillCard;
            this.speed = missileSpeed;
            this.parentCard = parent;
            this.damageInheritance = damInheritance;
            this.hitTargets = hitTarget ?? new HashSet<string>();
            this.hasTriggered = false;

            this._projectileName = skillCard.Config.ProjectileName;
            this._splitsAgainNum = splitsAgainNum;

            PenetrateNum = (int)_skillCard.Attributes.GetFinal(AttributeType.SkillPenetrateNum)
                           + (int)parent.BaseAttributes.GetFinal(AttributeType.NumberOfBulletPenetrations);
            this.rangeSize = _skillCard.Attributes.GetFinal(AttributeType.SkillRange);
            if (this.rangeSize <= 0)
            {
                this.rangeSize = 1;
            }

            // 获取分裂效果
            _splitsNum = 0;
            _splitsDamage = 0;
            _canSplitsNumAgain = 0;
            _splitsDamageAgain = 0;
            var splitSkill = _skillCard.Parent.GetSkillByType(SpecialEffectType.FenLie);
            if (splitSkill != null)
            {
                _splitsNum = (int)splitSkill.Attributes.GetFinal(AttributeType.SkillSplitsNum); // 获取分裂效果
                _splitsDamage = splitSkill.Attributes.GetFinal(AttributeType.SkillSplitsDamage); // 技能再分裂伤害继承
                _canSplitsNumAgain = (int)_skillCard.Attributes.GetFinal(AttributeType.SkillSplitsNumAgain);// 技能能否再分裂个数
                _splitsDamageAgain = _skillCard.Attributes.GetFinal(AttributeType.SkillSplitsDamageAgain);// 技能再分裂伤害继承
            }

            // _splitsNum = (int)_skillCard.Attributes.GetFinal(AttributeType.SkillSplitsNum); //获取分裂效果
            lastPosition = transform.position;
            
            // 子弹从角色身上发出（分裂子弹除外）
            if (_splitsAgainNum == 1)
            {
                transform.position = parentCard.transform.position;
            }

            // 计算子弹朝向
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // 计算目标位置并开始移动
            Vector3 targetPosition = transform.position + direction * 100f;
            pTween = transform.DOMove(targetPosition, speed).SetEase(Ease.Linear).SetSpeedBased();

            // 回旋逻辑
            var lvCfg = _skillCard.Config.LevelPropertyList[_skillCard.Level - 1];
            if (lvCfg.RevolvingStatus.Count > 0)
            {
                _revolvingType = lvCfg.RevolvingStatus[0].Type;
                _returnRange = lvCfg.RevolvingStatus[0].ReturnRange;
                StartRotation(360);
            }

            // 自动创建或获取碰撞体
            detectionCollider = GetComponent<BoxCollider2D>();
            if (detectionCollider == null)
            {
                detectionCollider.size = new Vector2(2f, 2f);
                detectionCollider.offset = Vector2.zero;
                detectionCollider.isTrigger = false;
            }
        }


        private Vector3 lastPosition; // 记录上一帧位置

        private void OnEnable()
        {
            transform.position = Vector3.zero;
            lastPosition = transform.position;
            // hasTriggered = false;
        }

        private void FixedUpdate()
        {
            // 检测子弹是否超出屏幕范围
            if (!IsOnScreen(transform.position))
            {
                StartCoroutine(DisableAfterDelay(pool, gameObject, Random.Range(0.05f, 0.5f))); // 销毁对象
            }

            if (!GamePlayManager.Default.IsPaused())
            {
                // 回旋距离
                if (_returnRange > 0f && transform.PositionY() >= (_returnRange / 100))
                {
                    ReturnBoomerang();
                }

                if (parentCard == null || parentCard.IsDie())
                {
                    StartCoroutine(DisableAfterDelay(pool, gameObject, 0.01f)); // 销毁对象
                }
            }

            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
            // 添加碰撞检测
            RayCastCollider(detectionCollider);

            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        }

        private HashSet<MonsterCard> hitMonsters = new HashSet<MonsterCard>(); // 存储已命中的怪物

        public void RayCastCollider(BoxCollider2D collider)
        {
            if (collider == null || hasTriggered) return;

            // 计算碰撞器的中心偏移
            Vector2 colliderOffset = collider.offset;
            Vector2 worldOffset = transform.TransformVector(colliderOffset);

            Vector2 currentPosWithOffset = (Vector2)transform.position + worldOffset;
            Vector2 lastPosWithOffset = (Vector2)lastPosition + worldOffset;

            Vector2 movement = currentPosWithOffset - lastPosWithOffset;
            float distance = movement.magnitude;

            if (distance > 0f)
            {
                Vector2 worldSize = Vector2.Scale(collider.size, transform.lossyScale);
                Vector2 castSize = worldSize * 0.5f * rangeSize; // 缩小一半

                RaycastHit2D[] hits = Physics2D.BoxCastAll(
                    lastPosWithOffset, // 使用带偏移的位置
                    castSize,
                    transform.eulerAngles.z,
                    movement.normalized,
                    distance,
                    LayerMask.GetMask("MonsterBox")
                );
                
                //特殊场景交互
                RaycastHit2D[] hits2 = Physics2D.BoxCastAll(
                    lastPosWithOffset, // 使用带偏移的位置
                    castSize,
                    transform.eulerAngles.z,
                    movement.normalized,
                    distance,
                    LayerMask.GetMask("InterProp")
                );
                //LogTool.LogError(hits2.Length);
                foreach (var hit in hits2)
                {
                    if (hit.collider == null || !hit.collider.CompareTag("InterProp")) continue;
                    hit.collider.GetComponent<SceneInteractiveProp>().CreateEffectHit(this.transform.position);
                }

                foreach (var hit in hits)
                {
                    if (hit.collider == null || hit.collider.name != "HitBox") continue;
                    MonsterCard hitTarget = hit.collider.transform.parent.GetComponent<MonsterCard>();
                    if (hitTarget.monsterData.monsterState == MonsterState.die) continue;
                    if (hitTarget != null && hitTarget.gameObject.activeSelf &&
                        !hitTargets.Contains(hitTarget.uniqueId) && hitTarget.enabled && !hasTriggered)
                    {
                        hitTargets.Add(hitTarget.uniqueId);

                        // 受伤坐标位置
                        var damageHitPos = hit.collider.bounds.ClosestPoint(currentPosWithOffset);

                        // 特殊类型技能
                        switch (_skillCard.Type)
                        {
                            case SpecialEffectType.Wanjian: // 万剑特效
                            case SpecialEffectType.SquallyShowers:
                                GenericObjectPool pool2 =
                                    ObjectPoolManager.Instance.GetGameObjectPool("wanjianprefect");
                                if (pool2 != null)
                                {
                                    var wjEfect = pool2.Get();
                                    wjEfect.transform.SetParent(GameLand.Default.DamageParent.transform);
                                    wjEfect.transform.position = hitTarget.transform.position;
                                    wjEfect.transform.localScale = Vector3.one * 200;
                                    StartCoroutine(DisableAfterDelay(pool2, wjEfect, 0.5f));
                                }

                                break;
                        }

                        hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                            hitTarget, _skillCard, damageHitPos, damageInheritance));

                        // 魔法爆炸
                        MagicianBombAttack(hitTarget);

                        if (_skillCard == null) continue;

                        // 连续伤害逻辑
                        var continuousDamageNum =
                            (int)_skillCard.Attributes.GetFinal(AttributeType.SkillContinuousDamageNum);
                        if (continuousDamageNum > 0)
                        {
                            ApplyContinuousDamage(hitTarget, continuousDamageNum);
                        }

                        // 分裂逻辑
                        if (_splitsNum > 0 && _splitsAgainNum > 0)
                        {
                            // 分裂箭不读表（写死）
                            _projectileName = "ArcherProjectile";
                            // var splitsNumAgain = (int)_skillCard.Attributes.GetFinal(AttributeType.SkillSplitsNumAgain);
                            switch (_splitsAgainNum)
                            {
                                case 1:
                                    // if (_skillCard.Config.ActsOnCard == Global.ArcherCardId)
                                    // _projectileName += "Splits";

                                    _splitsAgainNum = _canSplitsNumAgain > 0 ? 2 : 0;// 判断是否可再次分裂
                                    SplitProjectile(_splitsNum, damageHitPos, _splitsDamage);
                                    break;
                                case 2:
                                    _splitsAgainNum = 0;
                                    // if (_skillCard.Config.ActsOnCard == Global.ArcherCardId)
                                    // _projectileName += "SplitsAgain";

                                    this.hitTargets = new HashSet<string>();
                                    SplitProjectile(_canSplitsNumAgain, damageHitPos, _splitsDamageAgain);
                                    break;
                            }
                        }
                        
                        // 怪物受击概率无法穿透
                        if (PenetrateNum > 0 && 
                            hitTarget.Attributes.GetFinal(AttributeType.MonsterHitProbabilityImpenetrable) > 0f)
                        {
                            PenetrateNum = 0;// 直接设置为0，强制不穿透
                        }

                        // 穿透逻辑
                        if (PenetrateNum > 0)
                        {
                            PenetrateNum--;
                            // 穿透伤害继承
                            var penetrateDamageInheritance =
                                _skillCard.Attributes.GetFinal(AttributeType.SkillPenetrateInheritance);
                            if (penetrateDamageInheritance > 0f)
                            {
                                damageInheritance *= penetrateDamageInheritance;
                            }
                        }
                        else
                        {
                            hasTriggered = true;
                            DestroyProjectile();
                        }
                    }
                }
            }

            lastPosition = transform.position; // 更新上一帧位置
        }

        public void ResetHitMonsters()
        {
            hitMonsters.Clear();
            LogTool.Log("已重置命中列表，可再次触发怪物");
        }


        // 检测物体是否在屏幕内的方法
        private bool IsOnScreen(Vector3 position)
        {
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(position);
            return screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
        }

        // 在 Projectile 类中添加以下代码

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;


            // 绘制碰撞盒
            if (detectionCollider != null && !hasTriggered)
            {
                Gizmos.color = Color.green;

                // 计算碰撞盒的世界位置和旋转
                Vector2 worldPos = detectionCollider.transform.TransformPoint(detectionCollider.offset);
                Vector2 size = detectionCollider.size * 0.5f * rangeSize;
                float angle = detectionCollider.transform.eulerAngles.z;

                // 保存当前矩阵
                Matrix4x4 originalMatrix = Gizmos.matrix;

                // 设置旋转矩阵
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(
                    worldPos,
                    Quaternion.Euler(0, 0, angle),
                    Vector3.one
                );
                Gizmos.matrix = rotationMatrix;

                // 绘制碰撞盒
                Gizmos.DrawWireCube(Vector3.zero, size);

                // 恢复原始矩阵
                Gizmos.matrix = originalMatrix;

                // 绘制检测到的目标
                Gizmos.color = Color.yellow;
                int enemyLayer = LayerMask.GetMask("MonsterBox");
                Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, size, angle, enemyLayer);
            }

            // 绘制投射物路径
            if (pTween != null && pTween.IsActive())
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, transform.position + direction * 5f);
            }

            // 绘制回旋镖返回范围
            if (_returnRange > 0)
            {
                Gizmos.color = Color.yellow;
                float returnY = _returnRange / 100f;
                Gizmos.DrawLine(
                    new Vector3(-10, returnY, 0),
                    new Vector3(10, returnY, 0)
                );
            }
        }
#endif

        // 回旋镖返回
        private void ReturnBoomerang()
        {
            hitTargets.Clear();
            if (parentCard != null)
            {
                transform.DOMove(parentCard.transform.position, speed).SetEase(Ease.Linear).SetSpeedBased();
            }

            StartRotation(-360); // 逆时针旋转
        }

        // 开始旋转
        private void StartRotation(float angle)
        {
            if (_revolvingType != RevolvingType.Rotate360) return;

            if (rotationTweener != null)
            {
                rotationTweener.Kill();
            }

            rotationTweener = transform.DORotate(new Vector3(0, angle, 0), 0.2f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental);
        }

        private void DestroyProjectile()
        {
            // 连续伤害逻辑
            var continuousDamageNum = (int)_skillCard.Attributes.GetFinal(AttributeType.SkillContinuousDamageNum);
            if (continuousDamageNum > 0 && damageSequence != null && damageSequence.IsActive())
            {
                // 如果有连续伤害且还未完成，隐藏子弹并等待伤害完成再销毁
                HideProjectile();
                isPendingDestroy = true;
            }
            else
            {
                // 没有连续伤害或伤害已经完成，直接销毁
                isDestroyed = true;
                pTween.Kill();
                StartCoroutine(DisableAfterDelay(pool, gameObject, 0.01f)); // 销毁对象
            }
        }

        private void HideProjectile()
        {
            // 隐藏子弹对象
            gameObject.transform.localScale = Vector3.zero;
            damageSequence.OnComplete(() =>
            {
                if (isPendingDestroy)
                {
                    DestroyProjectile(); // 如果标记为待销毁，则在连续伤害完成后销毁子弹
                }
            });
        }

        private void ApplyContinuousDamage(MonsterCard target, int damageNum)
        {
            // 使用 DOTween 的 Sequence 代替协程处理连续伤害
            damageSequence = DOTween.Sequence();

            for (int i = 0; i < damageNum; i++)
            {
                damageSequence.AppendCallback(() =>
                {
                    if (target != null && target.enabled != false)
                    {
                        target.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(target, _skillCard, this.transform.position));
                    }
                });
                damageSequence.AppendInterval(0.1f); // 每次伤害间隔
            }
        }

        /**
         * 分裂子弹
         */
        private void SplitProjectile(int splitsNum, Vector3 hitTargetPos, float damInheritance = 1)
        {
            for (int i = 0; i < splitsNum; i++)
            {
                // 计算每个投射物的角度偏移，保持对称分布
                float angleOffset = 0f;
                if (splitsNum > 1)
                {
                    int halfCount = splitsNum / 2;
                    if (i < halfCount) // 左侧子弹
                    {
                        angleOffset = Random.Range(-45f, -15f);
                    }
                    else if (i > halfCount) // 右侧子弹
                    {
                        angleOffset = Random.Range(15f, 45f);
                    }
                }
                else
                {
                    angleOffset = Random.Range(-45f, 45f);
                }

                Vector3 finalDirection = Quaternion.Euler(0, 0, angleOffset) * direction;

                // 创建分裂子弹
                CreateSplitProjectile(finalDirection, hitTargetPos, damInheritance);
            }
        }

        /**
         * 创建分裂子弹
         */
        private void CreateSplitProjectile(Vector3 splitDirection, Vector3 hitTargetPos, float damInheritance)
        {
            // LogTool.Log($">>>>>> CreateSplitProjectile - _projectileName: {_projectileName}");
            var poolProjectile = ObjectPoolManager.Instance.GetGameObjectPool(this._projectileName);
            if (poolProjectile != null)
            {
                // 实例化投射物
                GameObject splitProjectile = poolProjectile.Get();
                splitProjectile.transform.SetParent(GameLand.Default.Bullet);
                splitProjectile.LocalScale(1f);
                splitProjectile.transform.position = hitTargetPos;

                var splitProjectileScript = splitProjectile.GetComponent<Projectile>();
                splitProjectileScript.pool = poolProjectile;

                splitProjectileScript.Launch(splitDirection, speed / 2, this._skillCard, parentCard, damInheritance,
                    this.hitTargets, _splitsAgainNum);
            }
        }

        /**
         * 魔法爆炸
         */
        private void MagicianBombAttack(MonsterCard hitTarget)
        {
            // var skillMagicExplosion = _skillCard.Parent.GetReadySkillByType(SpecialEffectType.MagicExplosion);
            // if (skillMagicExplosion != null)
            // {
            //     GenericObjectPool pool3 = ObjectPoolManager.Instance.GetGameObjectPool("MagicianBombSkill2");
            //     if (pool3 != null)
            //     {
            //         var prefect = pool3.Get();
            //         prefect.transform.SetParent(GameLand.Default.DamageParent.transform);
            //         prefect.transform.position = hitTarget.transform.position;
            //         prefect.transform.localScale =
            //             Vector3.one * skillMagicExplosion.Attributes.GetFinal(AttributeType.SkillRange);
            //
            //         // 调用碰撞脚本
            //         var magicianBombScript = prefect.GetComponent<MagicianBombAttack>();
            //         magicianBombScript.StartAttack(pool3, skillMagicExplosion);
            //     }
            // }
        }

        private IEnumerator DisableAfterDelay(GenericObjectPool pool, GameObject damageObject, float delay)
        {
            // 等待指定的时间
            yield return new WaitForSeconds(delay);

            // 检查对象是否仍然有效
            if (damageObject != null && pool != null)
            {
                pool.Return(damageObject);
            }
        }

        private void OnDisable()
        {
            // 确保子弹销毁时，停止所有与其相关的 DOTween 动画
            DOTween.Kill(transform, true);
        }

        private void OnDestroy()
        {
            // 确保子弹销毁时，停止所有与其相关的 DOTween 动画
            DOTween.Kill(transform, true);
        }
    }
}