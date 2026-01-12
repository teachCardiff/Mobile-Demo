using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class DemoInputManager : MonoBehaviour
{
    [Header("Generated input actions (InputTest.inputactions)")]
    [Tooltip("Optional. If assigned, this asset will be enabled and used for tap/press bindings. Swipe/pinch are detected in code.")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;

    // Generated wrapper (Assets/Demo/Test Inputs/InputTest.cs).
    private InputTest generated;

    // Cached actions (optional if user assigns InputActionAsset instead of using generated wrapper).
    private InputAction tapAction;
    private InputAction pressAction;
    private InputAction twoFingerTapAction;

    // Swipe / pinch state.
    [Header("Gesture thresholds")]
    [SerializeField] private float swipeMinDistancePixels = 80f;
    [SerializeField] private float swipeMaxTimeSeconds = 0.5f;
    [SerializeField] private float pinchMinDeltaPixels = 10f;

    private bool primaryActive;
    private Vector2 primaryStartPos;
    private double primaryStartTime;
    private int primaryFingerId = -1;

    private bool twoFingerActive;
    private Vector2 twoFingerStartA;
    private Vector2 twoFingerStartB;
    private double twoFingerStartTime;
    private float twoFingerStartDistance;

    private void OnEnable()
    {
        // Prefer the generated wrapper because it is already in the project.
        // If an explicit InputActionAsset reference is set, use it instead.
        if (inputActions != null)
        {
            inputActions.Enable();
            var touchMap = inputActions.FindActionMap("Touch", throwIfNotFound: false);
            tapAction = touchMap?.FindAction("Tap", throwIfNotFound: false);
            pressAction = touchMap?.FindAction("Press", throwIfNotFound: false);
            twoFingerTapAction = touchMap?.FindAction("TwoFingerTap", throwIfNotFound: false);
        }
        else
        {
            generated = new InputTest();
            generated.Enable();
            tapAction = generated.Touch.Tap;
            // These won't exist until you add them to the .inputactions and regenerate.
            pressAction = generated.asset.FindAction("Touch/Press", throwIfNotFound: false);
            twoFingerTapAction = generated.asset.FindAction("Touch/TwoFingerTap", throwIfNotFound: false);
        }

        if (tapAction != null)
            tapAction.performed += OnTapPerformed;

        if (pressAction != null)
        {
            pressAction.started += OnPressStarted;
            pressAction.canceled += OnPressCanceled;
        }

        if (twoFingerTapAction != null)
            twoFingerTapAction.performed += OnTwoFingerTapPerformed;
    }

    private void OnDisable()
    {
        if (tapAction != null)
            tapAction.performed -= OnTapPerformed;

        if (pressAction != null)
        {
            pressAction.started -= OnPressStarted;
            pressAction.canceled -= OnPressCanceled;
        }

        if (twoFingerTapAction != null)
            twoFingerTapAction.performed -= OnTwoFingerTapPerformed;

        if (generated != null)
        {
            generated.Disable();
            generated.Dispose();
            generated = null;
        }

        if (inputActions != null)
            inputActions.Disable();
    }

    private void Update()
    {
        var ts = Touchscreen.current;
        if (ts == null)
            return;

        // We use the raw touches for swipe/pinch/two-finger swipe because they need movement deltas.
        // Touchscreen.touches is fixed-size; inactive touches have phase == None.
        TouchControl touch0 = ts.touches.Count > 0 ? ts.touches[0] : null;
        TouchControl touch1 = ts.touches.Count > 1 ? ts.touches[1] : null;

        bool t0Active = touch0 != null && touch0.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.None;
        bool t1Active = touch1 != null && touch1.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.None;

        // Primary swipe detection (one finger).
        if (t0Active && !t1Active)
        {
            var phase = touch0.phase.ReadValue();
            var pos = touch0.position.ReadValue();
            var fid = touch0.touchId.ReadValue();

            if (!primaryActive && phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                primaryActive = true;
                primaryStartPos = pos;
                primaryStartTime = Time.unscaledTimeAsDouble;
                primaryFingerId = fid;
            }

            if (primaryActive && (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled))
            {
                double dt = Time.unscaledTimeAsDouble - primaryStartTime;
                Vector2 delta = pos - primaryStartPos;
                float dist = delta.magnitude;

                if (dt <= swipeMaxTimeSeconds && dist >= swipeMinDistancePixels)
                {
                    var dir = GetCardinalDirection(delta);
                    Log($"Swipe (1 finger): dir={dir}, dist={dist:0}, time={dt:0.00}s");
                }

                primaryActive = false;
                primaryFingerId = -1;
            }

            // If the finger id changed (touch array got reused), reset.
            if (primaryActive && primaryFingerId != fid)
            {
                primaryActive = false;
                primaryFingerId = -1;
            }
        }
        else
        {
            primaryActive = false;
            primaryFingerId = -1;
        }

        // Two finger swipe + pinch.
        if (t0Active && t1Active)
        {
            var phase0 = touch0.phase.ReadValue();
            var phase1 = touch1.phase.ReadValue();

            var p0 = touch0.position.ReadValue();
            var p1 = touch1.position.ReadValue();

            if (!twoFingerActive && (phase0 == UnityEngine.InputSystem.TouchPhase.Began || phase1 == UnityEngine.InputSystem.TouchPhase.Began))
            {
                twoFingerActive = true;
                twoFingerStartA = p0;
                twoFingerStartB = p1;
                twoFingerStartTime = Time.unscaledTimeAsDouble;
                twoFingerStartDistance = Vector2.Distance(p0, p1);
            }

            if (twoFingerActive)
            {
                // Pinch detection (continuous) - only log when it changes enough.
                float distNow = Vector2.Distance(p0, p1);
                float pinchDelta = distNow - twoFingerStartDistance;
                if (Mathf.Abs(pinchDelta) >= pinchMinDeltaPixels)
                {
                    Log($"Pinch: {(pinchDelta > 0 ? "out" : "in")} Δ={pinchDelta:0} px");
                    twoFingerStartDistance = distNow; // rebase so it doesn't spam.
                }
            }

            bool ended = (phase0 == UnityEngine.InputSystem.TouchPhase.Ended || phase0 == UnityEngine.InputSystem.TouchPhase.Canceled)
                      || (phase1 == UnityEngine.InputSystem.TouchPhase.Ended || phase1 == UnityEngine.InputSystem.TouchPhase.Canceled);
            if (twoFingerActive && ended)
            {
                double dt = Time.unscaledTimeAsDouble - twoFingerStartTime;

                Vector2 aDelta = p0 - twoFingerStartA;
                Vector2 bDelta = p1 - twoFingerStartB;
                Vector2 avgDelta = (aDelta + bDelta) * 0.5f;

                float dist = avgDelta.magnitude;
                if (dt <= swipeMaxTimeSeconds && dist >= swipeMinDistancePixels)
                {
                    var dir = GetCardinalDirection(avgDelta);
                    Log($"Swipe (2 finger): dir={dir}, dist={dist:0}, time={dt:0.00}s");
                }

                twoFingerActive = false;
            }
        }
        else
        {
            twoFingerActive = false;
        }
    }

    private void OnTapPerformed(InputAction.CallbackContext ctx)
    {
        // Tap binding is to <Touchscreen>/primaryTouch/tap; position isn't exposed by that binding.
        // We'll use Touchscreen.current for a best-effort position.
        var pos = Touchscreen.current?.primaryTouch?.position.ReadValue() ?? Vector2.zero;
        Log($"Tap: pos={pos}");
    }

    private void OnPressStarted(InputAction.CallbackContext ctx)
    {
        var pos = Touchscreen.current?.primaryTouch?.position.ReadValue() ?? Vector2.zero;
        Log($"Press started: pos={pos}");
    }

    private void OnPressCanceled(InputAction.CallbackContext ctx)
    {
        var pos = Touchscreen.current?.primaryTouch?.position.ReadValue() ?? Vector2.zero;
        Log($"Press ended: pos={pos}");
    }

    private void OnTwoFingerTapPerformed(InputAction.CallbackContext ctx)
    {
        Log("Two-finger tap");
    }

    private enum Cardinal { Up, Down, Left, Right }

    private static Cardinal GetCardinalDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return delta.x >= 0 ? Cardinal.Right : Cardinal.Left;
        return delta.y >= 0 ? Cardinal.Up : Cardinal.Down;
    }

    private void Log(string msg)
    {
        if (!logToConsole) return;
        Debug.Log($"[DemoInputManager] {msg}");
    }
}
