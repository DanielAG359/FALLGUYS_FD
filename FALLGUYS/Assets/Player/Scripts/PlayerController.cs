using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour {
    [Header("Settings")]
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float interpolationSpeed = 15f;

    private Rigidbody _rb;

    // 1. Definimos el estado que viaja del Servidor -> Clientes
    struct PlayerState : INetworkSerializable {
        public Vector3 Position;
        public Quaternion Rotation;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
        }
    }

    private NetworkVariable<PlayerState> _netState = new NetworkVariable<PlayerState>(
        writePerm: NetworkVariableWritePermission.Server
    );

    // 2. Variable para almacenar el input en el servidor
    private Vector3 _serverInput;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn() {
        // Si no somos el servidor, el Rigidbody no debe interferir con la posición
        if (!IsServer) {
            _rb.isKinematic = true;
        }
    }

    private void Update() {
        if (IsOwner) {
            // CLIENTE: Captura y envía
            float h = Keyboard.current.aKey.IsPressed() ? -1 : 0 + (Keyboard.current.dKey.IsPressed() ? 1 : 0); // A/D
            float v = Keyboard.current.sKey.IsPressed() ? -1 : 0 + (Keyboard.current.wKey.IsPressed() ? 1 : 0); // W/S

            Vector3 moveInput = new Vector3(h, 0, v);
            MoveServerRpc(moveInput.normalized);
        }

        if (!IsServer) {
            // CLIENTE: Suaviza la posición recibida del servidor
            ApplySmoothMovement();
        }
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 input) {
        // SERVIDOR: Guarda el input para el siguiente FixedUpdate
        _serverInput = input;
    }

    private void FixedUpdate() {
        if (!IsServer) return;


        ApplyMovement();
        ApplyDeceleration();
        LimitSpeed();

        // SERVIDOR: Publica el resultado para todos
        _netState.Value = new PlayerState {
            Position = _rb.position,
            Rotation = _rb.rotation
        };
    }

    private void ApplySmoothMovement() {
        transform.position = Vector3.Lerp(transform.position, _netState.Value.Position, Time.deltaTime * interpolationSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _netState.Value.Rotation, Time.deltaTime * interpolationSpeed);
    }

    void ApplyMovement() {
        if (_serverInput.sqrMagnitude > 0) {
            // SERVIDOR: Aplica físicas reales
            Vector3 force = _serverInput * acceleration;
            _rb.AddForce(force, ForceMode.Acceleration);
        }
    }

    private void ApplyDeceleration() {
        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);

        if (_serverInput.sqrMagnitude == 0 && horizontalVelocity.sqrMagnitude > 0.01f) {
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

}