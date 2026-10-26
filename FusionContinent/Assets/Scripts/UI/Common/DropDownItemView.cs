using TMPro;
using UnityEngine;

namespace FusionContinent.UI
{
    public class DropDownItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI mLabel;
        [SerializeField] private GameObject mCheckMark;

        public TextMeshProUGUI Label => mLabel;
        public GameObject CheckMark => mCheckMark;

        public void SetText(string text)
        {
            if (mLabel != null)
            {
                mLabel.text = text;
            }
        }

        public void SetSelected(bool selected)
        {
            if (mCheckMark != null)
            {
                mCheckMark.SetActive(selected);
            }
        }
    }
}
