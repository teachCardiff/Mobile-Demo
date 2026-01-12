using UnityEngine;
using UnityEngine.InputSystem;

public class MovePlayer : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("An Input Action (Vector2) driven by a virtual joystick (e.g., an On-Screen Stick).")]
    [SerializeField] private InputActionReference move;

    [Header("Movement")]
    [SerializeField] private float moveSpeedUnitsPerSecond = 5f;

    [Tooltip("If set, movement is in this camera's X/Y plane (useful for top-down). If null, uses world X/Y.")]
    [SerializeField] private Camera relativeToCamera;

    private void OnEnable()
    {
        if (move != null)
            move.action.Enable();
    }

    private void OnDisable()
    {
        if (move != null)
            move.action.Disable();
    }

    private void Update()
    {
        if (move == null)
            return;

        Vector2 input = move.action.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.0001f)
            return;

        Vector3 delta;

        if (relativeToCamera != null)
        {
            // Project camera forward/right onto the XY plane (2D/top-down).
            Vector3 right = relativeToCamera.transform.right;
            Vector3 up = relativeToCamera.transform.up;
            right.z = 0f;
            up.z = 0f;
            right.Normalize();
            up.Normalize();

            delta = (right * input.x + up * input.y) * (moveSpeedUnitsPerSecond * Time.deltaTime);
        }
        else
        {
            delta = new Vector3(input.x, input.y, 0f) * (moveSpeedUnitsPerSecond * Time.deltaTime);
        }

        transform.position += delta;
    }
}