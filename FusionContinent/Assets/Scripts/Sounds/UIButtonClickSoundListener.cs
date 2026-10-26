using Manager;

namespace FusionContinent.Sounds
{
// csharp - Assets/Scripts/UIButtonClickSoundListener.cs
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using QFramework;

    [RequireComponent(typeof(Button))]
    public class UIButtonClickSoundListener : SoundComponent, IPointerClickHandler, ISubmitHandler
    {

        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null && button.IsInteractable() && eventData.button == PointerEventData.InputButton.Left)
            {
                PlaySound();
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (button != null && button.IsInteractable())
            {
                PlaySound();
            }
        }
    }
}