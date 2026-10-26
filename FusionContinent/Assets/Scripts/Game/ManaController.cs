using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using DG.Tweening;
using UnityEngine.UI;
using FusionContinent;

public class ManaController : MonoSingleton<ManaController>
{
    // private GenericObjectPool _pool;
    //
    // [Header("回收目标")]
    // public Transform target;
    //
    // [Header("物体大小")]
    // public float localScale = 0.7f;
    //
    // [Header("随机角度")]
    // public float randomAngle = 2f;
    //
    // [Header("随机力度")]
    // public float randomForce = 2f;
    //
    // [Header("掉落速度")]
    // public float fallDuration = 0.2f;
    //
    // [Header("等待时长")]
    // public float waitDuration = 0.5f;
    //
    // [Header("回收速度")]
    // public float recycleDuration = 0.2f;
    //
    // void Start()
    // {
    // }
    //
    // public void CreateMana(int count, Vector3 pos)
    // {
    //     for (int i = 0; i < count; i++)
    //         StartCoroutine(CreateWait(pos, 1));
    // }
    //
    // IEnumerator CreateWait(Vector3 pos, int addNum)
    // {
    //     yield return new WaitForSeconds(0.2f);
    //     
    //     _pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("GoldCoin");
    //     if (_pool != null)
    //     {
    //         float angle = Random.Range(0f, randomAngle * Mathf.PI); // 随机角度
    //         float distance = Random.Range(0f, randomForce); // 随机力度
    //
    //         float x = pos.x + distance * Mathf.Cos(angle); // 随机x
    //         float y = pos.y + distance * Mathf.Sin(angle); // 随机y
    //
    //         GameObject mana = _pool.Get();
    //         mana.SetActive(true);
    //         mana.transform.SetParent(GameLand.Default?.Bullet);
    //         mana.transform.position = pos;
    //         // mana.transform.localScale = Vector3.one * Random.Range(0.2f, 0.4f);
    //         mana.transform.localScale = Vector3.one * localScale;
    //         mana.transform.rotation= Quaternion.Euler(new Vector3(0,0, Random.Range(-180f, 180f)));
    //         
    //         mana.transform.DOMove(new Vector3(x, y, 0f), fallDuration).SetEase(Ease.Linear).OnComplete(() =>
    //         {
    //             DOTween.To(v => { }, 0, 0, waitDuration).SetEase(Ease.Linear).onComplete += () =>
    //             {
    //                 mana.transform.DOMove(target.position, recycleDuration).SetEase(Ease.Linear).OnComplete(() =>
    //                 {
    //                     Global.GameGold.Value += addNum;
    //                     // ManaShowController.Instance.SetManaFragment(Global.GameManaFragment.Value);
    //                     _pool.Return(mana);
    //                 });
    //             };
    //         });
    //     }
    // }

}
