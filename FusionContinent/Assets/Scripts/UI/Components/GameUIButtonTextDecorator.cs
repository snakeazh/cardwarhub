using TMPro;
using UnityEngine;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// 文字装饰器：只负责把文案显示到按钮文本上。
    /// </summary>
    public class GameUIButtonTextDecorator : GameUIButtonDecorator
    {
        [SerializeField] private TextMeshProUGUI mLabel;
        [SerializeField] private string mText = "";

        public override int Order => -100;

        public override void Refresh()
        {
            ApplyText();
        }

        public void SetText(string text)
        {
            mText = text ?? "";
            ApplyText();
        }

        private void ApplyText()
        {
            if (mLabel != null)
            {
                mLabel.text = mText;
                mLabel.rectTransform.sizeDelta = new Vector2(mLabel.preferredWidth, mLabel.preferredHeight);
            }
        }
    }
}
