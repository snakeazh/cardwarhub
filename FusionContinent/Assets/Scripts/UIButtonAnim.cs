using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (animator == null)
        {
            animator = transform.parent != null ? transform.parent.GetComponent<Animator>() : null;
        }
    }

    public void SetAnimatorEnabled(bool enabled)
    {
        if (animator != null)
        {
            animator.enabled = enabled;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (animator == null)
        {
            return;
        }

        animator.Play("Pressed");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (animator == null)
        {
            return;
        }

        animator.Play("Release");
    }
}
