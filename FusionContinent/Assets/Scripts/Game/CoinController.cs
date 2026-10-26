using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using QFramework;
using FusionContinent;

public class CoinController : MonoBehaviour
{
    public float radius; // Բ�İ뾶
    public Transform target;
    public GameObject prefab; // Ҫʵ������Ԥ����
    public int coin;
    public Vector3 pos;
    public GenericObjectPool pool;
    // Start is called before the first frame update
    void Start()
    {
        pool = ObjectPoolManager.Instance.GetGameObjectPool("coin");
    }
    public void CreateCoin(float dealy)
    {
        pos = transform.position;
        StartCoroutine(wait(dealy));
    }

    IEnumerator wait(float delay)
    {
        yield return new WaitForSeconds(delay);  // �ȴ���Ч������
        float angle = Random.Range(0f, 2f * Mathf.PI); // ��������Ƕ�
        float distance = Random.Range(0f, radius); // ����һ����0��radius���������

        float x = pos.x + distance * Mathf.Cos(angle); // ����x����
        float y = pos.y + distance * Mathf.Sin(angle); // ����y����
        if (pool != null)
        {
            GameObject obj = pool.Get();
            obj.transform.position = pos;
            //obj.transform.SetParent(transform.parent.parent);
            obj.transform.localScale = Vector3.zero;
            //CoinManager.Instance.coins.Add(obj);
            Sequence sequence = DOTween.Sequence();
            sequence.Append(obj.transform.DOScale(Vector3.one * 0.5f, 0.5f));
            sequence.Join(obj.transform.DOMove(new Vector3(x, y, 0), 0.5f).OnComplete(() => { obj.transform.SetAsFirstSibling(); }));
           //if (!isSoundPlaying)  // �����ǰû����Ч�ڲ���
           //{
           //    isSoundPlaying = true;  // �����Ч�Ѿ�����
           //    AudioKit.PlaySound("resources://Audio/fx_goldGet");
           //
           //    // ������Ч�Ĳ���ʱ��Ϊ1�룬��Ч��������ٽ���־λ����
           //    StartCoroutine(ResetSoundFlag(1f));  // 1�������
           //}
        }
    }
}
