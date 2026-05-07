using Unity.Netcode;
using UnityEngine;

public class MovableObs : NetworkBehaviour
{
    public float distance = 5f;
    public bool horizontal = true;
    public float speed = 3f;

    private bool forward = true;
    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        if (!IsServer) return;

        Vector3 dir =
            horizontal ? Vector3.right : Vector3.forward;

        if (forward)
        {
            transform.position +=
                dir * speed * Time.deltaTime;

            if (Vector3.Distance(startPos, transform.position)
                >= distance)
            {
                forward = false;
            }
        }
        else
        {
            transform.position -=
                dir * speed * Time.deltaTime;

            if (Vector3.Distance(startPos, transform.position)
                <= 0.1f)
            {
                forward = true;
            }
        }
    }
}