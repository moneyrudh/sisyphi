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
        if (IsServer)
        {
            health.Value = maxHealth;
            isDestroyed.Value = false;
        }
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
            OnTreeDestroyed();
        }
    }

    private void OnTreeDestroyed()
    {
        GameObject particlesGO = Instantiate(onDestroyedParticlesGO, new(transform.position.x, transform.position.y + 2.5f, transform.position.z), Quaternion.identity);
        // transform.Find("Remains").gameObject.SetActive(true);
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
