using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float acceleration = 45f;
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float airControl = 0.4f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float jumpCooldown = 0.25f;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Respawn")]
    [SerializeField] private float killHeight = -15f;

    [Header("Interpolation")]
    [SerializeField] private float interpolationSpeed = 15f;

    private Rigidbody rb;

    [SerializeField] private GameObject playerCamera;
    private Camera cam;

    // INPUT server
    private Vector3 serverInput;
    private bool serverJump;

    // INPUT cliente
    private Vector3 lastSentInput;
    //private bool lastJumpSent;
    private bool grounded;
    private float lastJumpTime;
    private Vector3 spawnPoint;
    private bool finished = false;

    // NETWORK STATE
    struct PlayerState : INetworkSerializable
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
        }
    }

    private NetworkVariable<PlayerState> netState = new NetworkVariable<PlayerState>(
            writePerm: NetworkVariableWritePermission.Server
        );

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        spawnPoint = transform.position;

        if (!IsServer)
        {
            rb.isKinematic = true;
        }

        if (IsOwner)
        {
            playerCamera.SetActive(true);

            cam = playerCamera.GetComponentInChildren<Camera>();
        }
        else
        {
            playerCamera.SetActive(false);
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            HandleInput();
        }

        if (!IsServer)
        {
            ApplyInterpolation();
        }
    }

    private void HandleInput()
    {
        if (!GameManager.Instance.GameStarted.Value)
            return;

        float h = 0f;
        float v = 0f;

        if (Keyboard.current.aKey.isPressed) h -= 1;
        if (Keyboard.current.dKey.isPressed) h += 1;
        if (Keyboard.current.wKey.isPressed) v += 1;
        if (Keyboard.current.sKey.isPressed) v -= 1;

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 input = (camForward * v + camRight * h).normalized;

        // MOVIMIENTO
        if (input != lastSentInput)
        {
            lastSentInput = input;

            SendInputServerRpc(input, false);
        }

        // SALTO
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SendInputServerRpc(input, true);
            Debug.Log("JUMP RPC");
        }
    }

    [ServerRpc]
    private void SendInputServerRpc(Vector3 input, bool jump, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        serverInput = input;
        serverJump = jump;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (!GameManager.Instance.GameStarted.Value) return;

        CheckGround();

        ApplyMovement();
        ApplyRotation();
        ApplyJump();
        ApplyDeceleration();
        LimitSpeed();
        CheckRespawn();

        netState.Value = new PlayerState
        {
            Position = rb.position,
            Rotation = rb.rotation
        };

        // reset salto (solo 1 frame)
        serverJump = false;
    }

    private void CheckGround()
    {
        float radius = 0.35f;     // ajusta al radio del capsule collider
        float distance = 1.1f;    // ajusta seg�n altura

        Vector3 origin = transform.position + Vector3.up * 0.1f;

        grounded = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out RaycastHit hit,
            distance,
            groundLayer
        );
    }

    private void ApplyMovement()
    {
        if (serverInput.sqrMagnitude <= 0) return;

        float control = grounded ? 1f : airControl;

        Vector3 force = serverInput * acceleration * control;

        rb.AddForce(force, ForceMode.Acceleration);
    }

    private void ApplyRotation()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (horizontalVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);

            Quaternion smoothRotation = Quaternion.Slerp( rb.rotation, targetRotation, 10f * Time.fixedDeltaTime);

            rb.MoveRotation(smoothRotation);
        }
    }

    private void ApplyJump()
    {
        if (!serverJump) return;

        if (grounded && Time.time > lastJumpTime + jumpCooldown)
        {
            lastJumpTime = Time.time;

            // reset velocidad vertical para salto consistente

            Vector3 vel = rb.linearVelocity;
            vel.y = 0;
            rb.linearVelocity = vel;

            rb.AddForce( Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log("APPLY JUMP");
        }
    }

    private void ApplyDeceleration()
    {
        if (!grounded) return;

        Vector3 horizontalVelocity =
            new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (serverInput.sqrMagnitude == 0 && horizontalVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 brakeForce = -horizontalVelocity * deceleration;

            rb.AddForce( brakeForce, ForceMode.Acceleration);
        }
    }

    private void LimitSpeed()
    {
        Vector3 horizontalVelocity = new Vector3( rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (horizontalVelocity.magnitude > maxSpeed)
        {
            Vector3 limited = horizontalVelocity.normalized * maxSpeed;

            rb.linearVelocity = new Vector3( limited.x, rb.linearVelocity.y, limited.z);
        }
    }

    private void ApplyInterpolation()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            netState.Value.Position,
            Time.deltaTime * interpolationSpeed
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            netState.Value.Rotation,
            Time.deltaTime * interpolationSpeed
        );
    }

    private void CheckRespawn()
    {
        if (transform.position.y < killHeight) Respawn();
        
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector3.zero;

        transform.position = spawnPoint;
    }

    public void Hit(Vector3 force)
    {
        if (!IsServer) return;

        rb.AddForce(force, ForceMode.Impulse);
    }

    public void ReachGoal()
    {
        if (finished) return;

        finished = true;

        GameManager.Instance.PlayerFinished(this);
    }
}