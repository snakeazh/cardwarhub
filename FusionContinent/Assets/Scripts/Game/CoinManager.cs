using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CoinManager : MonoBehaviour
{

  //  private static CoinManager _instance;
  //  public Transform target;
  //  public float RecycleCoinTime = 5;
  //
  //
  //  public static CoinManager Instance
  //  {
  //      get
  //      {
  //
  //          if (_instance == null)
  //          {
  //              _instance = FindObjectOfType<CoinManager>();
  //
  //              if (_instance == null)
  //              {
  //                  GameObject obj = new GameObject("CoinManager");
  //                  _instance = obj.AddComponent<CoinManager>();
  //              }
  //          }
  //
  //          return _instance;
  //      }
  //  }
  //
  //
  //  public List<GameObject> coins = new List<GameObject>();
  //
  //
  //  private void Awake()
  //  {
  //
  //      if (_instance != null && _instance != this)
  //      {
  //          ResetCoin();
  //          DOTween.To(v => { }, 0, 0, 0.1f).onComplete += () =>
  //          {
  //              GameObject prefab = Resources.Load<GameObject>("Coin/coin");
  //              ObjectPoolManager.Instance.CreateGameObjectPool("coin", prefab, GameLand.Default.Bullet);
  //              GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPool("coin");
  //              for (int i = 0; i < 100; i++)
  //              {
  //                  pool.Get().transform.localScale = Vector3.zero;
  //              }
  //              DOTween.To(v => { }, 0, 0, 0.1f).onComplete += () =>
  //              {
  //                  pool.ReturnAll();
  //              };
  //          };
  //          Destroy(gameObject);
  //      }
  //      else
  //      {
  //
  //          _instance = this;
  //
  //      }
  //
  //      DontDestroyOnLoad(gameObject);
  //  }
  //  private void Start()
  //  {
  //      ResetCoin();
  //      DOTween.To(v => { }, 0, 0, 0.1f).onComplete += () =>
  //      {
  //          GameObject prefab = Resources.Load<GameObject>("Coin/coin");
  //          ObjectPoolManager.Instance.CreateGameObjectPool("coin", prefab, GameLand.Default.Bullet);
  //          GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPool("coin");
  //          for (int i = 0; i < 100; i++)
  //          {
  //              pool.Get().transform.localScale=Vector3.zero;
  //          }
  //          DOTween.To(v => { }, 0, 0, 0.1f).onComplete += () =>
  //          {
  //              pool.ReturnAll();
  //          };
  //      };
  //  }
  //  public void ResetCoin()
  //  {
  //      coins.Clear();
  //      InvokeRepeating("RecycleCoin", 0, RecycleCoinTime);
  //  }
  //  public void RecycleCoin()
  //  {
  //      if (coins.Count > 0)
  //      {
  //          target = GameLand.Default.GoldText.transform.parent.transform;
  //          StartCoroutine(GoCoinbag());
  //      }
  //  }
  //  private IEnumerator DisableAfterDelay(GenericObjectPool pool, GameObject damageObject, float delay)
  //  {
  //      // 等待指定的时间
  //      yield return new WaitForSeconds(delay);
  //      GamePlayManager.Default.IncGameGold(damageObject.GetComponent<Coin>().coin);
  //      damageObject.GetComponent<Coin>().coin = 0;
  //      // 检查对象是否仍然有效
  //      if (damageObject != null)
  //      {
  //          pool.Return(damageObject);
  //      }
  //  }
  //  IEnumerator GoCoinbag()
  //  {
  //      yield return new WaitForSeconds(0.1f);
  //      List<GameObject> coinsToRemove = new List<GameObject>();
  //
  //
  //      var initialCoins = new List<GameObject>(CoinManager.Instance.coins);
  //
  //      foreach (var coin in initialCoins)
  //      {
  //          if (coin == null) break;
  //          Sequence sequence = DOTween.Sequence();
  //
  //          sequence.Append(coin.transform.DOMove(target.position, 1f).OnComplete(() =>
  //          {
  //
  //              coinsToRemove.Add(coin);
  //
  //              if (coinsToRemove.Count == initialCoins.Count)
  //              {
  //                  foreach (var item in coinsToRemove)
  //                  {
  //                      CoinManager.Instance.coins.Remove(item);
  //                      StartCoroutine(DisableAfterDelay(ObjectPoolManager.Instance.GetGameObjectPool("coin"), item.gameObject, 0.0001f));
  //                  }
  //                  coinsToRemove.Clear();
  //                  AudioKit.PlaySound("resources://Audio/fx_goldGet");
  //                  GameObject coinbag = Instantiate(Resources.Load<GameObject>("Coin/jinbi_huode_01"), target.position, Quaternion.identity);
  //                  coinbag.transform.SetParent(target);
  //                  Destroy(coinbag, 2);
  //              }
  //          }).OnKill(() =>
  //          {
  //              DOTween.Kill(sequence);
  //          }));
  //      }
  //  }
}
