using System.Collections.Generic;
using FusionContinent;
using UnityEngine;

/// <summary>
/// 波次配置初始化模块 - 计算刷怪配置总数
/// </summary>
public class SceneInitWaveConfig : SceneMapBase
{
    public override string ModuleName => "波次配置初始化";

    protected override void OnStartLoad()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        if (gpm._monsterGenCfg == null)
        {
            ErrorMessage = "MonsterGenCfg is null";
            return;
        }

        // 计算刷怪配置总数
        gpm._monsterGenDayUpperLimitMap.Clear();
        foreach (var dayCfg in gpm._monsterGenCfg.StageList)
        {
            int monsterGenDayUpperLimit = 0;
            foreach (var cfg in dayCfg.WaveList)
            {
                foreach (var v in cfg.GenCards)
                {
                    monsterGenDayUpperLimit += v.Num;
                }
            }

            gpm._monsterGenDayUpperLimitMap.Add(dayCfg.Day, monsterGenDayUpperLimit);
        }

        progress = 1.0f;
    }
}
