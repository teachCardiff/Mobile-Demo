using System.Collections;
using UnityEngine;

namespace UX_Candy
{
/// <summary>
/// Pop-up effect for UI elements: scales from 0 to its designed (initial) scale.
/// Use an AnimationCurve that can overshoot above 1 and come back to 1.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class PopUpUI : MonoBehaviour, ITransitionEffect
{
    [Header("Pop Up")]
    [Tooltip("Seconds for the pop-up animation.")]
    [Min(0.001f)]
    [SerializeField] private float duration = 0.25f;

    [Tooltip("Curve evaluated from 0..1 time. Y is the scale multiplier (0=start, 1=end).\n" +
             "For overshoot, set keys above 1 and end at 1.")]
    [SerializeField] private AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 3f),
        new Keyframe(0.7f, 1.15f, 0f, 0f),
        new Keyframe(1f, 1f, -2f, 0f)
    );

    [Tooltip("If true, uses Time.unscaledDeltaTime so it can play while timeScale is 0.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Runtime")]
    [SerializeField] private bool playOnEnable = true;

    private RectTransform rect;
    private Vector3 designedScale;
    private Coroutine routine;

    private void Awake()
    {
        rect = (RectTransform)transform;
        designedScale = rect.localScale;
    }

    private void OnEnable()
    {
        // Re-cache in case layout/authoring changed it.
        designedScale = rect.localScale;

        if (playOnEnable)
            Play(restart: true);
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        // Don't force scale here; some UI might disable mid-animation intentionally.
    }

    /// <summary>
    /// Plays the pop-up from 0 scale to designed scale.
    /// </summary>
    public void Play(bool restart = true)
    {
        if (restart)
            ResetToZero();

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(PopRoutine());
    }

    /// <summary>
    /// Plays the reverse pop (from designed scale down to 0) following the inverse of the curve.
    /// </summary>
    public void PopOut(bool restartFromDesigned = true)
    {
        if (restartFromDesigned)
            SnapToDesigned();

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(PopRoutine(reverse: true));
    }

    /// <summary>
    /// Sets scale to 0 immediately.
    /// </summary>
    public void ResetToZero()
    {
        if (rect == null) rect = (RectTransform)transform;
        rect.localScale = Vector3.zero;
    }

    /// <summary>
    /// Sets scale to the designed (initial) scale immediately.
    /// </summary>
    public void SnapToDesigned()
    {
        if (rect == null) rect = (RectTransform)transform;
        rect.localScale = designedScale;
    }

    private IEnumerator PopRoutine(bool reverse = false)
    {
        float t = 0f;

        // If something changed our authored scale, keep the cached designedScale from Awake/OnEnable.
        // (SnapToDesigned/ResetToZero manage the current scale explicitly.)

        while (t < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float normalized = Mathf.Clamp01(t / duration);
            float curveTime = reverse ? 1f - normalized : normalized;
            float m = curve != null ? curve.Evaluate(curveTime) : curveTime;

            rect.localScale = designedScale * m;
            yield return null;
        }

        rect.localScale = reverse ? Vector3.zero : designedScale;
        routine = null;
    }

    // ITransitionEffect implementation (covered = full-size/designed, revealed = hidden/scale 0)
    public float InDuration => duration;
    public float OutDuration => duration;

    public void SnapCovered() => SnapToDesigned();
    public void SnapRevealed() => ResetToZero();

    public void PlayIn() => Play(restart: true);
    public void PlayOut() => PopOut(restartFromDesigned: true);
}
}
