using Unity.Netcode;
using UnityEngine;

public class WallMovable : NetworkBehaviour
{
    public float height = 4f;
    public float speed = 2f;

    private Vector3 startPos;

    void Awake()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (!IsServer) return;

        float y =
            Mathf.PingPong(Time.time * speed, height);

        transform.position =
            startPos + Vector3.up * y;
    }
}