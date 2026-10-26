using Manager;
using QFramework;
using UnityEngine;

namespace FusionContinent.Sounds
{
    public abstract class SoundComponent : MonoBehaviour
    {
        public SoundType soundType = SoundType.CommonBtn;
        public float volumeScale = 1f;


        protected virtual bool IsInteractable
        {
            get { return true; }
        }

        public virtual void PlaySound()
        {
            SoundManager.Instance.PlayVoice(soundType);
        }
    }
}