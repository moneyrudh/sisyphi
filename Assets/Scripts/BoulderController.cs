using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class BoulderController : NetworkBehaviour
{
    private NetworkRigidbody networkRigidbody;
    private Rigidbody rb;
    public int restingMass = 20;
    public int pushingMass = 7;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
    }

    // Call this when a player's collider enters the boulder's trigger zone
    public void OnPlayerApproach(NetworkObject playerObject)
    {
        if (!playerObject.IsOwner) return;
        rb.mass = pushingMass;
        // Request ownership of the boulder
        // RequestBoulderOwnershipServerRpc(playerObject.OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBoulderOwnershipServerRpc(ulong requestingPlayerId)
    {
        // Transfer ownership to the requesting player
        NetworkObject.ChangeOwnership(requestingPlayerId);
    }

    // Optional: Release ownership when player moves away
    public void OnPlayerLeave(NetworkObject playerObject)
    {
        if (!playerObject.IsOwner) return;
        rb.mass = restingMass;
        // if (NetworkObject.OwnerClientId == playerObject.OwnerClientId)
        // {
        //     ReleaseBoulderOwnershipServerRpc();
        // }
    }

    [ServerRpc]
    private void ReleaseBoulderOwnershipServerRpc()
    {
        // Transfer ownership back to the server
        NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
    }
}