using System.Linq;
using DG.Tweening;
using FusionContinent;
using UnityEngine;

public class MonsterSortManager : MonoBehaviour
{
    public static MonsterSortManager Instance { get; private set; }

    private const int BaseOrder = 1;
    private const float SortGateDuration = 5f;

    private bool _isSortGateActive;
    private float _sortGateTimer;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!_isSortGateActive)
        {
            return;
        }

        _sortGateTimer += Time.deltaTime;
        if (_sortGateTimer >= SortGateDuration)
        {
            _sortGateTimer = 0f;
            _isSortGateActive = false;
        }
    }

    public void ResetSortGate()
    {
        _isSortGateActive = false;
        _sortGateTimer = 0f;
    }

    public void RequestSort(float delay, MonsterCard requester = null)
    {
        DOTween.To(_ => { }, 0f, 0f, delay).OnComplete(() =>
        {
            if (requester != null && !requester.gameObject.activeInHierarchy)
            {
                return;
            }

            SortAll();
        });
    }

    public void SortAll()
    {
        if (_isSortGateActive)
        {
            return;
        }

        var monsters = MonsterManager.Instance.enemies
            .Where(card => card != null && card.gameObject.activeInHierarchy)
            .ToArray();

        if (monsters.Length == 0)
        {
            LogTool.LogWarning("[SortY] 没有可排序的怪物！");
            return;
        }

        var sorted = monsters
            .OrderBy(card => -card.SortTransform.position.y)
            .ToArray();

        for (int i = 0; i < sorted.Length; i++)
        {
            var monsterCard = sorted[i];
            monsterCard.mySiblingIndex = i;

            if (monsterCard.Renderers == null)
            {
                LogTool.LogWarning($"[SortY] 未找到 Renderer: {monsterCard.name}");
                continue;
            }

            int finalOrder = BaseOrder + i;
            for (int r = 0; r < monsterCard.Renderers.Length; r++)
            {
                var renderer = monsterCard.Renderers[r];
                if (renderer == null)
                {
                    LogTool.LogWarning($"[SortY] 未找到 MeshRenderer: {monsterCard.name}");
                    continue;
                }

                renderer.sortingOrder = finalOrder;
            }
        }

        _isSortGateActive = true;
    }
}
