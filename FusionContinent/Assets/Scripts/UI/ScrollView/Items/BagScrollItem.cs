using System;
using System.Collections;
using System.Collections.Generic;
using ScrollView;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollView
{
    public class BagScrollItem : ScrollItem
    {
        public Button clickBtn;


        public Action OnClick;
        public override void OnSpawn()
        {
            base.OnSpawn();
            if (clickBtn!= null)
            {
                clickBtn.onClick.RemoveAllListeners();
                clickBtn.onClick.AddListener(() =>
                {
                    OnClick?.Invoke();
                });
            }
        }

        public override void OnRelease()
        {
            if (clickBtn!= null)
            {
                clickBtn.onClick.RemoveAllListeners();
            }
            base.OnRelease();
            
        }
    }

}
