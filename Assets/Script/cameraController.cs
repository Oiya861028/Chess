using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 2f;
    private float mouseX, mouseY;
    private float rotationX, rotationY;
    private bool cameraControlEnabled = false;

    void Start()
    {
        // Start with cursor visible and unlocked for piece selection
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Toggle camera control with Left Shift
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            cameraControlEnabled = !cameraControlEnabled;
            Cursor.lockState = cameraControlEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !cameraControlEnabled;
        }
        // Reset camera to face black pawns
        if(Input.GetKeyDown(KeyCode.B))
        {
            transform.SetPositionAndRotation(new Vector3(4, 10, -3.5f), Quaternion.Euler(60f, 0f, 0f));
        }
        // Reset camera to face white pawns
        if(Input.GetKeyDown(KeyCode.R))
        {
            transform.SetPositionAndRotation(new Vector3(4, 10, 10.5f), Quaternion.Euler(60f, 180f, 0f));
        }
        // Only process camera movement when control is enabled
        if (cameraControlEnabled)
        {
            // Get mouse input
            mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // Calculate rotation
            rotationY += mouseX;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            // Apply rotation
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);

            // Movement
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            float upDownInput = 0;
            
            if (Input.GetKey(KeyCode.E)) upDownInput = 1;
            if (Input.GetKey(KeyCode.Q)) upDownInput = -1;

            Vector3 moveDirection = transform.right * horizontalInput + 
                                  transform.forward * verticalInput +
                                  transform.up * upDownInput;

            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }
}
