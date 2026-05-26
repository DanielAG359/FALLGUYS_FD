using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform pivot;
    [SerializeField] private Camera cam;

    [Header("Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float followSpeed = 12f;

    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 60f;

    [SerializeField] private float distance = 4f;

    private float yaw;
    private float pitch;

    private bool gameplayActive = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            cam.gameObject.SetActive(false);
            enabled = false;
            return;
        }
        
        cam.gameObject.SetActive(true);

        AudioListener listener = cam.GetComponent<AudioListener>();

        if (listener != null)
        {
            listener.enabled = IsOwner;
        }
        // LOBBY = cursor libre
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (!IsOwner) return;

        if (!gameplayActive)
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.GameStarted.Value)
            {
                gameplayActive = true;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                return;
            }
        }

        FollowTarget();
        RotateCamera();
    }

    void FollowTarget()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            target.position,
            Time.deltaTime * followSpeed
        );
    }

    void RotateCamera()
    {
        Vector2 mouse = Mouse.current.delta.ReadValue();

        yaw += mouse.x * mouseSensitivity;
        pitch -= mouse.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0, yaw, 0);

        pivot.localRotation = Quaternion.Euler(pitch, 0, 0);

        cam.transform.localPosition = new Vector3(0, 0, -distance);
    }
}