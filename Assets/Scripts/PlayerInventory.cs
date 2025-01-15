using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] public NetworkVariable<int> wood = new NetworkVariable<int>();
    private bool collecting;
    private float collectionCooldown = 0.1f;
    private float lastCollectionTime;
    private HashSet<GameObject> processedWood;
    // Start is called before the first frame update
    void Awake()
    {
        collecting = false;
        processedWood = new HashSet<GameObject>();
        lastCollectionTime = -collectionCooldown;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            wood.Value = 0;
        }
        if (IsOwner) PlayerHUD.Instance.SetWood(wood.Value);
    }

    public void AddWood(int amount)
    {
        if (IsServer)
        {
            wood.Value += amount;
        }
        else
        {
            AddWoodServerRpc(amount);
        }
        collecting = false;
    }

    [ServerRpc]
    private void AddWoodServerRpc(int amount)
    {
        wood.Value += amount;
    }

    public void RemoveWood(int amount)
    {
        if (IsOwner) RemoveWoodServerRpc(amount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveWoodServerRpc(int amount)
    {
        wood.Value = Mathf.Max(0, wood.Value - amount);
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;

        if (processedWood.Contains(collision.gameObject)) return;

        if (Time.time - lastCollectionTime < collectionCooldown) return;

        if (collecting) return;

        if (collision.gameObject.CompareTag("Wood"))
        {
            collecting = true;
            processedWood.Add(collision.gameObject);
            LogCount logCount = collision.gameObject.GetComponent<LogCount>();
            int count = logCount.count;
            lastCollectionTime = Time.time;

            NetworkObject netObj = collision.gameObject.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                CollectWoodServerRpc(netObj.NetworkObjectId, count);
            }
            // StartCoroutine(ProcessWood(count, collision.gameObject));
            PlayerHUD.Instance.SetWood(wood.Value);
            collecting = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CollectWoodServerRpc(ulong networkId, int woodAmount)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject netObj))
        {
            wood.Value += woodAmount;

            netObj.Despawn();
            Destroy(netObj.gameObject);
        }
    }

    private IEnumerator ProcessWood(int count, GameObject wood)
    {
        AddWood(count);
        yield return new WaitForEndOfFrame();
        processedWood.Remove(wood);
        Destroy(wood);
    }

    private void OnDisable()
    {
        processedWood.Clear();
    }
}
