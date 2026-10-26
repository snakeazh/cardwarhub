using Game.RedDot.View;
using TMPro;
using UnityEngine;

namespace FusionContinent
{
    public class LocalRedDot : MonoBehaviour
    {
        public GameObject Dot;
        public TMP_Text CountText;
        public string DotPrefabAddress = RedDotDisplay.RedPointPrefab;

        int _redCount;

        public bool IsActive => _redCount > 0;

        public void SetDotPrefab(string prefabAddress)
        {
            if (string.IsNullOrEmpty(prefabAddress))
            {
                prefabAddress = RedDotDisplay.RedPointPrefab;
            }

            if (DotPrefabAddress == prefabAddress)
            {
                return;
            }

            DotPrefabAddress = prefabAddress;
            if (Dot != null)
            {
                Destroy(Dot);
                Dot = null;
            }
        }

        public void SetRed(int count, Vector2 anchoredPosition = default)
        {
            _redCount = count;
            RedDotDisplay.Apply(transform, ref Dot, CountText, _redCount, anchoredPosition, DotPrefabAddress);
        }

        public void SetRed(int count, string prefabAddress, Vector2 anchoredPosition = default)
        {
            SetDotPrefab(prefabAddress);
            SetRed(count, anchoredPosition);
        }
    }
}
