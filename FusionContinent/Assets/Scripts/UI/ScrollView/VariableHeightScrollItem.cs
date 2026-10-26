using TMPro;
using FusionContinent.Model;
using UnityEngine;

namespace ScrollView
{
    /// <summary>
    /// 不等高列表项基类。通过 TMP 文本首选尺寸计算行高，不触发布局重建。
    /// </summary>
    public abstract class VariableHeightScrollItem : ScrollItem, IVariableHeightScrollItem
    {
        [Tooltip("参与高度测量的根节点，为空则使用自身")]
        public RectTransform MeasureTarget;

        [Tooltip("用于计算高度的文本，为空则从 MeasureTarget 子节点查找")]
        public TMP_Text MeasureText;

        [Tooltip("文本换行宽度；MeasureText 当前宽度为 0 时使用")]
        public float TextMaxWidth = 500f;

        [Tooltip("MeasureTarget 宽度 = 文本实际宽度 + 此值，聊天里通常表示气泡框左右内边距")]
        public float MeasureTargetExtraWidth;

        [Tooltip("MeasureTarget 高度 = 文本高度 + 此值，聊天里通常表示气泡框上下内边距")]
        public float MeasureTargetExtraHeight;

        [Tooltip("TweenTarget 最小高度；大于 0 时会作为 item 行高下限")]
        public float TweenTargetMinHeight;

        [Tooltip("Item 高度 = MeasureTarget 高度 + 此值，聊天里通常表示头像/昵称区域额外高度")]
        public float ExtraHeight;

        public void SetChatPush(ChatPushModel model)
        {
            ApplyChatPush(model);
            SetMeasureTextAndFit(GetMeasureContent(model));
        }

        protected abstract void ApplyChatPush(ChatPushModel model);

        protected virtual string GetMeasureContent(ChatPushModel model)
        {
            return model != null ? model.Content : string.Empty;
        }

        protected void SetPlayerInfo(ChatPushModel model, bool visible)
        {
            var playerNameTransform = transform.Find("PlayerName");
            if (playerNameTransform != null)
            {
                playerNameTransform.gameObject.SetActive(visible);
                if (visible)
                {
                    var playerName = playerNameTransform.GetComponent<TMP_Text>();
                    if (playerName != null)
                    {
                        playerName.text = model != null && model.Sender != null ? model.Sender.PlayerName : string.Empty;
                    }
                }
            }

            var playerHead = GetComponentInChildren<PlayerHead>(true);
            if (playerHead != null)
            {
                playerHead.gameObject.SetActive(visible);
                if (visible)
                {
                    var sender = model.Sender;
                    playerHead.SetData(new PlayerHeadData
                    {
                        Avatar = sender.Avatar,
                        AvatarFrame = sender.AvatarFrame,
                    });
                }
            }
        }

        public float GetPreferredHeight()
        {
            var target = MeasureTarget != null ? MeasureTarget : (RectTransform)transform;
            var text = MeasureText != null ? MeasureText : target.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                var width = GetMeasureTextMaxWidth(text);
                var preferred = text.GetPreferredValues(width, 0f);
                return GetTweenTargetHeight(GetMeasureTargetHeight(preferred.y) + ExtraHeight);
            }

            return target.rect.height;
        }

        public virtual void SetMeasureTextAndFit(string value)
        {
            var target = MeasureTarget != null ? MeasureTarget : (RectTransform)transform;
            var text = MeasureText != null ? MeasureText : target.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                return;
            }

            text.text = value ?? string.Empty;

            var width = GetMeasureTextMaxWidth(text);
            var preferred = text.GetPreferredValues(width, 0f);
            var textWidth = GetMeasureTextWidth(preferred.x, width);
            var targetHeight = GetMeasureTargetHeight(preferred.y);
            text.rectTransform.sizeDelta = new Vector2(textWidth, preferred.y);
            target.sizeDelta = new Vector2(GetMeasureTargetWidth(textWidth), targetHeight);

            if (TweenTarget != null)
            {
                var tweenTargetHeight = GetTweenTargetHeight(targetHeight + ExtraHeight);
                TweenTarget.sizeDelta = new Vector2(TweenTarget.sizeDelta.x, tweenTargetHeight);
            }
        }

        private float GetMeasureTargetHeight(float textHeight)
        {
            return Mathf.Max(1f, textHeight + MeasureTargetExtraHeight);
        }

        private float GetMeasureTargetWidth(float textWidth)
        {
            return Mathf.Max(1f, textWidth + MeasureTargetExtraWidth);
        }

        private float GetMeasureTextMaxWidth(TMP_Text text)
        {
            var width = TextMaxWidth > 0f ? TextMaxWidth : text.rectTransform.rect.width;
            return width > 0f ? width : 500f;
        }

        private static float GetMeasureTextWidth(float preferredWidth, float maxWidth)
        {
            return Mathf.Clamp(preferredWidth, 1f, maxWidth);
        }

        private float GetTweenTargetHeight(float height)
        {
            if (TweenTargetMinHeight > 0f)
            {
                height = Mathf.Max(height, TweenTargetMinHeight);
            }

            return Mathf.Max(1f, height);
        }
    }
}
