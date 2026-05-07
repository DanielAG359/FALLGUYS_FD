using Unity.Netcode;
using UnityEngine;

public class Rotator : NetworkBehaviour
{
    public float speed = 100f;

    void Update()
    {
        if (!IsServer) return;

        transform.Rotate(
            0,
            0,
            speed * Time.deltaTime
        );
    }
}