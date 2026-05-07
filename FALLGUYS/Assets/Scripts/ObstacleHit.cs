using Unity.Netcode;
using UnityEngine;

public class ObstacleHit : NetworkBehaviour
{
    [SerializeField] private Vector3 force = new Vector3(0, 5, 10);

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        PlayerController player =
            collision.gameObject.GetComponent<PlayerController>();

        if (player != null)
        {
            Vector3 dir =
                (collision.transform.position - transform.position).normalized;

            player.Hit(dir * force.z + Vector3.up * force.y);
        }
    }
}