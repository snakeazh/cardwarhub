using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DropRewardItem : MonoBehaviour
{
    public RewardEntity rewardEntity;
    public GameObject BadgeGameObject;
    public TextMeshProUGUI ProbabilityNumText;

    public void SetData(Config.RewardAndNumAndPro rewardAndNumAndPro)
    {
        rewardEntity.SetData(new FusionContinent.Model.RewardItem(rewardAndNumAndPro.Id, rewardAndNumAndPro.Num));
        ProbabilityNumText.text = $"{rewardAndNumAndPro.Pro}%";
    }
}
