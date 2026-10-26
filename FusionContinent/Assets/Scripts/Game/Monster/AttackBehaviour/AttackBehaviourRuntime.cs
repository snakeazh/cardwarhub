using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Monster.AttackBehaviour
{
    /// <summary>
    /// 攻击行为运行时资源跟踪，OnCleanup 时统一释放。
    /// </summary>
    public sealed class AttackBehaviourRuntime
    {
        private readonly List<Tween> _tweens = new();
        private readonly List<Coroutine> _coroutines = new();
        private MonoBehaviour _coroutineHost;

        public void BindCoroutineHost(MonsterCard owner)
        {
            _coroutineHost = owner;
        }

        public void RegisterTween(Tween tween)
        {
            if (tween == null)
            {
                return;
            }

            _tweens.Add(tween);
        }

        public Coroutine RunCoroutine(IEnumerator routine)
        {
            if (_coroutineHost == null || routine == null)
            {
                return null;
            }

            var coroutine = _coroutineHost.StartCoroutine(routine);
            _coroutines.Add(coroutine);
            return coroutine;
        }

        public void StopAll()
        {
            for (int i = 0; i < _tweens.Count; i++)
            {
                if (_tweens[i] != null && _tweens[i].IsActive())
                {
                    _tweens[i].Kill();
                }
            }

            _tweens.Clear();

            if (_coroutineHost != null)
            {
                for (int i = 0; i < _coroutines.Count; i++)
                {
                    if (_coroutines[i] != null)
                    {
                        _coroutineHost.StopCoroutine(_coroutines[i]);
                    }
                }
            }

            _coroutines.Clear();
        }
    }
}
