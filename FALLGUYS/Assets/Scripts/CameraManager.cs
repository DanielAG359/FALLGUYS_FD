using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    public float followSpeed = 8f;
    public float mouseSpeed = 2f;
    public float cameraDist = 3f;

    public Transform target;

    private Transform pivot;
    private Transform camTrans;

    public float minAngle = -35f;
    public float maxAngle = 35f;

    private float lookAngle;
    private float tiltAngle;

    private bool initialized = false;

    void Start()
    {
        Invoke(nameof(InitializeCamera), 0.5f);
    }

    void InitializeCamera()
    {
        Camera cam = GetComponentInChildren<Camera>();

        if (cam == null)
        {
            Debug.LogError("No camera found");
            return;
        }

        camTrans = cam.transform;
        pivot = camTrans.parent;

        PlayerController[] players =
            FindObjectsOfType<PlayerController>();

        foreach (PlayerController player in players)
        {
            if (player.IsOwner)
            {
                target = player.transform;
                initialized = true;
                break;
            }
        }
    }

    void FixedUpdate()
    {
        if (!initialized || target == null)
            return;

        float h = Mouse.current.delta.ReadValue().x * 0.01f;
        float v = Mouse.current.delta.ReadValue().y * 0.01f;

        FollowTarget(Time.deltaTime);

        HandleRotations(Time.deltaTime, v, h);
    }

    void FollowTarget(float delta)
    {
        Vector3 targetPosition =
            Vector3.Lerp(
                transform.position,
                target.position,
                delta * followSpeed
            );

        transform.position = targetPosition;
    }

    void HandleRotations(float delta, float v, float h)
    {
        lookAngle += h * mouseSpeed;

        transform.rotation =
            Quaternion.Euler(0, lookAngle, 0);

        tiltAngle -= v * mouseSpeed;

        tiltAngle = Mathf.Clamp(
            tiltAngle,
            minAngle,
            maxAngle
        );

        pivot.localRotation =
            Quaternion.Euler(tiltAngle, 0, 0);
    }

    void LateUpdate()
    {
        if (!initialized || camTrans == null)
            return;

        float dist = cameraDist;

        Ray ray = new Ray(
            pivot.position,
            camTrans.position - pivot.position
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, cameraDist + 1f))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                dist = hit.distance - 0.25f;
            }
        }

        dist = Mathf.Clamp(dist, 1f, cameraDist);

        camTrans.localPosition =
            new Vector3(0, 0, -dist);
    }
}