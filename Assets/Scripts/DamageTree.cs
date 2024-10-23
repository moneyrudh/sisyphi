using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DamageTree : NetworkBehaviour
{
    private int maxHealth = 4;
    private NetworkVariable<int> health = new NetworkVariable<int>();
    private NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>();

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
        transform.Find("Trees").gameObject.SetActive(false);
        transform.Find("Remains").gameObject.SetActive(true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("FarmPoint"))
        {
            Debug.Log("FarmPoint in range");
        }
    }
}
