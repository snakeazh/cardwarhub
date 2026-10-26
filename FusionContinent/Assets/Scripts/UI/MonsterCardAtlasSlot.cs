using System;
using Config;
using FusionContinent.Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
    public class MonsterCardAtlasSlotData : UIPanelData
    {
        public string CardName;
        public Sprite IconSprite;
        public MonsterQuality Quality;
        public bool IsUnlock;
        public bool IsBoss;
        public Action OnClick;
        /// <summary>怪物类型标签（飞行怪/远程怪等），来自 MonsterBattleConfig.Label，空则不显示标签</summary>
        public string Label;
    }
	
    public partial class MonsterCardAtlasSlot : UIPanel
    {
        [SerializeField]
        public Image IsBoss;
		
        private MonsterCardAtlasSlotData _data;
        protected override void OnInit(IUIData uiData = null)
        {
            CardBtn.onClick.AddListener(() =>
            {
                if (_data?.OnClick == null) return;
                _data.OnClick.Invoke();
            });
        }


        public void SetData(MonsterCardAtlasSlotData data)
        {
            _data = data;

            if (data.Quality == MonsterQuality.Boss)
            {
                // SpriteHelper.TryLoadTexture($"{data.Quality}MonsterCard", Bg);
                // SpriteHelper.TryLoadTexture($"BossMonsterCard", Bg);
                IsBoss.gameObject.SetActive(true);
            }
            else
            {
                // SpriteHelper.TryLoadTexture($"NormalMonsterCard", Bg);
                IsBoss.gameObject.SetActive(false);
            }
			
            Icon.sprite = data.IconSprite;
            SetNativeSizeFitParent(Icon);
            Icon.transform.localScale = Vector3.one * 0.9f;

            CardName.text = data.CardName;
            if (!data.IsUnlock)
            {
                // CardName.text = "???";
                // Icon.color = new Color(Icon.color.r,Icon.color.g,Icon.color.b,0.4f);
                Lock.gameObject.SetActive(true);
                Icon.gameObject.SetActive(false);
                CardName.gameObject.SetActive(false);
            }
            else
            {
                // Icon.color = new Color(Icon.color.r,Icon.color.g,Icon.color.b,1f);
                Lock.gameObject.SetActive(false);
                Icon.gameObject.SetActive(true);
                CardName.gameObject.SetActive(true);
            }

            // 类型标签：Label 既是图集 sprite 索引，也决定显示文字；为空则隐藏整组
            var hasLabel = !string.IsNullOrEmpty(data.Label);
            Tag.gameObject.SetActive(hasLabel);
            if (hasLabel)
            {
                Tag.sprite = Global.MonsterCardSlotTagAtlas.GetSpriteExt(data.Label);
                TagText.text = GetTagDisplayText(data.Label);
            }
            // Lock.gameObject.SetActive(!data.IsUnlock);

            // 名称上下渐变：上方统一白色，下方按解锁状态与品质区分
            // Color topColor = new Color(1f, 1f, 1f);
            // Color bottomColor;
            // if (!data.IsUnlock)
            // {
            //     // 未解锁（???）：#808080
            //     bottomColor = HexToColor(0x80, 0x80, 0x80);
            // }
            // else if (data.Quality == MonsterQuality.Boss)
            // {
            //     // Boss：#EAA000
            //     bottomColor = HexToColor(0xEA, 0xA0, 0x00);
            // }
            // else
            // {
            //     // 精英/普通：#EEC2FF
            //     bottomColor = HexToColor(0xEE, 0xC2, 0xFF);
            // }
            // ApplyNameGradient(topColor, bottomColor);
        }

        /// <summary>对 CardName 应用上下双色渐变（上=顶色，下=底色）</summary>
        private void ApplyNameGradient(Color topColor, Color bottomColor)
        {
            CardName.enableVertexGradient = true;
            CardName.colorGradient = new TMPro.VertexGradient(topColor, topColor, bottomColor, bottomColor);
        }

        private static Color HexToColor(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f);
        }

        /// <summary>标签 Label（图集索引）→ 显示文字映射，新增标签时在此扩展</summary>
        private static string GetTagDisplayText(string label)
        {
            switch (label)
            {
                case "FlyTag": return "飞行怪";
                case "RemoteTag": return "远程怪";
                default: return label;
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }
		
        protected override void OnShow()
        {
        }
		
        protected override void OnHide()
        {
        }
		
        protected override void OnClose()
        {
        }
		
        public static void SetNativeSizeFitParent(Image image)
        {
            if (image.sprite == null) return;

            RectTransform rt = image.rectTransform;
            RectTransform parent = rt.parent as RectTransform;

            Vector2 spriteSize = image.sprite.rect.size;
            Vector2 parentSize = parent.rect.size;

            float scale = Mathf.Min(
                parentSize.x / spriteSize.x,
                parentSize.y / spriteSize.y
            );

            rt.sizeDelta = spriteSize * scale;
        }
		
    }
}