using UnityEngine;

namespace UX_Candy
{
/// <summary>
/// Simple "shake/wiggle" effect for UI elements.
/// Attach to a UI GameObject with a RectTransform.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class WiggleUI : MonoBehaviour
{
    [Header("Wiggle")]
    [Tooltip("How far (in UI units / pixels) the element can move from its starting anchored position.")]
    [Min(0f)]
    [SerializeField] private float positionIntensity = 8f;

    [Tooltip("How fast the wiggle moves. Higher = faster.")]
    [Min(0f)]
    [SerializeField] private float speed = 18f;

    [Tooltip("If true, uses Time.unscaledTime so the wiggle still plays while the game is paused.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Optional rotation")]
    [Tooltip("If > 0, also wiggles rotation by this many degrees.")]
    [Min(0f)]
    [SerializeField] private float rotationIntensityDegrees = 0f;

    [Header("Runtime")]
    [SerializeField] private bool playOnEnable = true;

    private RectTransform rect;
    private Vector2 baseAnchoredPosition;
    private Quaternion baseLocalRotation;

    // Phase offsets so multiple elements don't wiggle in sync.
    private float seed;

    public float PositionIntensity
    {
        get => positionIntensity;
        set => positionIntensity = Mathf.Max(0f, value);
    }

    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0f, value);
    }

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        rect = (RectTransform)transform;
        baseAnchoredPosition = rect.anchoredPosition;
        baseLocalRotation = rect.localRotation;
        seed = Random.value * 1000f;
    }

    private void OnEnable()
    {
        // Cache in case layout moved it while disabled.
        baseAnchoredPosition = rect.anchoredPosition;
        baseLocalRotation = rect.localRotation;

        IsPlaying = playOnEnable;
    }

    private void OnDisable()
    {
        // Restore so we don't leave the UI offset when disabled.
        if (rect != null)
        {
            rect.anchoredPosition = baseAnchoredPosition;
            rect.localRotation = baseLocalRotation;
        }
    }

    private void LateUpdate()
    {
        if (!IsPlaying)
            return;

        if (positionIntensity <= 0f && rotationIntensityDegrees <= 0f)
            return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        t = (t + seed) * speed;

        // Smooth pseudo-random movement using Perlin noise.
        // Perlin is 0..1, remap to -1..1.
        float nx = Mathf.PerlinNoise(t, 0.1234f) * 2f - 1f;
        float ny = Mathf.PerlinNoise(0.5678f, t) * 2f - 1f;

        Vector2 offset = new Vector2(nx, ny) * positionIntensity;
        rect.anchoredPosition = baseAnchoredPosition + offset;

        if (rotationIntensityDegrees > 0f)
        {
            float nr = Mathf.PerlinNoise(t, t * 0.37f) * 2f - 1f;
            rect.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, nr * rotationIntensityDegrees);
        }
    }

    /// <summary>Starts wiggling from the current anchored position.</summary>
    public void Play(bool recacheBase = true)
    {
        if (recacheBase && rect != null)
        {
            baseAnchoredPosition = rect.anchoredPosition;
            baseLocalRotation = rect.localRotation;
        }
        IsPlaying = true;
    }

    /// <summary>Stops wiggling and restores the original anchored position.</summary>
    public void Stop(bool restore = true)
    {
        IsPlaying = false;

        if (restore && rect != null)
        {
            rect.anchoredPosition = baseAnchoredPosition;
            rect.localRotation = baseLocalRotation;
        }
    }
}
}
