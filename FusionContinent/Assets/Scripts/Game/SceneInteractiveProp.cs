using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;

public class SceneInteractiveProp : MonoBehaviour
{
    private TbGameBattleConst _gameBattleConst ;
    public BattleSceneBodyType  battleSceneBodyType;
    private string effectname;
    private void Awake()
    {
        _gameBattleConst = DataConfigManager.Tables.TbGameBattleConst;
    }

    private void Start()
    {
        foreach (var effect in  _gameBattleConst.BattleSceneBodyEffects)
        {
            if (effect.Type == battleSceneBodyType)
            {
                effectname = effect.EffectName;
            }
        }
    }

    // Start is called before the first frame updat
    private bool _isOnCooldown = false;

    public void CreateEffectHit(Vector3 pos)
    {
        // 如果还在冷却，就直接 return
        if (_isOnCooldown) return;

        _isOnCooldown = true;

        // 实例化特效
        GameObject hitEffect = GamePlayManager.Default._resLoader
            .Get<GameObject>(effectname)
            .Instantiate();

        hitEffect.transform.SetParent(GameLand.Default.Bullet);
        hitEffect.transform.localScale = Vector3.one * 100;
        hitEffect.transform.position = pos;
        Destroy(hitEffect, 2f);

        // 0.2s 后解除冷却
        StartCoroutine(ResetCooldown());
    }

    private IEnumerator ResetCooldown()
    {
        yield return new WaitForSeconds(0.2f);
        _isOnCooldown = false;
    }

}
