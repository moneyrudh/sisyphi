using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WaterSplash : NetworkBehaviour
{
    [SerializeField] private GameObject waterSplash;

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Boulder") || collider.CompareTag("Player"))
        {
            CreateWaterSplashClientRpc(collider.transform.position, collider.CompareTag("Player"));
        }
    }

    [ClientRpc]
    private void CreateWaterSplashClientRpc(Vector3 position, bool isPlayer)
    {
        GameObject waterSplashGO = Instantiate(waterSplash, position, Quaternion.identity);
        SoundManager.Instance.PlayAtPosition(isPlayer ? "PlayerSplash" : "BoulderSplash", position);
        Destroy(waterSplashGO, 4f);
    }
}
