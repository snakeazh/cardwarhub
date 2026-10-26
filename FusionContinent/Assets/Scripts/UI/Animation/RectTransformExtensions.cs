using UnityEngine;

namespace FusionContinent.Animation
{
    public static class RectTransformExtensions
    {
        public static void SetAnchorPosX(this RectTransform self,float x)
        {
            var pos = self.anchoredPosition;
            self.anchoredPosition = new Vector2(x, pos.y);
        }
        
        public static void SetAnchorPosY(this RectTransform self,float y)
        {
            var pos = self.anchoredPosition;
            self.anchoredPosition = new Vector2(pos.x, y);
        }
    }
}