using System;
using FusionContinent.Model;
using UnityEngine;
using Object = System.Object;

namespace FusionContinent.Guide
{
    public class GuideMask : MonoBehaviour
    {
        public GameObject Mask;
        private void OnEnable()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_STATUS,OnGuideChange);
        }

        void OnGuideChange(object info)
        {
            bool status =  (bool)info;
            Mask.SetActive(status);
            // LogTool.LogError(status);
        }

        private void OnDisable()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GUIDE_STATUS,OnGuideChange);
        }
    }
}