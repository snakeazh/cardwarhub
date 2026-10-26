using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    public static class GrayTools
    {
        public static Material GrayMaterial;
        public static void SetGray(GameObject target,bool interactable)
        {
            var graphic = target.GetComponent<MaskableGraphic>();


            SetSelfGray(graphic,interactable);
            foreach (var maskableGraphic in target.GetComponentsInChildren<MaskableGraphic>(true))
            {
                SetSelfGray(maskableGraphic,interactable);
            }
        }

        private static void SetSelfGray(MaskableGraphic target,bool interactable)
        {
            if (target== null)
            {
                return;
            }
            
            if (target)
            {
                target.material = interactable ? null : GrayMaterial;
            }
        }
    }
}