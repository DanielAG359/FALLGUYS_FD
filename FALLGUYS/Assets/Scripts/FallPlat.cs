using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class FallPlat : NetworkBehaviour
{
    public float fallDelay = 1f;

    private Rigidbody rb;

    private bool activated = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (activated) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            activated = true;

            StartCoroutine(Fall());
        }
    }

    IEnumerator Fall()
    {
        yield return new WaitForSeconds(fallDelay);

        rb.isKinematic = false;
    }
}