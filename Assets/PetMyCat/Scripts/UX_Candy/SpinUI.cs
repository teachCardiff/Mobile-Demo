using System.Collections;
using UnityEngine;

namespace UX_Candy
{
/// <summary>
/// Spins a UI element (RectTransform) around a chosen local axis.
/// Supports one-shot spins or looping, with AnimationCurve control.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SpinUI : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Spin")]
    [SerializeField] private Axis axis = Axis.Z;

    [Tooltip("Total degrees to rotate for one spin cycle (one-shot mode).")]
    [SerializeField] private float degrees = 360f;

    [Tooltip("Seconds for one spin cycle (one-shot or each loop cycle).")]
    [Min(0.001f)]
    [SerializeField] private float duration = 0.75f;

    [Tooltip("If true, repeats forever (each cycle uses 'degrees' over 'duration').")]
    [SerializeField] private bool loop = true;

    [Tooltip("Curve evaluated from 0..1 time. Y is normalized rotation (0..1).\n" +
             "For ease-in/out, use a smooth curve from 0 to 1.")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("If true, uses unscaled time so it can spin while the game is paused.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Runtime")]
    [SerializeField] private bool playOnEnable = true;

    private RectTransform rect;
    private Quaternion baseLocalRotation;
    private Coroutine routine;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        rect = (RectTransform)transform;
        baseLocalRotation = rect.localRotation;
    }

    private void OnEnable()
    {
        baseLocalRotation = rect.localRotation;
        if (playOnEnable)
            Play(restart: true);
    }

    private void OnDisable()
    {
        Stop(restore: true);
    }

    /// <summary>
    /// Start spinning.
    /// If restart is true, caches the current rotation as the base.
    /// </summary>
    public void Play(bool restart = true)
    {
        if (restart && rect != null)
            baseLocalRotation = rect.localRotation;

        if (routine != null)
            StopCoroutine(routine);

        IsPlaying = true;
        routine = StartCoroutine(SpinRoutine());
    }

    /// <summary>
    /// Stop spinning.
    /// </summary>
    public void Stop(bool restore = false)
    {
        IsPlaying = false;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (restore && rect != null)
            rect.localRotation = baseLocalRotation;
    }

    private IEnumerator SpinRoutine()
    {
        if (duration <= 0f)
        {
            routine = null;
            yield break;
        }

        // Loop cycles forever or run once.
        do
        {
            float t = 0f;
            float prevCurveValue = EvaluateCurve01(0f);

            // Ensure we start from a clean base at the beginning of each cycle.
            rect.localRotation = baseLocalRotation;

            while (t < duration)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t += dt;

                float normalized = Mathf.Clamp01(t / duration);
                float curveValue = EvaluateCurve01(normalized);

                // Apply delta rotation based on curve change since last frame.
                float delta01 = curveValue - prevCurveValue;
                prevCurveValue = curveValue;

                float deltaDegrees = delta01 * degrees;
                rect.localRotation = rect.localRotation * Quaternion.AngleAxis(deltaDegrees, AxisVector(axis));

                yield return null;
            }

            // Snap to exact end of cycle.
            rect.localRotation = baseLocalRotation * Quaternion.AngleAxis(degrees, AxisVector(axis));

            // If looping, reset base to keep the rotation accumulating smoothly.
            if (loop)
                baseLocalRotation = rect.localRotation;

        } while (loop && IsPlaying);

        routine = null;
        IsPlaying = false;
    }

    private float EvaluateCurve01(float t)
    {
        if (curve == null)
            return t;

        // Clamp to [0,1] so weird curves don't break delta logic too badly.
        return Mathf.Clamp01(curve.Evaluate(t));
    }

    private static Vector3 AxisVector(Axis a)
    {
        return a switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            _ => Vector3.forward,
        };
    }
}
}
