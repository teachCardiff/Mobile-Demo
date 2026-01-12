using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UX_Candy;

/// <summary>
/// Controls full-screen scene transitions using a UI cover (e.g., an Image) with a transition effect implementing ITransitionEffect.
/// 
/// Typical setup:
/// - Put this on your TransitionCanvas prefab root.
/// - Assign transitionEffect (e.g., SlideUI, PopUpUI).
/// - Mark this prefab DontDestroyOnLoad (done automatically here).
/// - Call TransitionTo(...) from buttons / gameplay code.
/// 
/// Flow:
/// 1) Ensure cover is visible (SnapCovered)
/// 2) Start async load with allowSceneActivation=false
/// 3) When loaded (progress>=0.9) + optional delay, activate
/// 4) After activation, reveal by PlayOut (covered -> revealed)
/// </summary>
[DisallowMultipleComponent]
public class TransitionController : MonoBehaviour
{
    public static TransitionController Instance { get; private set; }

    // Raised when the transition begins (right before cover-in).
    public static event System.Action TransitionStarted;

    // Raised after the new scene becomes active, but before cover-out (if any).
    public static event System.Action<Scene> SceneActivated;

    // Raised when the full transition routine finishes.
    public static event System.Action TransitionFinished;

    [Header("References")]
    [Tooltip("Component that implements UX_Candy.ITransitionEffect (e.g., SlideUI, PopUpUI).")]
    [SerializeField] private MonoBehaviour transitionEffect;

    private ITransitionEffect Effect => transitionEffect as ITransitionEffect;

    [Header("Editor")]
    [Tooltip("If true, OnValidate will auto-assign the first component found on this GameObject/children that implements ITransitionEffect.")]
    [SerializeField] private bool autoFindTransitionEffectInChildren = true;

    [Header("Timing")]
    [Tooltip("Optional extra delay before beginning the async load (after PlayIn completes).")]
    [Min(0f)]
    [SerializeField] private float delayBeforeLoadSeconds = 0f;

    [Tooltip("Optional delay after the new scene becomes active before revealing it.")]
    [Min(0f)]
    [SerializeField] private float delayAfterSceneActivatedSeconds = 0f;

    [Tooltip("If true, uses unscaled time for delays.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Behavior")]
    [Tooltip("If true, this object persists across scene loads.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Tooltip("If true, reveals the new scene by calling Effect.PlayOut().")]
    [SerializeField] private bool revealOnLoad = true;

    [Header("Cover animation")]
    [Tooltip("Overrides how long to wait for PlayIn(). If 0, uses Effect.InDuration.")]
    [Min(0f)]
    [SerializeField] private float coverInWaitSeconds = 0f;

    [Tooltip("Overrides how long to wait after PlayOut(). If 0, uses Effect.OutDuration.")]
    [Min(0f)]
    [SerializeField] private float coverOutWaitSeconds = 0f;

    private Coroutine transitionRoutine;
    private AsyncOperation loadOp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // Auto-find an effect if one isn't wired.
        if (transitionEffect == null)
            Debug.LogWarning("No transition effect assigned in the inspector");

        Effect?.SnapCovered();
    }

    private void OnValidate()
    {
        // 1) Reject invalid assignments.
        if (transitionEffect != null && transitionEffect is not ITransitionEffect)
        {
            Debug.LogWarning(
                $"{nameof(TransitionController)} on '{name}': Assigned component '{transitionEffect.GetType().Name}' does not implement {nameof(ITransitionEffect)}. Clearing reference.",
                this);
            transitionEffect = null;
        }

        // 2) Optionally auto-pick an effect from this object/children.
        if (autoFindTransitionEffectInChildren && transitionEffect == null)
        {
            var candidates = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < candidates.Length; i++)
            {
                var mb = candidates[i];
                if (mb == null)
                    continue;

                // Don't select ourselves.
                if (ReferenceEquals(mb, this))
                    continue;

                if (mb is ITransitionEffect)
                {
                    transitionEffect = mb;
                    break;
                }
            }
        }
    }

    /// <summary>Transition to a scene by name.</summary>
    public void TransitionTo(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        StartTransition(() => SceneManager.LoadSceneAsync(sceneName));
    }

    /// <summary>Transition to a scene by build index.</summary>
    public void TransitionTo(int buildIndex)
    {
        StartTransition(() => SceneManager.LoadSceneAsync(buildIndex));
    }

    // Convenience methods for wiring to UI Buttons without parameters.
    [Tooltip("Optional default scene build index for button wiring.")]
    [SerializeField] private int defaultBuildIndex = 1;

    public void TransitionToDefault()
    {
        TransitionTo(defaultBuildIndex);
    }

    private void StartTransition(System.Func<AsyncOperation> beginLoad)
    {
        if (beginLoad == null)
            return;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        transitionRoutine = StartCoroutine(TransitionRoutine(beginLoad));
    }

    private IEnumerator TransitionRoutine(System.Func<AsyncOperation> beginLoad)
    {
        TransitionStarted?.Invoke();

        var effect = Effect;

        // 0) Cover-in.
        if (effect != null)
        {
            effect.SnapRevealed();
            effect.PlayIn();

            float inWait = coverInWaitSeconds > 0f ? coverInWaitSeconds : effect.InDuration;
            if (inWait > 0f)
                yield return Wait(inWait);
        }

        // 1) Optional extra delay before load.
        if (delayBeforeLoadSeconds > 0f)
            yield return Wait(delayBeforeLoadSeconds);

        // 2) Start loading.
        loadOp = beginLoad.Invoke();
        if (loadOp == null)
        {
            transitionRoutine = null;
            TransitionFinished?.Invoke();
            yield break;
        }

        loadOp.allowSceneActivation = false;

        // 3) Wait until loaded to activation point.
        while (loadOp.progress < 0.9f)
            yield return null;

        // 4) Activate.
        loadOp.allowSceneActivation = true;

        // 5) Wait a frame so the new scene becomes active.
        yield return null;

        SceneActivated?.Invoke(SceneManager.GetActiveScene());

        if (delayAfterSceneActivatedSeconds > 0f)
            yield return Wait(delayAfterSceneActivatedSeconds);

        // 6) Cover-out.
        if (revealOnLoad && effect != null)
        {
            effect.PlayOut();

            float outWait = coverOutWaitSeconds > 0f ? coverOutWaitSeconds : effect.OutDuration;
            if (outWait > 0f)
                yield return Wait(outWait);
        }

        transitionRoutine = null;
        TransitionFinished?.Invoke();
    }

    private object Wait(float seconds)
    {
        return useUnscaledTime ? (object)new WaitForSecondsRealtime(seconds) : new WaitForSeconds(seconds);
    }
}
