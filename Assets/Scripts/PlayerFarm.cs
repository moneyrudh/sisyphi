using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerFarm : NetworkBehaviour
{
    private Animator animator;
    private float farmRange = 0.5f;
    [SerializeField] private LayerMask treeLayer;
    [SerializeField] private Transform farmPoint;

    private bool isFarming = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0) && !isFarming && Movement.isGrounded)
        {
            StartCoroutine(PerformFarmRoutine());
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(farmPoint.position, farmRange);
    }

    IEnumerator PerformFarmRoutine()
    {
        isFarming = true;
        animator.SetTrigger("farm");
        Debug.Log("Farming");
        yield return new WaitForSeconds(0.5f);

        CheckHit();

        yield return new WaitForSeconds(0.3f);
        isFarming = false;
    }

    void CheckHit()
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            farmPoint.position,
            farmRange,
            treeLayer
        );

        foreach (var hit in hitColliders)
        {
            Debug.Log("Hit: " + hit.name);
            if (hit.TryGetComponent<DamageTree>(out var tree))
            {
                Debug.Log("Tree Found");
                DamageTreeServerRpc(tree.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DamageTreeServerRpc(ulong treeNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(treeNetworkId, out NetworkObject tree))
        {
            Debug.Log("Tree Damaged");
            tree.GetComponent<DamageTree>().Damage();
        }
    }
}
