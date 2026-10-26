using System.Collections;
using UnityEngine;

public class EffectAutoRecycle : MonoBehaviour
{
    private QFramework.GenericObjectPool _pool;
    private float _lifetime = 0.5f;

    public void Init(QFramework.GenericObjectPool pool, float delay)
    {
        _pool = pool;
        _lifetime = delay;
        StartCoroutine(RecycleAfterDelay());
    }

    private IEnumerator RecycleAfterDelay()
    {
        yield return new WaitForSeconds(_lifetime);
        _pool?.Return(gameObject);
    }
}