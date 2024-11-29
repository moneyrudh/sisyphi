using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BoulderCollision : NetworkBehaviour
{
    [SerializeField]
    private float breakVelocityThreshold = 5f;
    private Rigidbody rb;
    private NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (IsServer)
        {
            netPosition.Value = transform.position;
            netRotation.Value = transform.rotation;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, netPosition.Value, Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, netRotation.Value, Time.deltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Tree") && rb.velocity.magnitude > breakVelocityThreshold)
        {
            if (collision.gameObject.TryGetComponent<BoulderTreeBreak>(out var tree))
            {
                tree.BreakTreeServerRpc();
            }
        }
    }
}