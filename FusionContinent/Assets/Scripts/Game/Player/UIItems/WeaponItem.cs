using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UIItems
{
    /// <summary>
    /// 武器列表项：控制 WeaponItem 预制体的信息显示。
    /// </summary>
    public class WeaponItem : MonoBehaviour
    {
        [Tooltip("武器品质背景图")]
        [SerializeField] private Image qualityFrame;
        [Tooltip("武器大图")]
        [SerializeField] private Image weaponIcon;
        [Tooltip("选中框")]
        [SerializeField] private GameObject isSelected;
        [Tooltip("星级容器（HorizontalLayoutGroup）")]
        [SerializeField] private Transform weaponStar;
        [Tooltip("武器名称")]
        [SerializeField] private TextMeshProUGUI weaponName;
        [Tooltip("未解锁遮罩（含锁图标的父物体）")]
        [SerializeField] private GameObject lockMask;
        [Tooltip("已穿戴标签")]
        [SerializeField] private GameObject isWear;

        private const string StarActiveName = "IsActive";

        private readonly List<GameObject> _starActiveNodes = new List<GameObject>();

        private void Awake()
        {
            CacheNodes();
        }

        public void SetData(
            string name,
            string icon,
            QualityType quality,
            int starCount,
            bool isUnlocked,
            bool isEquipped,
            bool isSelected,
            int maxStarCount = 0)
        {
            gameObject.SetActive(true);

            if (weaponName != null)
            {
                weaponName.text = name;
            }

            if (weaponIcon != null)
            {
                weaponIcon.enabled = !string.IsNullOrEmpty(icon);
                if (weaponIcon.enabled)
                {
                    SpriteHelper.TryLoadTextureAsync(icon, weaponIcon, false, "UIWeaponTextures");
                }
            }

            if (qualityFrame != null)
            {
                qualityFrame.sprite = Global.WeaponQualityFrameAtlas.GetSpriteExt(quality.ToString());
            }

            var requiredStarCount = maxStarCount > 0 ? maxStarCount : starCount;
            RefreshStars(starCount, requiredStarCount);
            SetActive(this.isSelected, isSelected);
            SetActive(isWear, isEquipped);
            SetActive(lockMask, !isUnlocked);
        }

        private void RefreshStars(int starCount, int maxStarCount)
        {
            if (weaponStar == null)
            {
                return;
            }

            EnsureStarNodes(maxStarCount);
            starCount = Mathf.Clamp(starCount, 0, _starActiveNodes.Count);
            for (int i = 0; i < _starActiveNodes.Count; i++)
            {
                _starActiveNodes[i].SetActive(i < starCount);
            }
        }

        private void EnsureStarNodes(int requiredCount = 0)
        {
            if (_starActiveNodes.Count == 0)
            {
                for (int i = 0; i < weaponStar.childCount; i++)
                {
                    var star = weaponStar.GetChild(i);
                    var active = star.Find(StarActiveName);
                    if (active != null)
                    {
                        _starActiveNodes.Add(active.gameObject);
                    }
                }
            }

            if (requiredCount <= _starActiveNodes.Count || weaponStar.childCount == 0)
            {
                return;
            }

            var template = weaponStar.GetChild(0);
            while (_starActiveNodes.Count < requiredCount)
            {
                var star = Instantiate(template, weaponStar);
                var active = star.Find(StarActiveName);
                if (active == null)
                {
                    break;
                }

                active.gameObject.SetActive(false);
                _starActiveNodes.Add(active.gameObject);
            }
        }

        private static void SetActive(GameObject go, bool value)
        {
            if (go != null && go.activeSelf != value)
            {
                go.SetActive(value);
            }
        }

        private void CacheNodes()
        {
            if (weaponIcon == null) weaponIcon = transform.Find("WeaponIcon")?.GetComponent<Image>();
            if (isSelected == null) isSelected = transform.Find("IsSelected")?.gameObject;
            if (weaponStar == null) weaponStar = transform.Find("WeaponStar");
            if (weaponName == null) weaponName = transform.Find("WeaponName")?.GetComponent<TextMeshProUGUI>();
            if (lockMask == null) lockMask = transform.Find("Mask")?.gameObject;
            if (isWear == null) isWear = transform.Find("IsWear")?.gameObject;
        }
    }
}
