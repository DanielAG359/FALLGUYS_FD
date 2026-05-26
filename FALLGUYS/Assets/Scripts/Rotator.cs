using Unity.Netcode;
using UnityEngine;

public class Rotator : NetworkBehaviour
{
    [SerializeField] private float speed = 100;
    [SerializeField] private float direction = 90;

    private float currentAngle;

    void Update()
    {
        if (!IsServer) return;

        if (!GameManager.Instance.GameStarted.Value)
            return;

        currentAngle += speed * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(direction, currentAngle, 0);
    }
}