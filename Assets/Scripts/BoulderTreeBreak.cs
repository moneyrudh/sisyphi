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
    public void BreakTreeServerRpc(float waitForSeconds)
    {
        if (isBroken.Value) return;

        isBroken.Value = true;
        BreakTreeClientRpc(waitForSeconds);
        // gameObject.SetActive(false);
    }

    [ClientRpc]
    private void BreakTreeClientRpc(float waitForSeconds)
    {
        SpawnParticlesAndDestroy(waitForSeconds);
    }

    private void SpawnParticlesAndDestroy(float waitForSeconds)
    {
        // Destroy(gameObject, 0.5f);
        StartCoroutine(SpawnParticlesCoroutine(waitForSeconds));
    }

    private IEnumerator SpawnParticlesCoroutine(float waitForSeconds)
    {
        yield return new WaitForSeconds(waitForSeconds);
        gameObject.SetActive(false);
        if (particlesPrefab != null)
        {
            Vector3 particlePos = new(transform.position.x, transform.position.y + 2.5f, transform.position.z);
            GameObject particles = Instantiate(particlesPrefab, particlePos, Quaternion.identity);
            SoundManager.Instance.PlayAtPosition("TreeBreak", transform.position);
            SoundManager.Instance.PlayAtPosition("Explosion", transform.position);
            Destroy(particles, 2.5f);
        }
    }

    private void OnDestroy()
    {
        
    }
}
