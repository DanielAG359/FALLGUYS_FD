using Unity.Netcode;
using UnityEngine;

public class Bounce : NetworkBehaviour
{
    public float force = 12f;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        PlayerController player =
            collision.gameObject.GetComponent<PlayerController>();

        if (player == null) return;

        Vector3 dir =
            (collision.transform.position - transform.position)
            .normalized;

        player.Hit(dir * force + Vector3.up * 5f);
    }
}