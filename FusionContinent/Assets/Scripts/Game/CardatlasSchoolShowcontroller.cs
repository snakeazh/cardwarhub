using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class CardatlasSchoolShowcontroller : MonoBehaviour
{
    public Image icon;

    public TMP_Text schooldesc;

    public TMP_Text schoolname;

    public SpriteAtlas cardicon;

    public TMP_Text schooltitlename;
    public GameObject schoolState;
    public Transform schoolStateparent;

    public GameObject school;
    public Transform schoolparent;
    private TbSchoolConfig _schoolCfg;
    private TbSchoolCardConfig _schoolCard;

    // Start is called before the first frame update

    public void SetSchool(int schoolid, Sprite schoolicon)
    {
        _schoolCard = DataConfigManager.Tables.TbSchoolCardConfig;
        _schoolCfg = DataConfigManager.Tables.TbSchoolConfig;
        var schoolCardCfg = _schoolCard.GetOrDefault(schoolid);
        if (schoolCardCfg == null) return;

        icon.sprite = schoolicon;
        schoolname.text = schoolCardCfg.Name;
        
        // 描述
        var desc = schoolCardCfg.Desc;
        var starCfg = schoolCardCfg.StarList.First();
        for (int i = 1; i <= starCfg.Attributes.Count; i++)
        {
            var attr = starCfg.Attributes[i - 1];
            var val = attr.ValueType == ValueType.Percentage ? attr.Value * 100 : attr.Value;
            if (attr.Type is AttributeType.SoldierCriticalRate or AttributeType.SoldierCriticalDamage) {
                val = attr.Value * 100;
            }
            desc = desc.Replace("{Attribute" + i + "}", $"{val}");
        }

        for (int i = icon.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(icon.transform.GetChild(i).gameObject);
        }
        GameObject schoolcardobj = Global.ResLoader.LoadSync<GameObject>("CardSlot").Instantiate();
        schoolcardobj.transform.SetParent(icon.transform);
        schoolcardobj.transform.localPosition = Vector3.zero;
        schoolcardobj.transform.localScale = Vector3.one;
        schoolcardobj.GetComponent<CardSlot>().SetData(new CardSlotData()
        {
            Icon = schoolicon,
            NameText = schoolCardCfg.Name,
            QualityType = schoolCardCfg.Quality,
            Desc = schoolCardCfg.Desc,
        });
        if (starCfg.Param1 > 0) desc = desc.Replace("{Param1}", $"{starCfg.Param1}");
        if (starCfg.Param2 > 0) desc = desc.Replace("{Param2}", $"{starCfg.Param2}");
        schooldesc.text = desc;

        // 清除旧school图标（保留第0个）
        for (int i = schoolparent.childCount - 1; i >= 1; i--)
        {
            Destroy(schoolparent.GetChild(i).gameObject);
        }

        List<Transform> iconList = new List<Transform>();

        for (int i = 0; i < schoolCardCfg.Schools.Count; i++)
        {
            int currentIndex = i;
            int schoolConfigId = schoolCardCfg.Schools[currentIndex];
            var schoolCfgItem = _schoolCfg.GetOrDefault(schoolConfigId);
            if (schoolCfgItem == null) continue;

            GameObject schoolObj = Instantiate(school, schoolparent);
            schoolObj.SetActive(true);

            var iconTransform = schoolObj.transform.Find("icon");
            var iconImage = iconTransform.GetComponent<Image>();
            iconImage.sprite = cardicon.GetSprite(schoolCfgItem.Icon2);

            iconTransform.localScale = new Vector3(0.8f, 0.8f, 1); // 初始缩放
            iconList.Add(iconTransform);

            var iconButton = schoolObj.transform.Find("icon").GetComponent<Button>();
            iconButton.onClick.RemoveAllListeners();

            // 局部变量防闭包引用错误
            var localCfg = schoolCfgItem;
            var localIconTransform = iconTransform;

            iconButton.onClick.AddListener(() => { SelectSchool(localCfg, localIconTransform, iconList); });
        }

        // 默认选择第一个（注意 childCount > 1）
        if (iconList.Count > 0)
        {
            var defaultCfg = _schoolCfg.GetOrDefault(schoolCardCfg.Schools[0]);
            SelectSchool(defaultCfg, iconList[0], iconList);
        }
    }

    private void SelectSchool(SchoolConfig schoolCfgItem, Transform iconTransform, List<Transform> allIcons)
    {
        if (schoolCfgItem == null || iconTransform == null) return;

        schooltitlename.text = schoolCfgItem.Name;

        // 缩回所有图标
        foreach (var otherIcon in allIcons)
        {
            if (otherIcon != iconTransform)
            {
                otherIcon.DOScale(new Vector3(0.8f, 0.8f, 1), 0.2f).SetEase(Ease.OutQuad);
            }
        }

        // 当前图标放大
        iconTransform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);

        // 清除旧状态（保留前两个）
        for (int j = schoolStateparent.childCount - 1; j >= 2; j--)
        {
            Destroy(schoolStateparent.GetChild(j).gameObject);
        }

        // 添加新状态
        foreach (var prop in schoolCfgItem.CardCountList)
        {
            GameObject schoolStateObj = Instantiate(schoolState, schoolStateparent);
            schoolStateObj.SetActive(true);
            schoolStateObj.transform.Find("Skilllv").GetComponent<TMP_Text>().text = $"（{prop.CardCount}）";
            schoolStateObj.transform.Find("SkillStatedesc").GetComponent<TMP_Text>().text = $"（{prop.Desc}）";
            UIHelper.SafeRefreshLayout(schoolStateObj.transform.GetComponent<RectTransform>() as RectTransform);
            UIHelper.SafeRefreshLayout(schoolStateObj.transform.parent.GetComponent<RectTransform>() as RectTransform);
        }
    }
}

public static class UIHelper
{
    public static void SafeRefreshLayout(RectTransform rt)
    {
        DOTween.Sequence()
            .AppendInterval(0.01f)
            .AppendCallback(() => { LayoutRebuilder.ForceRebuildLayoutImmediate(rt); });
    }
}