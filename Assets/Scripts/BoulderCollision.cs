using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BoulderCollision : NetworkBehaviour
{
    [SerializeField]
    private float breakVelocityThreshold = 0.5f;
    private Rigidbody rb;
    private NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Tree"))
        {
            Debug.Log("TREE HIT AT VELOCITY " + rb.velocity.magnitude);
            float vel = Mathf.Sqrt(Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2));
            Debug.Log("BOULDER VELOCITY Z + X" + vel);
            switch (GetComponent<BoulderController>().GetBoulderProperties().boulderSize)
            {
                case BoulderSize.Small:
                    return;
                case BoulderSize.Medium:
                    {
                        if (rb.velocity.magnitude > breakVelocityThreshold && collision.gameObject.TryGetComponent<BoulderTreeBreak>(out var tree))
                        {
                            tree.BreakTreeServerRpc(0.5f);
                        }
                    }
                    break;
                case BoulderSize.Large:
                    {
                        if (collision.gameObject.TryGetComponent<BoulderTreeBreak>(out var tree))
                        {
                            tree.BreakTreeServerRpc(0f);
                        }           
                    }
                    break;
                default:
                    return;
            }
        }
    }
}