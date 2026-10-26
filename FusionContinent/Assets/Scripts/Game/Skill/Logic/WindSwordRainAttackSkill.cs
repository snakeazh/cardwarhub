using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

namespace FusionContinent.Skill
{
    /**
     * 风之箭雨
     **/
    public class WindSwordRainAttackSkill : SkillBase
    {
        // 配置参数
        public string arrowPoolName = "AnnaSkillRandomShoot09";
        public float spawnInterval = 0.01f;
        public float arrowLifeTime = 1.5f;
        private float starttime = 0.2f;

        private float _continuousAtkSpeed = 1.2f;

        // 对象池名称常量
        private const string StartEffectPool = "AnnaSkillRandomShoot11";
        private const string StartEffectPool2 = "AnnaSkillRandomShoot12";
        private const string MineEffectPool = "AnnaSkillRandomShoot08";

        private const string SkillRandomShootBottom = "AnnaSkillRandomShoot07";
        
        // DOTween管理
        private List<Tween> _activeTweens = new List<Tween>();
        private Sequence _mainSequence;
        private Sequence _bottomSequence;
        private bool _isCasting = false;

        protected override void OnCast(object finalDirection)
        {
            if (_isCasting) return; // 防止重复施放
            _isCasting = true;
            CleanupTweens(); // 先清理之前的动画

            Vector3 targetPosition = (Vector3)finalDirection;
            float skillRange = Attributes.GetFinal(AttributeType.SkillRange);
            starttime = 1f;

            // 播放起始效果
            PlayStartEffect(Parent.transform.position, skillRange);

            var thunderboltRealDamage = Attributes.GetFinal(AttributeType.SkillThunderboltRealDamage);
            if (thunderboltRealDamage > 0)
            {
                // 播放地雷效果
                PlayMineEffect(targetPosition, thunderboltRealDamage);
                starttime += 0.3f;
            }
            
            // 技能持续时间
            var duration = Attributes.GetFinal(AttributeType.SkillDuration);
            
            // 技能射速
            spawnInterval = Mathf.Max(Attributes.GetFinal(AttributeType.SkillContinuousAtkSpeed), 0.05f);
            
            // 技能箭矢数量
            var skillArrowNum = Mathf.Max((int)Attributes.GetFinal(AttributeType.SkillArrowNum), 1);
            
            // 技能连射速度
            _continuousAtkSpeed = Mathf.Max((int)Attributes.GetFinal(AttributeType.SkillContinuousAtkSpeed), 1.2f);

            // 延迟播放循环效果和箭雨
            var delayTween = DOVirtual.DelayedCall(starttime, () =>
            {
                if (!_isCasting) return; // 检查是否还在施法状态
                
                // StartSkillRandomShootBottom(targetPosition, skillRange, duration);
                CreateArrowRain(targetPosition, skillRange, duration, skillArrowNum);

            });
            RegisterTween(delayTween);
        }


        private void PlayStartEffect(Vector3 position, float scale)
        {
            var effect = GetEffectFromPool(StartEffectPool);
            if (effect == null) return;

            effect.transform.SetParent(GamePlayManager.Default.droppingFireController.transform);
            effect.transform.position = position;
            effect.transform.localScale = Vector3.one * scale;

            // 1秒后回收
            var returnTween = DOVirtual.DelayedCall(0.6f, () =>
            {
                if (effect != null)
                    ReturnEffectToPool(StartEffectPool, effect);
                var effect2 = GetEffectFromPool(StartEffectPool2);
                if (effect2 == null) return;

                effect2.transform.SetParent(GamePlayManager.Default.droppingFireController.transform);
                effect2.transform.position = position;
                effect2.transform.localScale = Vector3.one * scale;
                DOVirtual.DelayedCall(0.4f, () =>
                {
                    if (effect2 != null)
                    {
                        ReturnEffectToPool(StartEffectPool2, effect2);
                    }
                });
            });
            RegisterTween(returnTween);
        }

        private float lastMineEffectTime = -999f; // 上次触发时间

        private void PlayMineEffect(Vector3 position, float thunderboltRealDamage)
        {
            if (Time.time - lastMineEffectTime < 0.5f) return; // 冷却未结束
            lastMineEffectTime = Time.time;

            var effect = GetEffectFromPool(MineEffectPool);
            if (effect == null) return;

            effect.transform.SetParent(GamePlayManager.Default.droppingFireController.transform);
            effect.transform.position = position;

            var arrowBullet = effect.transform.GetChild(0).Find("box")?.GetComponent<ArrowBullet>();
            if (arrowBullet != null)
            {
                arrowBullet.StartAttackSet(this, thunderboltRealDamage);
            }
        }
        
        private void PlayEndEffect(Vector3 position)
        {
            var thunderboltRealDamage = Attributes.GetFinal(AttributeType.SkillThunderboltRealDamage);
            if (thunderboltRealDamage > 0)
            {
                PlayMineEffect(position, thunderboltRealDamage);
            }


            var returnTween = DOVirtual.DelayedCall(0.5f, () => { ReturnMineEffect(); });
            RegisterTween(returnTween);
        }

        private GameObject GetEffectFromPool(string poolName)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(poolName);
            return pool?.Get();
        }

        private void ReturnEffectToPool(string poolName, GameObject effect)
        {
            if (effect == null) return;

            var pool = ObjectPoolManager.Instance.GetGameObjectPool(poolName);
            pool?.Return(effect);
        }

        
        private Tween _spawnTween;
        private Tween _cleanupTween;
        
