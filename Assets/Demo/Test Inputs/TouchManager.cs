using UnityEngine;
using UnityEngine.InputSystem;

namespace Mobile_Demo
{
    public class TouchManager : MonoBehaviour
    {
        [SerializeField] GameObject player;
        [SerializeField] bool useVirtualJoystick;
        [SerializeField] float moveSpeed;
        Vector2 moveInput;
        private PlayerInput playerInput;
        Vector2 newPos;
        bool isDragging;


        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
        }
#region SendMessages
        void OnTouchPosition(InputValue value)
        {
            if (useVirtualJoystick) return;
            
            // Test to verify a position input
            newPos = value.Get<Vector2>();
            Debug.Log(newPos);

            // Move the player as long as you are dragging on the screen
            Vector3 position = Camera.main.ScreenToWorldPoint(newPos);
            position.z = player.transform.position.z;
            player.transform.position = position;
        }

        void OnTouchPress(InputValue value)
        {
            if (useVirtualJoystick) return;

            // Test to verify a touch input
            float val = value.Get<float>();
            Debug.Log(val);

            // Move the object to the position of the touch
            Vector3 position = Camera.main.ScreenToWorldPoint(newPos);
            position.z = player.transform.position.z;
            player.transform.position = position;

        }

        void OnMove(InputValue value)
        {
            if (useVirtualJoystick)
            {
                moveInput = value.Get<Vector2>();
            }
        }

        void Update()
        {
            if (useVirtualJoystick)
            {
                Vector3 delta = new Vector3(moveInput.x, moveInput.y, 0f) * moveSpeed * Time.deltaTime;
                player.transform.position += delta;
            }
        }
        #endregion

        #region UnityEvents

        // Change the input type to allow us to determine the state of the input
        // i.e. canceled, performed, or started
        public void OnTouchPosition(InputAction.CallbackContext ctx)
        {
            if (isDragging)
            {
                // Move the player as long as you are dragging on the screen
                Vector3 position = Camera.main.ScreenToWorldPoint(ctx.ReadValue<Vector2>());
                position.z = player.transform.position.z;
                player.transform.position = position;
                player.GetComponent<SpriteRenderer>().color = Color.green;
                Debug.Log("Moving the player");
            }
        }

        public void OnTouchPress(InputAction.CallbackContext ctx)
        {
            //Delineate from performed, started, and cancelled actions
            if (ctx.started)
            {
                isDragging = true;
                Debug.Log("Position action started");
                player.GetComponent<SpriteRenderer>().color = Color.yellow;
            }
            else if (ctx.performed)
            {
                isDragging = true;
                player.GetComponent<SpriteRenderer>().color = Color.green;
                Debug.Log("Moving the player");
            }
            else if (ctx.canceled)
            {
                isDragging = false;
                player.GetComponent<SpriteRenderer>().color = Color.red;
                Debug.Log("Position action canceled. Trigger effects!");
            }
        }
#endregion
    }
}
