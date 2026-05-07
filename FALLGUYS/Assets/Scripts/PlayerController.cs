using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float speed = 7f;
    public float jumpForce = 7f;
    public float airControl = 0.4f;

    [Header("Ground")]
    public LayerMask groundLayer;

    [Header("Respawn")]
    public float killHeight = -15f;

    private Rigidbody rb;

    private Vector3 moveInput;
    private bool jumpInput;

    private bool grounded;

    private Vector3 spawnPoint;

    private bool finished = false;  
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        spawnPoint = transform.position;
    }

    void Update()
    {
        if (!IsOwner) return;

        Vector2 movement = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            movement.y += 1;

        if (Keyboard.current.sKey.isPressed)
            movement.y -= 1;

        if (Keyboard.current.aKey.isPressed)
            movement.x -= 1;

        if (Keyboard.current.dKey.isPressed)
            movement.x += 1;

        Vector3 input =
            new Vector3(movement.x, 0, movement.y).normalized;

        bool jump =
            Keyboard.current.spaceKey.wasPressedThisFrame;

        SendInputServerRpc(input, jump);
    }

    [ServerRpc]
    private void SendInputServerRpc(Vector3 input, bool jump)
    {
        moveInput = input;
        jumpInput = jump;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (!GameManager.Instance.GameStarted.Value)
            return;

        CheckGround();

        Move();

        Jump();

        CheckRespawn();
    }

    private void Move()
    {
        Vector3 move = moveInput * speed;

        if (!grounded)
            move *= airControl;

        Vector3 velocity = rb.linearVelocity;

        rb.linearVelocity = new Vector3(
            move.x,
            velocity.y,
            move.z
        );

        if (moveInput.sqrMagnitude > 0.01f)
        {
            transform.rotation =
                Quaternion.LookRotation(moveInput);
        }
    }

    private void Jump()
    {
        if (jumpInput && grounded)
        {
            rb.AddForce(Vector3.up * jumpForce,
                ForceMode.Impulse);
        }

        jumpInput = false;
    }

    private void CheckGround()
    {
        grounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            1.2f,
            groundLayer
        );
    }

    private void CheckRespawn()
    {
        if (transform.position.y < killHeight)
        {
            Respawn();
        }
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