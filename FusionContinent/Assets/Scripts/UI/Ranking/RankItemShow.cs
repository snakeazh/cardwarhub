using TMPro;
using UnityEngine;

public class RankItemData
{
    /// <summary>名次，<=0 表示未上榜</summary>
    public int rankNum;
    public PlayerHeadData headData;
    public string playerName;
    public string detailData;
}

public class RankItemShow : MonoBehaviour
{
    public TMP_Text rankNumText;
    public PlayerHead playerHead;
    public TMP_Text playerNameText;
    public TMP_Text detailData;
    public TMP_Text notInRankText;

    public void SetData(RankItemData data)
    {
        var ranked = data.rankNum > 0;

        if (rankNumText != null)
        {
            rankNumText.gameObject.SetActive(ranked);
            if (ranked)
            {
                rankNumText.text = data.rankNum.ToString();
            }
        }

        if (notInRankText != null)
        {
            notInRankText.gameObject.SetActive(!ranked);
        }

        if (playerHead != null && data.headData != null)
        {
            playerHead.SetData(data.headData);
        }

        if (playerNameText != null)
        {
            playerNameText.text = data.playerName;
        }

        if (detailData != null)
        {
            detailData.text = data.detailData;
        }
    }
}
