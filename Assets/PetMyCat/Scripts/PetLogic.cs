using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PetLogic : MonoBehaviour
{
    [Header("References")]
    [Tooltip("RectTransform for the cat UI image (the thing being petted).")]
    [SerializeField] private RectTransform catRect;

    [Tooltip("Optional: Canvas that renders the cat. Used for proper screen->UI conversion.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Optional zones component on the catRect for favorable/unfavorable areas.")]
    [SerializeField] private CatPettingZones zones;

    [Header("UI Output (optional)")]
    [SerializeField] private Slider irritationSlider;

    [Tooltip("Optional: display score as UI Text.")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Score text format string. Example: 'Score: {0:0}'")]
    [SerializeField] private string scoreFormat = "Score: {0:0}";

    [Header("Scoring")]
    [Tooltip("Base points per second while petting (before zone multipliers).")]
    [Min(0f)]
    [SerializeField] private float basePointsPerSecond = 10f;

    [Tooltip("Minimum screen movement (pixels) per frame to count as a 'petting stroke'.")]
    [Min(0f)]
    [SerializeField] private float minMovePixels = 1.5f;

    [Header("Irritation")]
    [Tooltip("Max irritation value.")]
    [Min(0.001f)]
    [SerializeField] private float irritationMax = 100f;

    [Tooltip("Base irritation increase per second while petting (before zone multipliers).")]
    [Min(0f)]
    [SerializeField] private float baseIrritationPerSecond = 6f;

    [Tooltip("Irritation decrease per second when not petting.")]
    [Min(0f)]
    [SerializeField] private float irritationDecayPerSecond = 8f;

    [Tooltip("If true, uses unscaledTime for scoring/irritation (works when timeScale=0).")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Runtime (read-only)")]
    [SerializeField] private float score;
    [SerializeField] private float irritation;

    private Vector2 lastScreenPos;
    private bool lastPosValid;

    private void Reset()
    {
        // Best-effort auto-hook.
        catRect = GetComponentInChildren<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        zones = catRect != null ? catRect.GetComponent<CatPettingZones>() : null;
    }

    private void Awake()
    {
        if (catRect == null)
            catRect = GetComponentInChildren<RectTransform>();

        if (canvas == null)
            canvas = catRect != null ? catRect.GetComponentInParent<Canvas>() : null;

        if (zones == null && catRect != null)
            zones = catRect.GetComponent<CatPettingZones>();

        irritation = Mathf.Clamp(irritation, 0f, irritationMax);
        SyncUI();
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        bool pettingThisFrame = false;

        // Mobile (New Input System): use Touchscreen.
        var ts = Touchscreen.current;
        if (ts != null && catRect != null)
        {
            var touch = ts.primaryTouch;
            var phase = touch.phase.ReadValue();
            bool pressed = phase != UnityEngine.InputSystem.TouchPhase.None
                        && phase != UnityEngine.InputSystem.TouchPhase.Ended
                        && phase != UnityEngine.InputSystem.TouchPhase.Canceled;

            if (pressed)
            {
                Vector2 screenPos = touch.position.ReadValue();

                if (TryGetCatNormalizedPoint(screenPos, out var normalizedPoint))
                {
                    // Determine whether finger movement is actually 'petting'.
                    float move = 0f;
                    if (lastPosValid)
                        move = Vector2.Distance(screenPos, lastScreenPos);

                    lastScreenPos = screenPos;
                    lastPosValid = true;

                    if (move >= minMovePixels)
                    {
                        // NEW: Only count petting if we hit a configured petting zone.
                        if (zones != null && zones.TryGetZone(catRect, normalizedPoint, out var zone))
                        {
                            pettingThisFrame = true;

                            float scoreMult = zone.scoreMultiplier <= 0f ? 1f : zone.scoreMultiplier;
                            float irritationMult = zone.irritationMultiplier <= 0f ? 1f : zone.irritationMultiplier;

                            score += basePointsPerSecond * scoreMult * dt;
                            irritation += baseIrritationPerSecond * irritationMult * dt;
                            irritation = Mathf.Clamp(irritation, 0f, irritationMax);
                        }
                    }
                }
                else
                {
                    // Touch is active but not on cat.
                    lastPosValid = false;
                }
            }
            else
            {
                lastPosValid = false;
            }
        }

        // Decay irritation when not petting.
        if (!pettingThisFrame)
        {
            irritation -= irritationDecayPerSecond * dt;
            irritation = Mathf.Clamp(irritation, 0f, irritationMax);
        }

        SyncUI();
    }

    private bool TryGetCatNormalizedPoint(Vector2 screenPos, out Vector2 normalizedPoint)
    {
        normalizedPoint = default;
        if (catRect == null)
            return false;

        // Convert screen point to local point in the cat rect.
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(catRect, screenPos, eventCamera, out var local))
            return false;

        var r = catRect.rect;
        // Local is centered; remap to 0..1.
        float nx = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float ny = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
        normalizedPoint = new Vector2(nx, ny);

        // Only count if inside the rect.
        return nx >= 0f && nx <= 1f && ny >= 0f && ny <= 1f;
    }

    private void SyncUI()
    {
        if (irritationSlider != null)
        {
            irritationSlider.minValue = 0f;
            irritationSlider.maxValue = irritationMax;
            irritationSlider.value = irritation;
        }

        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, score);
        }
    }

    public float Score => score;
    public float Irritation => irritation;
}
