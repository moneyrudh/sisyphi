using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DamageTree : NetworkBehaviour
{
    [SerializeField] GameObject onDestroyedParticlesGO;
    private TileSetter tileSetter;
    private int maxHealth = 4;
    private NetworkVariable<int> health = new NetworkVariable<int>();
    private NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>();

    public void Start()
    {
        tileSetter = FindObjectOfType<TileSetter>();
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
        if (isDestroyed.Value) return;

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
        GameObject gameObject = Instantiate(onDestroyedParticlesGO, new(transform.position.x, transform.position.y + 3f, transform.position.z), Quaternion.identity);
        // transform.Find("Remains").gameObject.SetActive(true);
        Destroy(gameObject, 5f);
        GameObject trees = transform.Find("Trees").gameObject;
        foreach (Transform child in trees.transform)
        {
            int logCount = child.gameObject.GetComponent<LogCount>().count;
            int logIndex = logCount - 1;
            GameObject log = Instantiate(tileSetter.logs[logIndex].log, child.position, Quaternion.identity);
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
}
