using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class BoulderController : NetworkBehaviour
{
    private Rigidbody rb;
    private NetworkVariable<Vector3> targetPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> targetVelocity = new NetworkVariable<Vector3>();
    public float syncThreshold = 0.1f;  // Only sync if position difference is greater than this
    public float smoothFactor = 10f;    // Higher = faster correction
    private Vector3 currentVelocity;
    public float smoothTime = 0.1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            targetPosition.Value = transform.position;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            // Server updates network position if boulder has moved significantly
            if (Vector3.Distance(targetPosition.Value, rb.position) > syncThreshold)
            {
                targetPosition.Value = rb.position;
                targetVelocity.Value = rb.velocity;
            }
        }
        else
        {
            // Clients smoothly correct any desync while maintaining physics simulation
            Vector3 positionError = targetPosition.Value - rb.position;
            if (positionError.magnitude > syncThreshold)
            {
                rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity.Value, Time.deltaTime * smoothFactor);
                rb.MovePosition(Vector3.Lerp(rb.position, targetPosition.Value, Time.deltaTime * smoothFactor));
            }
        }
    }

    public void MoveBoulder(Vector3 movement)
    {
        // Client and Host both just request the move
        RequestMoveServerRpc(movement);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestMoveServerRpc(Vector3 movement)
    {
        // Server applies the movement
        Vector3 newPosition = transform.position + movement;
        transform.position = newPosition;
        targetPosition.Value = newPosition;
        
        // Tell clients to update their positions
        UpdatePositionClientRpc(newPosition);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition)
    {
        // Only non-host clients need to update
        if (!IsServer)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                newPosition,
                ref currentVelocity,
                smoothTime
            );
        }
    }
}