        /**
         * DOTween Sequence 在一帧里执行了大量 AppendCallback
         * 只创建 1 个 Tween，不会一次性创建 N 个 Tween，不卡顿
         **/
        private void CreateArrowRain(Vector3 centerPos, float skillRange, float duration, int arrowNum)
        {
            // 结束旧的生成
            _spawnTween?.Kill();
            _cleanupTween?.Kill();
        
            float elapsed = 0f;
            _isCasting = true;
        
            void SpawnNext()
            {
                elapsed += spawnInterval;
                if (!_isCasting || elapsed > duration)
                {
                    StartCleanup(centerPos);
                    return;
                }

                for (int i = 0; i < arrowNum; i++)
                {
                    SpawnSingleArrow(centerPos, skillRange);
                }
                
        
                // 每 spawnInterval 执行一次
                _spawnTween = DOVirtual.DelayedCall(spawnInterval, SpawnNext).SetUpdate(true);
            }
        
            // 立刻开始第一次
            SpawnNext();
        }
        
        private void StartCleanup(Vector3 centerPos)
        {
            if (!_isCasting) return;
        
            _cleanupTween = DOVirtual.DelayedCall(arrowLifeTime - 1f, () =>
            {
                if (!_isCasting) return;
        
                ReturnMineEffect();
                PlayEndEffect(centerPos);
                _isCasting = false;
            }).SetUpdate(true);
        }

        private void SpawnSingleArrow(Vector3 centerPos, float skillRange)
        {
            Vector2 randomPos = Random.insideUnitCircle * _continuousAtkSpeed * skillRange;
            Vector3 spawnPos = new Vector3(
                centerPos.x + randomPos.x * _continuousAtkSpeed,
                centerPos.y + randomPos.y,
                0
            );

            var arrowPool = ObjectPoolManager.Instance.GetGameObjectPool(arrowPoolName);
            if (arrowPool == null) return;

            GameObject arrow = arrowPool.Get();
            if (arrow == null) return;

            arrow.transform.position = spawnPos;
            arrow.transform.SetParent(GamePlayManager.Default.droppingFireController.transform);
            arrow.transform.localScale = Vector3.one * 0.65f;
            arrow.SetActive(true);

            var arrowBullet = arrow.transform.GetOrAddComponent<ArrowBullet>();
            if (arrowBullet != null)
            {
                arrowBullet.StartAttackSet(this, 0);
            }
            else
            {
                arrowBullet.StartAttackSet(this, 0);
            }

            var returnTween = DOVirtual.DelayedCall(arrowLifeTime, () =>
            {
                if (arrow != null && arrow.activeSelf)
                {
                    arrow.SetActive(false);
                    arrowPool.Return(arrow);
                }
            });
            RegisterTween(returnTween);
        }


        void StartSkillRandomShootBottom(Vector3 centerPos, float skillRange, float duration)
        {
            var arrowPool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{SkillRandomShootBottom}Loop");
            if (arrowPool == null) return;

            GameObject arrow = arrowPool.Get();
            if (arrow == null) return;

            arrow.transform.position = centerPos;
            arrow.transform.SetParent(GamePlayManager.Default.droppingFireController.transform);
            arrow.transform.localScale = Vector3.one * skillRange;
            arrow.SetActive(true);

            var returnTween = DOVirtual.DelayedCall(duration, () =>
            {
                _isCasting = false;
                _spawnTween?.Kill();
                StartCleanup(centerPos);
                
                if (arrow != null && arrow.activeSelf)
                {
                    arrow.SetActive(false);
                    arrowPool.Return(arrow);
                }
            });
            RegisterTween(returnTween);
        }

        private void ReturnMineEffect()
        {
            var minePool = ObjectPoolManager.Instance.GetGameObjectPool(MineEffectPool);
            minePool?.ReturnAll();
        }

        // 注册Tween以便统一管理
        private void RegisterTween(Tween tween)
        {
            if (tween != null && tween.IsActive())
            {
                _activeTweens.Add(tween);
                tween.OnComplete(() => _activeTweens.Remove(tween));
                tween.OnKill(() => _activeTweens.Remove(tween));
            }
        }

        // 清理所有Tween
        public void CleanupTweens()
        {
            // 先完成所有正在进行的动画
            foreach (var tween in _activeTweens.ToArray()) // 使用ToArray避免修改集合时的异常
            {
                if (tween != null && tween.IsActive())
                {
                    tween.Kill(); // 使用Kill而不是Complete，避免重复触发回调
                }
            }

            _activeTweens.Clear();

            // 确保主序列也被清理
            if (_mainSequence != null && _mainSequence.IsActive())
            {
                _mainSequence.Kill();
                _mainSequence = null;
            }
        }

        // 强制完成并清理所有动画
        public void ForceCompleteAndCleanup()
        {
            CleanupTweens();
            _isCasting = false;

            // 这里可以添加其他需要清理的资源
            ReturnMineEffect();

            // 清理对象池中的箭头
            var arrowPool = ObjectPoolManager.Instance.GetGameObjectPool(arrowPoolName);
            arrowPool?.ReturnAll();
        }

        // 添加OnDestroy清理
        private void OnDestroy()
        {
            ForceCompleteAndCleanup();
        }
    }
}