using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DamageTree : NetworkBehaviour
{
    [SerializeField] GameObject onDestroyedParticlesGO;
    private List<GameObject> spawnedParticlesGO;
    private TileSetter tileSetter;
    private int maxHealth = 4;
    private NetworkVariable<int> health = new NetworkVariable<int>();
    private NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>();

    public void Start()
    {
        tileSetter = FindObjectOfType<TileSetter>();
        spawnedParticlesGO = new List<GameObject>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
        {
            health.Value = maxHealth;
            isDestroyed.Value = false;
        }

        isDestroyed.OnValueChanged += OnIsDestroyedChanged;
    }

    public override void OnNetworkDespawn()
    {
        isDestroyed.OnValueChanged -= OnIsDestroyedChanged;
        base.OnNetworkDespawn();
    }

    public void Damage()
    {
        if (!IsServer) return;
        if (isDestroyed.Value || health.Value < 0) return;

        Debug.Log("Tree Damaged. Current Health: " + health.Value);
        health.Value --;
        if (health.Value <= 0)
        {
            isDestroyed.Value = true;
            // OnTreeDestroyed();
        }
    }

    private void OnIsDestroyedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            HandleTreeDestruction();
        }
    }

    private void HandleTreeDestruction()
    {
        // Visual effects and sound (runs on all clients)
        GameObject particlesGO = Instantiate(onDestroyedParticlesGO, 
            new(transform.position.x, transform.position.y + 2.5f, transform.position.z), 
            Quaternion.identity);
        spawnedParticlesGO.Add(particlesGO);
        Destroy(particlesGO, 5f);

        SoundManager.Instance.PlayAtPosition("TreeBreak", transform.position);
        SoundManager.Instance.PlayAtPosition("Explosion", transform.position);

        // Only the server spawns logs
        if (IsServer)
        {
            SpawnLogsServerRpc();
        }

        // Disable trees on all clients
        GameObject trees = transform.Find("Trees").gameObject;
        if (trees != null)
        {
            trees.SetActive(false);
        }
    }

    [ServerRpc]
    private void SpawnLogsServerRpc()
    {
        GameObject trees = transform.Find("Trees").gameObject;
        if (trees == null) return;

        foreach (Transform child in trees.transform)
        {
            LogCount logCount = child.gameObject.GetComponent<LogCount>();
            int count = logCount.count;
            int logIndex = count - 1;
            
            // Spawn the log as a network object
            GameObject logPrefab = tileSetter.logs[logIndex].log;
            GameObject log = Instantiate(logPrefab, child.position, Quaternion.identity);
            
            // Add LogCount before spawning
            
            // Spawn on network
            NetworkObject netObj = log.GetComponent<NetworkObject>();
            netObj.Spawn();

            AddLogCountClientRpc(new NetworkObjectReference(log), count);
        }
    }

    [ClientRpc]
    private void AddLogCountClientRpc(NetworkObjectReference logRef, int count)
    {
        if (!logRef.TryGet(out NetworkObject logObj)) return;

        logObj.gameObject.AddComponent<LogCount>().count = count * 3;
    } 

    private void OnTreeDestroyed()
    {
        GameObject particlesGO = Instantiate(onDestroyedParticlesGO, new(transform.position.x, transform.position.y + 2.5f, transform.position.z), Quaternion.identity);
        // transform.Find("Remains").gameObject.SetActive(true);
        SoundManager.Instance.PlayAtPosition("TreeBreak", transform.position);
        SoundManager.Instance.PlayAtPosition("Explosion", transform.position);
        spawnedParticlesGO.Add(particlesGO);
        Destroy(particlesGO, 5f);
        GameObject trees = transform.Find("Trees").gameObject;
        if (trees == null) return;
        foreach (Transform child in trees.transform)
        {
            LogCount logCount = child.gameObject.GetComponent<LogCount>();
            int count = logCount.count;
            int logIndex = count - 1;
            GameObject log = Instantiate(tileSetter.logs[logIndex].log, child.position, Quaternion.identity);
            count *= 3;
            log.AddComponent<LogCount>().count = count;
            child.gameObject.SetActive(false);
        }
        transform.Find("Trees").gameObject.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (isDestroyed.Value) return;
        if (other.CompareTag("FarmPoint"))
        {
            Debug.Log("FarmPoint in range");
        }
    }

    private void OnDestroy()
    {
        if (spawnedParticlesGO == null) return;
        foreach (GameObject go in spawnedParticlesGO)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }
    }
}
