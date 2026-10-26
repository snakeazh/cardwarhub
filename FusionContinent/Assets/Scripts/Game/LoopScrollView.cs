using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LoopScrollView : MonoBehaviour
{
    [SerializeField] private Texture[] roomTextures; //所有卡片
    [SerializeField] private GameObject itemPrefab; //列表项预制体
    [SerializeField] private Transform itemParent; //列表项的父级，将卡片生成到该物体下
    [SerializeField] private Button prevButton; //上一个按钮
    [SerializeField] private Button nextButton; //下一个按钮
    [SerializeField] private float interval = 400f; //卡片之间的间距
    
    //生成的卡片列表
    private readonly List<LoopScrollViewItem> itemList = new List<LoopScrollViewItem>();
    //字典用于存储各位置对应的卡片层级和坐标
    private readonly Dictionary<int, LoopScrollViewData> map = new Dictionary<int, LoopScrollViewData>();

    private void Start()
    {
        for (int i = 0; i < roomTextures.Length; i++)
        {
            var tex = roomTextures[i];
            var instance = Instantiate(itemPrefab);
            instance.SetActive(true);
            instance.transform.SetParent(itemParent, false);
            instance.GetComponent<RawImage>().texture = tex;
            instance.name = i.ToString();

            //坐标位置
            (instance.transform as RectTransform).anchoredPosition3D = Vector3.right * interval
                * (i <= roomTextures.Length / 2 ? i : (i - roomTextures.Length));
            //层级关系
            instance.transform.SetSiblingIndex(i <= roomTextures.Length / 2 ? 0 : itemParent.childCount - 2);
            //大小
            instance.transform.localScale = (i == 0 ? 1.2f : 1f) * Vector3.one;

            var item = instance.AddComponent<LoopScrollViewItem>();
            item.Index = i;
            itemList.Add(item);
        }
        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            map.Add(i, new LoopScrollViewData(item.transform.GetSiblingIndex(), (item.transform as RectTransform).anchoredPosition3D));
        }

        //添加按钮点击事件
        nextButton.onClick.AddListener(OnNextButtonClick);
        prevButton.onClick.AddListener(OnPrevButtonClick);
    }

    //下一个按钮点击事件
    private void OnNextButtonClick()
    {
        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            bool last = item.Index == itemList.Count / 2;
            int index = item.Index + 1;
            index = index >= itemList.Count ? 0 : index;
            item.Index = index;
            item.Move(map[index]);
        }
    }
    //上一个按钮点击事件
    private void OnPrevButtonClick()
    {
        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            int index = item.Index - 1;
            index = index < 0 ? itemList.Count - 1 : index;
            item.Index = index;
            item.Move(map[index]);
        }
    }
}
