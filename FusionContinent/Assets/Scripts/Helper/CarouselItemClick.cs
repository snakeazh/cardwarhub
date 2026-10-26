using UnityEngine;
using UnityEngine.UI;

public class CarouselItemClick : MonoBehaviour
{
    public LoopCarousel carousel;

    public void OnClick()
    {
        carousel.CenterOnItem(transform as RectTransform);
    }
}