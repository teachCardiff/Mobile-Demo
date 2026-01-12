using UnityEngine;
using UnityEngine.InputSystem;

public class DragPlayer : MonoBehaviour
{
    [Tooltip("Camera used to convert screen touch position to world. Defaults to Camera.main.")]
    [SerializeField] private Camera worldCamera;

    [Tooltip("If true, only move while the finger is down; otherwise keep last position.")]
    [SerializeField] private bool onlyWhilePressed = true;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        var ts = Touchscreen.current;
        if (ts == null || worldCamera == null)
            return;

        var primary = ts.primaryTouch;
        var phase = primary.phase.ReadValue();
        bool pressed = phase != UnityEngine.InputSystem.TouchPhase.None
                    && phase != UnityEngine.InputSystem.TouchPhase.Ended
                    && phase != UnityEngine.InputSystem.TouchPhase.Canceled;

        if (onlyWhilePressed && !pressed)
            return;

        Vector2 screenPos = primary.position.ReadValue();
           
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        world.z = 0;

        transform.position = world;
    }
}
