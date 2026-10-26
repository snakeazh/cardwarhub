using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;

namespace FusionContinent
{
    public class EffectItemData : UIPanelData
    {
        public int skillLevel;
        public string skillDes;
        public bool isLock;
    }
    
    public class EffectItem : MonoBehaviour
    {
        public TMP_Text levelText;
        public TMP_Text desText;
        public Transform mask;

        private RectTransform mSelfRect;

        void Awake()
        {
            mSelfRect = GetComponent<RectTransform>();
        }

        public void SetData(EffectItemData data)
        {
            if (data == null) return;
            levelText.text = "lv." + data.skillLevel;
            desText.text = data.skillDes;
            mask.gameObject.SetActive(data.isLock);

            // 先更新文本自身高度为文本显示实际高度
            desText.ForceMeshUpdate();
            var textHeight = desText.renderedHeight;
            desText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

            // 再更新 EffectItem 节点高度为文本高度 + 100
            if (mSelfRect != null)
            {
                mSelfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight + 70);
            }
        }


    }
    
}

