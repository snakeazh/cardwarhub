using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreSituationPaneal : MonoBehaviour
{
    private Button quitbtn;
    private TMP_Text myscore;
    private Transform levelscorecontent;
    public GameObject scoreobj;
   // public LevelScoreSituationTable levelScoreSituationTable;
    // Start is called before the first frame update
    private void Awake()
    {
        quitbtn = transform.Find("quitPanel").GetComponent<Button>();
        myscore = transform.Find("Down/score").GetComponent<TMP_Text>();
        levelscorecontent = transform.Find("Scroll View/Viewport/Content");
    }
    void Start()
    {
        quitbtn.onClick.AddListener(() =>
        {
            this.gameObject.SetActive(false);
        });
    }
    private void OnEnable()
    {
        myscore.text = Global.PlayerBattle.Score.ToString();
        Init();
    }

    private void Init()
    {
        //�ѹؿ����з���
        List<StageBattleConfig> stageBattleConfigs = DataConfigManager.Tables.TbStageBattleConfig.DataList;
        foreach (var a in stageBattleConfigs)
        {
            if (!Global.PlayerStages.ContainsKey(a.Id) || Global.PlayerStages[a.Id].Score == 0) continue;
            int i = 0;
            for (;i< levelscorecontent.childCount;i++)
            {
               if( levelscorecontent.GetChild(i).transform.Find("�ؿ�����").GetComponent<TMP_Text>().text== DataConfigManager.Tables.TbStageBattleConfig.Get(Global.PlayerStages[a.Id].StageId).StageName)
                {
                    levelscorecontent.GetChild(i).transform.Find("ʱ��").GetComponent<TMP_Text>().text = FormatTime(Global.PlayerStages[a.Id].ClearStageTime);
                    levelscorecontent.GetChild(i).transform.Find("����").GetComponent<TMP_Text>().text = Global.PlayerStages[a.Id].Score.ToString();
                    break;
                }
            }
            if(i>= levelscorecontent.childCount)
            {
                GameObject obj = Instantiate(scoreobj);
                obj.transform.SetParent(levelscorecontent);
                obj.SetActive(true);
                obj.transform.Find("�ؿ�����").GetComponent<TMP_Text>().text = DataConfigManager.Tables.TbStageBattleConfig.Get(Global.PlayerStages[a.Id].StageId).StageName;
                obj.transform.Find("ʱ��").GetComponent<TMP_Text>().text = FormatTime(Global.PlayerStages[a.Id].ClearStageTime);
                obj.transform.Find("����").GetComponent<TMP_Text>().text = Global.PlayerStages[a.Id].Score.ToString();
                //LogTool.LogError("woshengcl");
            }
        }
    }
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);

        // ʹ���ַ�����ʽ��
        string formattedTime = string.Format("{0:D2}:{1:D2}", minutes, seconds);
        return formattedTime;
    }
}
