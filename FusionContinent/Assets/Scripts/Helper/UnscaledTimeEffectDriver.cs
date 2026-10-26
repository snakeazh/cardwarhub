using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Force common visual effect components to run with unscaled time.
/// Attach this to the effect root that should ignore Time.timeScale.
/// </summary>
[DisallowMultipleComponent]
public class UnscaledTimeEffectDriver : MonoBehaviour
{
    [Header("Apply Timing")]
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool includeInactiveChildren = true;

    [Header("Auto Play On Enable")]
    [SerializeField] private bool autoPlayParticleSystems = true;
    [SerializeField] private bool autoPlayPlayableDirectors = true;

    private ParticleSystem[] _particleSystems;
    private Animator[] _animators;
    private PlayableDirector[] _playableDirectors;

    private void Awake()
    {
        if (applyOnAwake)
        {
            RefreshAndApply();
        }
    }

    private void OnEnable()
    {
        if (applyOnEnable)
        {
            RefreshAndApply();
        }

        if (autoPlayParticleSystems)
        {
            PlayParticleSystems();
        }

        if (autoPlayPlayableDirectors)
        {
            PlayPlayableDirectors();
        }
    }

    [ContextMenu("Refresh And Apply Unscaled Time")]
    public void RefreshAndApply()
    {
        CacheComponents();
        ApplyUnscaledTime();
    }

    [ContextMenu("Apply Unscaled Time")]
    public void ApplyUnscaledTime()
    {
        if (_particleSystems != null)
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                if (ps == null) continue;
                var main = ps.main;
                main.useUnscaledTime = true;
            }
        }

        if (_animators != null)
        {
            for (int i = 0; i < _animators.Length; i++)
            {
                var animator = _animators[i];
                if (animator == null) continue;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }

        if (_playableDirectors != null)
        {
            for (int i = 0; i < _playableDirectors.Length; i++)
            {
                var director = _playableDirectors[i];
                if (director == null) continue;
                director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            }
        }
    }

    [ContextMenu("Play Particle Systems")]
    public void PlayParticleSystems()
    {
        if (_particleSystems == null)
        {
            CacheComponents();
        }

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            var ps = _particleSystems[i];
            if (ps == null) continue;
            ps.Play(true);
        }
    }

    [ContextMenu("Play Playable Directors")]
    public void PlayPlayableDirectors()
    {
        if (_playableDirectors == null)
        {
            CacheComponents();
        }

        for (int i = 0; i < _playableDirectors.Length; i++)
        {
            var director = _playableDirectors[i];
            if (director == null) continue;
            director.Play();
        }
    }

    private void CacheComponents()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>(includeInactiveChildren);
        _animators = GetComponentsInChildren<Animator>(includeInactiveChildren);
        _playableDirectors = GetComponentsInChildren<PlayableDirector>(includeInactiveChildren);
    }
}
