using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollView
{
    public class TestHorScrollView : MonoBehaviour
    {
        public UIScrollView horizontalScrollView;

        public UIScrollView verticalScrollView;

        public int count;
        public InputField inputField;
        public List<ScrollTestData> Datas = new List<ScrollTestData>();
        public void SetData()
        {
            Datas.Clear();

            for (int i = 0; i < count; i++)
            {
                Datas.Add(new ScrollTestData()
                {
                    index = i,
                });
            }
            horizontalScrollView.SetPrefabFactory((parent) =>
            {
                return UIItemCreator.Instance.GetPrefab("key1", parent).gameObject;
            });
            
            horizontalScrollView.SetReleaseAction((game) =>
            {
                UIItemCreator.Instance.ReleasePrefab("key1",game.GetComponent<RectTransform>());
            });
            horizontalScrollView.SetUpdateFunc((index,item) =>
            {
                item.SetData(Datas[index]);
            });
            
            verticalScrollView.SetReleaseAction((game) =>
            {
                UIItemCreator.Instance.ReleasePrefab("key2",game.GetComponent<RectTransform>());
            });
            verticalScrollView.SetPrefabFactory((parent) =>
            {
                return UIItemCreator.Instance.GetPrefab("key2", parent).gameObject;
            });
            verticalScrollView.SetUpdateFunc((index,item) =>
            {
                item.SetData(Datas[index]);
                (item as ScrollTestItem).OnClick = () =>
                {
                    if (selectedIndex  >= 0)
                    {
                        Datas[index].isSelected = !Datas[index].isSelected;
                    }
                    selectedIndex = index;
                    Datas[index].isSelected = !Datas[index].isSelected;
                    verticalScrollView.RefreshSelect();
                };
            });
            
            verticalScrollView.SetUpdateSelectFunc((index,item) =>
            {
                item.RefreshSelect(Datas[index]);
            });
        }

        protected int selectedIndex = -1;
        
        private void Start()
        {
  
        }

        public void Show()
        {
            SetData();
            horizontalScrollView.ShowList(count);
            verticalScrollView.ShowList(count);
            verticalScrollView.PlayScaleAnimationDOTween();
        }

        public void Hide()
        {
            horizontalScrollView.ReleaseScrollView();
            verticalScrollView.ReleaseScrollView();

        }

        public void SetScroll()
        {
            horizontalScrollView.ScrollToIndex(int.Parse(inputField.text),ScrollAlignment.Start,false);
        }
        public void SetScroll2()
        {
            //horizontalScrollView.ScrollToIndex(int.Parse(inputField.text));
        }
    }
}