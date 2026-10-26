using UnityEngine;

public class LevelUpAnimation : MonoBehaviour
{
    Animator animator;

    public Animator Animator
    {
        get
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            return animator;
        }
    }

    public enum State
    {
        LevelUp,
        Lock,
        UnLock
    }

    public static State ResolvePlayState(bool isLocked, bool hasCachedState, State cachedState)
    {
        if (hasCachedState && cachedState == State.Lock && !isLocked)
        {
            return State.LevelUp;
        }

        return isLocked ? State.Lock : State.UnLock;
    }

    public static State ToCachedState(bool isLocked)
    {
        return isLocked ? State.Lock : State.UnLock;
    }

    /// <summary>
    /// 节点从 inactive 变为 active 后调用，确保 Animator 能接收 Trigger。
    /// </summary>
    public void PrepareAnimator()
    {
        Animator.Update(0f);
    }

    public void SetAnimationState(State state)
    {
        ResetAllTriggers();
        Animator.SetTrigger(state.ToString());
    }

    void ResetAllTriggers()
    {
        Animator.ResetTrigger(State.LevelUp.ToString());
        Animator.ResetTrigger(State.Lock.ToString());
        Animator.ResetTrigger(State.UnLock.ToString());
    }
}
