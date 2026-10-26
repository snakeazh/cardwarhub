using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace FusionContinent
{
    
public class GameCardStay : MonoBehaviour
{
    private GameObject cardShadow;
    private AssetHandle handle;
    private void Awake()
    {
        handle = YooAssets.LoadAssetAsync<GameObject>("Finger2");
        handle.Completed += OnCompleted;
        Global.Publisher.Subscribe(MessagePublisher.CHANGE_GAME_CARD_COOLING,OnCoolingDone);
        
        Global.Publisher.Subscribe(MessagePublisher.CHANGE_GAME_CARD_RELEASE_COMPEPLETE,OnReleaseComplete);
        Global.Publisher.Subscribe( MessagePublisher.CHANGE_GAME_SETTLE,OnGameSettle);


        waitCo = StartCoroutine(WaitCDTime());

    }

    private Coroutine waitCo;

    private void OnGameSettle(object obj)
    {
        Clear();
    }

    private void OnDestroy()
    {
        Clear();
        Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GAME_CARD_COOLING,OnCoolingDone);
        Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GAME_CARD_RELEASE_COMPEPLETE,OnReleaseComplete);
        cardPos.Clear();
        handle?.Release();
        handle = null;
        if (waitCo != null)
        {
            StopCoroutine(waitCo);
            waitCo = null;
        }
        Object.Destroy(cardShadow);
    }

    private void OnReleaseComplete(object obj)
    {
        int skillId = (int)obj;
        Clear();
    }

    public IEnumerator WaitCDTime()
    {
        while (true)
        {
            CDTime -= Time.deltaTime*Global.SpeedMultiplier.Value;
            yield return null;
        }
    }

    Dictionary<int ,RectTransform> cardPos = new Dictionary<int ,RectTransform>();
    public void AddSkillById(int skillId,RectTransform target)
    {
        // RectTransformUtility.
        // var localPos = this.GetComponent<RectTransform>().InverseTransformPoint(worldPos);
        cardPos.TryAdd(skillId,target);
    }
    
    [SerializeField]
    private int displaySkillId = 0;
    
    
    // Stack<int> skillStack = new Stack<int>();
    
    private float CDTime;
    private void OnCoolingDone(object obj)
    {
        if (displaySkillId != 0)
        {
            return;
        }

        if (cardShadow == null)
        {
            return;
        }

        if (CDTime > 0)
        {
            return;
        }
        var skillId = (int)obj;
        // if (cardPos.ContainsKey(skillId))
        {
            displaySkillId =  skillId;
            stayCoroutine = StartCoroutine(CheckDone());
        }
    }
    
    Coroutine stayCoroutine;

    public void Clear()
    {
        CDTime = DataConfigManager.Tables.TbGameBattleConst.ShadowTriggerCDTime;
        if (stayCoroutine != null)
        {
            StopCoroutine(stayCoroutine);
            stayCoroutine = null;            
        }

        if (this.cardShadow)
        {
            this.cardShadow.SetActive(false);    
        }
        
        displaySkillId = 0;
    }

    public IEnumerator CheckDone()
    {
        float maxTime = DataConfigManager.Tables.TbGameBattleConst.ShadowTriggerTime;
        float times = maxTime;
        ///检查是否到时间
        while (times >= 0)
        {
            if (!GamePlayManager.Default.IsPaused())
            {
                times-= Time.deltaTime*Global.SpeedMultiplier.Value;
            }
            
            yield return null;
        }
        // yield return new WaitForSeconds(1f);
        if (!cardPos.ContainsKey(displaySkillId))
        {
            yield break;
        }
        
        this.cardShadow.transform.localPosition = this.GetComponent<RectTransform>()
            .InverseTransformPoint(cardPos[displaySkillId].position);
        this.cardShadow.SetActive(true);
        
        
        
        GamePlayManager.Default.TriggerGuide(GuideType.SkillGuide);
    }
    
    

    private void OnEnable()
    {
    }

    private void OnCompleted(AssetHandle obj)
    {
        cardShadow = GameObject.Instantiate(obj.GetAssetObject<GameObject>(), this.transform, true);
        // throw new NotImplementedException();
        cardShadow.gameObject.SetActive(false);
        cardShadow.transform.localScale = Vector3.one;
    }
    
    
}
}
