using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/**
 * 子scrollview调用OnBeginDrag时，传递OnBeginDrag到外层的scrollview
 */
public class ScrollViewItem: MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ScrollRect scrollview;

    public void SetScrollView(ScrollRect scrollView)
    {
        this.scrollview = scrollView;
    }

    public  void OnBeginDrag(PointerEventData eventdata)
    {
        if (scrollview != null) scrollview.OnBeginDrag(eventdata);
    }

    public  void OnDrag(PointerEventData eventdata)
    {
        if (scrollview != null) scrollview.OnDrag(eventdata);
    }

    public  void OnEndDrag(PointerEventData eventdata)
    {
        if (scrollview != null) scrollview.OnEndDrag(eventdata);
    }
}
