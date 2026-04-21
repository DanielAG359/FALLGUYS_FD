using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour {
    [Header("Movement")]
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float deceleration = 12f;
    [SerializeField] private float airControl = 0.4f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Interpolation")]
    [SerializeField] private float interpolationSpeed = 15f;

    private Rigidbody _rb;

    struct PlayerState : INetworkSerializable{
        public Vector3 Position;
        public Quaternion Rotation;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
        }
    }

    private NetworkVariable<PlayerState> _netState = new NetworkVariable<PlayerState>(
        writePerm: NetworkVariableWritePermission.Server
    );

    // INPUT servidor
    private Vector3 _serverInput;
    private bool _serverJump;

    // CLIENTE control
    private Vector3 _lastSentInput;
    private bool _lastJumpSent;

    private bool _isGrounded;
    private float _lastJumpTime;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn() {
        if (!IsServer)
            _rb.isKinematic = true;
    }

    private void Update() {
        if (IsOwner)
            HandleInput();

        if (!IsServer) {
            ApplySmoothMovement();
        }
    }

    private void HandleInput() {
        float h = 0f;
        float v = 0f;

        if (Keyboard.current.aKey.isPressed) h -= 1;
        if (Keyboard.current.dKey.isPressed) h += 1;
        if (Keyboard.current.sKey.isPressed) v -= 1;
        if (Keyboard.current.wKey.isPressed) v += 1;

        Vector3 input = new Vector3(h, 0, v).normalized;
        bool jump = Keyboard.current.spaceKey.wasPressedThisFrame;

        if (input != _lastSentInput || jump != _lastJumpSent)
        {
            _lastSentInput = input;
            _lastJumpSent = jump;
            MoveServerRpc(input, jump);
        }
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 input, bool jump, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
        _serverInput = input;
        _serverJump = jump;
    }

    private void FixedUpdate() {
        if (!IsServer) return;
        CheckGround();

        ApplyMovement();
        ApplyRotation();
        ApplyJump();
        ApplyDeceleration();
        LimitSpeed();

        _netState.Value = new PlayerState {
            Position = _rb.position,
            Rotation = _rb.rotation
        };
        // reset salto (solo 1 frame)
        _serverJump = false;
    }

    private void CheckGround() {
        float radius = 0.35f;     // ajusta al radio del capsule collider
        float distance = 1.1f;    // ajusta según altura

        Vector3 origin = transform.position + Vector3.up * 0.1f;

        _isGrounded = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out RaycastHit hit,
            distance,
            groundLayer
        );
    }

    private void ApplyMovement() {
        if (_serverInput.sqrMagnitude <= 0) return;
        float control = _isGrounded ? 1f : airControl;

        Vector3 force = _serverInput * acceleration * control;
        _rb.AddForce(force, ForceMode.Acceleration);
    }

    private void ApplyRotation() {
        if (_serverInput.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_serverInput);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, 10f * Time.fixedDeltaTime));
        }
    }

    private void ApplyJump() {
        if (!_serverJump) return;

        if (_isGrounded && Time.time > _lastJumpTime + jumpCooldown)
        {
            _lastJumpTime = Time.time;

            // reset velocidad vertical para salto consistente
            Vector3 vel = _rb.linearVelocity;
            vel.y = 0;
            _rb.linearVelocity = vel;

            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void ApplyDeceleration() {
        if (!_isGrounded) return;
        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);

        if (_serverInput.sqrMagnitude == 0 && horizontalVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 brakeForce = -horizontalVelocity * deceleration;
            _rb.AddForce(brakeForce, ForceMode.Acceleration);
        }
    }

    private void LimitSpeed() {
        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);

        if (horizontalVelocity.magnitude > maxSpeed) {
            Vector3 limitedVel = horizontalVelocity.normalized * maxSpeed;
            _rb.linearVelocity = new Vector3(limitedVel.x, _rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void ApplySmoothMovement() {
        transform.position = Vector3.Lerp(
            transform.position,
            _netState.Value.Position,
            Time.deltaTime * interpolationSpeed
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _netState.Value.Rotation,
            Time.deltaTime * interpolationSpeed
        );
    }
}