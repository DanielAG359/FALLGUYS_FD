using Unity.Netcode;
using UnityEngine;

public class SpeedPowerUp : NetworkBehaviour
{
    [Header("Boost")]
    [SerializeField] private float boostedSpeed = 14f;
    [SerializeField] private float duration = 5f;

    private bool used = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (used) return;

        PlayerController player =
            other.GetComponent<PlayerController>();

        if (player == null) return;

        used = true;

        player.SetSpeedBoost(boostedSpeed, duration);

        Debug.Log("SPEED BOOST");

        GetComponent<NetworkObject>().Despawn();
    }
}