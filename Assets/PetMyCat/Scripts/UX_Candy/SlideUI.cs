using System.Collections;
using UnityEngine;

namespace UX_Candy
{
/// <summary>
/// Slides a UI element in/out by animating its RectTransform.anchoredPosition.
/// The "designed" position is whatever anchoredPosition the element has when enabled/awake.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SlideUI : MonoBehaviour, ITransitionEffect
{
    public enum SlideDirection
    {
        Left,   // slides from right -> left
        Right,  // slides from left  -> right
        Up,     // slides from down  -> up
        Down    // slides from up    -> down
    }

    [Header("Slide")]
    [Min(0.001f)]
    [SerializeField] private float duration = 0.25f;

    [Tooltip("Curve evaluated from 0..1 time. Y is the interpolation amount.\n" +
             "Typical: start at 0 and end at 1. For overshoot, go above 1 then settle to 1.")]
    [SerializeField] private AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 3f),
        new Keyframe(0.8f, 1.05f, 0f, 0f),
        new Keyframe(1f, 1f, -2f, 0f)
    );

    [SerializeField] private SlideDirection direction = SlideDirection.Left;

    [Tooltip("How far (in pixels) the UI starts from its designed anchored position.")]
    [Min(0f)]
    [SerializeField] private float distance = 600f;

    [Tooltip("If true, uses Time.unscaledDeltaTime so it can play while timeScale is 0.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Runtime")]
    [SerializeField] private bool playOnEnable = true;

    private RectTransform rect;
    private Vector2 designedAnchoredPosition;
    private Coroutine routine;

    private void Awake()
    {
        rect = (RectTransform)transform;
        designedAnchoredPosition = rect.anchoredPosition;
    }

    private void OnEnable()
    {
        // Re-cache in case layout/authoring changed it.
        designedAnchoredPosition = rect.anchoredPosition;

        if (playOnEnable)
            SlideIn(restartFromOffscreen: true);
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    public void SlideIn(bool restartFromOffscreen = true)
    {
        if (restartFromOffscreen)
            SnapToOffscreen();

        StartSlide(reverse: false);
    }

    public void SlideOut(bool restartFromDesigned = true)
    {
        if (restartFromDesigned)
            SnapToDesigned();

        StartSlide(reverse: true);
    }

    public void SnapToDesigned()
    {
        if (rect == null) rect = (RectTransform)transform;
        rect.anchoredPosition = designedAnchoredPosition;
    }

    public void SnapToOffscreen()
    {
        if (rect == null) rect = (RectTransform)transform;
        rect.anchoredPosition = designedAnchoredPosition + GetOffset();
    }

    private void StartSlide(bool reverse)
    {
        if (routine != null)
            StopCoroutine(routine);
        
        routine = StartCoroutine(SlideRoutine(reverse));
    }

    private IEnumerator SlideRoutine(bool reverse)
    {
        float t = 0f;
        Vector2 start = designedAnchoredPosition + GetOffset();
        Vector2 end = designedAnchoredPosition;

        // Reverse means designed -> offscreen.
        if (reverse)
        {
            (start, end) = (end, start);
        }

        while (t < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float normalized = Mathf.Clamp01(t / duration);
            float m = curve != null ? curve.Evaluate(normalized) : normalized;

            rect.anchoredPosition = Vector2.LerpUnclamped(start, end, m);
            yield return null;
        }

        rect.anchoredPosition = end;
        routine = null;
    }

    private Vector2 GetOffset()
    {
        switch (direction)
        {
            case SlideDirection.Left:
                // slide from right -> left, so start to the right
                return new Vector2(+distance, 0f);
            case SlideDirection.Right:
                // slide from left -> right, so start to the left
                return new Vector2(-distance, 0f);
            case SlideDirection.Up:
                // slide from down -> up, so start below
                return new Vector2(0f, -distance);
            case SlideDirection.Down:
                // slide from up -> down, so start above
                return new Vector2(0f, +distance);
            default:
                return Vector2.zero;
        }
    }

    /// <summary>Effect duration in seconds.</summary>
    public float Duration => duration;

    // ITransitionEffect implementation (covered = on-screen/designed, revealed = offscreen)
    public float InDuration => duration;
    public float OutDuration => duration;

    public void SnapCovered() => SnapToDesigned();
    public void SnapRevealed() => SnapToOffscreen();

    public void PlayIn() => SlideIn(restartFromOffscreen: true);
    public void PlayOut() => SlideOut(restartFromDesigned: true);
}
}
