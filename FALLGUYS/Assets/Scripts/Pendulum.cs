using Unity.Netcode;
using UnityEngine;

public class Pendulum : NetworkBehaviour
{
    public float speed = 1.5f;
    public float limit = 75f;

    private void Update()
    {
        if (!IsServer) return;

        float angle =
            limit * Mathf.Sin(Time.time * speed);

        transform.localRotation =
            Quaternion.Euler(0, 0, angle);
    }
}