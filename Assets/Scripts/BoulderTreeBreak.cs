using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BoulderTreeBreak : NetworkBehaviour
{
    [SerializeField]
    private GameObject particlesPrefab;

    private NetworkVariable<bool> isBroken = new NetworkVariable<bool>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            isBroken.Value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BreakTreeServerRpc()
    {
        if (isBroken.Value) return;

        isBroken.Value = true;
        BreakTreeClientRpc();
        // gameObject.SetActive(false);
    }

    [ClientRpc]
    private void BreakTreeClientRpc()
    {
        SpawnParticlesAndDestroy();
    }

    private void SpawnParticlesAndDestroy()
    {
        Destroy(gameObject, 0.5f);
    }

    private void OnDestroy()
    {
        if (particlesPrefab != null)
        {
            Vector3 particlePos = new(transform.position.x, transform.position.y + 2.5f, transform.position.z);
            GameObject particles = Instantiate(particlesPrefab, particlePos, Quaternion.identity);
            Destroy(particles, 5f);
        }
    }
}
