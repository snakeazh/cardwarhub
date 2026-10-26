using UnityEngine;

public interface IPoolable
{
    void OnSpawn();
    void OnRecycle();
}
