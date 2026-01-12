using UnityEngine;

namespace UX_Candy
{
/// <summary>
/// Smooth "pulse" (scale up/down) effect for UI elements.
/// Attach to a UI GameObject (RectTransform).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class PulseUI : MonoBehaviour
{
    [Header("Pulse")]
    [Tooltip("Pulse amount as a multiplier. 0.15 means +/-15% around the base scale.")]
    [Min(0f)]
    [SerializeField] private float intensity = 0.15f;

    [Tooltip("How many pulses per second. 1 = one full up+down cycle per second.")]
    [Min(0f)]
    [SerializeField] private float speedHz = 1.5f;

    [Tooltip("If true, uses Time.unscaledTime so the pulse still plays while the game is paused.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Runtime")]
    [SerializeField] private bool playOnEnable = true;

    private RectTransform rect;
    private Vector3 baseScale;
    private float phaseOffset;

    public float Intensity
    {
        get => intensity;
        set => intensity = Mathf.Max(0f, value);
    }

    public float SpeedHz
    {
        get => speedHz;
        set => speedHz = Mathf.Max(0f, value);
    }

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        rect = (RectTransform)transform;
        baseScale = rect.localScale;
        phaseOffset = Random.value * Mathf.PI * 2f;
    }

    private void OnEnable()
    {
        // Re-cache in case layout/animation changed it while disabled.
        baseScale = rect.localScale;
        IsPlaying = playOnEnable;
    }

    private void OnDisable()
    {
        if (rect != null)
            rect.localScale = baseScale;
    }

    private void LateUpdate()
    {
        if (!IsPlaying)
            return;

        if (intensity <= 0f || speedHz <= 0f)
        {
            rect.localScale = baseScale;
            return;
        }

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        // Sin wave in [-1,1].
        float s = Mathf.Sin((t * speedHz * Mathf.PI * 2f) + phaseOffset);

        // Map to a multiplier in [1-intensity, 1+intensity].
        float multiplier = 1f + (s * intensity);

        rect.localScale = baseScale * multiplier;
    }

    /// <summary>Starts pulsing from the current localScale.</summary>
    public void Play(bool recacheBase = true)
    {
        if (recacheBase && rect != null)
            baseScale = rect.localScale;

        IsPlaying = true;
    }

    /// <summary>Stops pulsing and restores base scale.</summary>
    public void Stop(bool restore = true)
    {
        IsPlaying = false;

        if (restore && rect != null)
            rect.localScale = baseScale;
    }
}
}
