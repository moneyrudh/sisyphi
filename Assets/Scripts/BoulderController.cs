using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System;
using System.Collections.Generic;

[System.Serializable]
public enum BoulderSize {
    Small,
    Medium,
    Large
}

[System.Serializable]
public class BoulderProperties : INetworkSerializable, IEquatable<BoulderProperties> {
    public float pushingMass;
    public BoulderSize boulderSize;
    public float localScale;

    public bool Equals(BoulderProperties properties)
    {
        if (pushingMass == properties.pushingMass &&
            boulderSize == properties.boulderSize &&
            localScale == properties.localScale) return true;
        return false;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref pushingMass);
        serializer.SerializeValue(ref boulderSize);
        serializer.SerializeValue(ref localScale);
    }
}

public class BoulderController : NetworkBehaviour
{
    private NetworkRigidbody networkRigidbody;
    private Rigidbody rb;
    public int restingMass = 20;
    public int pushingMass = 7;
    public List<BoulderProperties> boulderPropertiesList;
    private NetworkVariable<BoulderProperties> netBoulderProperties = new NetworkVariable<BoulderProperties>();
    public BoulderProperties currentBoulderProperties => netBoulderProperties.Value;
    private NetworkVariable<bool> isPlayerPushing = new NetworkVariable<bool>(false);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            SetBoulderProperties(BoulderSize.Medium);
            netBoulderProperties.Value = boulderPropertiesList[1];
        }
    }

    // Call this when a player's collider enters the boulder's trigger zone
    public void OnPlayerApproach(NetworkObject playerObject)
    {
        if (!playerObject.IsOwner) return;
        rb.mass = currentBoulderProperties.pushingMass;
        SetPlayerPushingServerRpc(true);
        // Request ownership of the boulder
        // RequestBoulderOwnershipServerRpc(playerObject.OwnerClientId);
    }

    [ServerRpc]
    private void SetPlayerPushingServerRpc(bool pushing)
    {
        isPlayerPushing.Value = pushing;
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
        SetPlayerPushingServerRpc(false);
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

    // public BoulderProperties GetBoulderProperties()
    // {
    //     return currentBoulderProperties;
    // }

    public void SetBoulderProperties(BoulderSize boulderSize)
    {
        Debug.Log($"[{gameObject.name}] Setting boulder properties on {(IsServer ? "Server" : "Client")}, IsOwner: {IsOwner}");

        if (!IsServer) return;

        BoulderProperties boulderProperties = new BoulderProperties();
        switch (boulderSize)
        {
            case BoulderSize.Small:
            {
                boulderProperties = boulderPropertiesList[0];
            }
            break;
            case BoulderSize.Medium:
            {
                boulderProperties = boulderPropertiesList[1];
            }
            break;
            case BoulderSize.Large:
            {
                boulderProperties = boulderPropertiesList[2];
            }
            break;
        }
        Debug.Log($"[{gameObject.name}] Properties set, about to apply...");
        netBoulderProperties.Value = boulderProperties;

        ApplyBoulderPropertiesClientRpc(boulderProperties);
    }

    [ClientRpc]
    private void ApplyBoulderPropertiesClientRpc(BoulderProperties properties)
    {
        Debug.Log($"[{gameObject.name}] Applying properties on {(IsServer ? "Server" : "Client")}, Current scale: {transform.localScale}, Target scale: {properties.localScale}");
        string log = @"
            Applying the following Boulder Properties to Boulder " + gameObject.name + @":\n
            Pushing Mass: " + properties.pushingMass + @"\n 
            Boulder Size: " + properties.boulderSize.ToString() + @"\n
            Local Scale: " + properties.localScale 
        ;
        Debug.Log(log);
        float scale = properties.localScale;
        float localScale = scale;
        transform.localScale = new (scale, scale, scale);
        if (isPlayerPushing.Value) rb.mass = properties.pushingMass;
        Debug.Log($"[{gameObject.name}] Scale applied, new scale: {transform.localScale}");
    }

    public BoulderProperties GetBoulderProperties()
    {
        return netBoulderProperties.Value;
    }
}