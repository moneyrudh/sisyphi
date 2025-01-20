using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerFarm : NetworkBehaviour
{
    private Animator animator;
    private Movement movement;
    private float farmRange = 0.5f;
    [SerializeField] private LayerMask treeLayer;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform farmPoint;

    private bool isFarming = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<Movement>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (movement.moveDirection.magnitude < 0.1f && Input.GetMouseButton(1) && !isFarming && movement.isGrounded)
        {
            // StartCoroutine(PerformFarmRoutine());
            isFarming = true;
            animator.SetTrigger("farm");
        }

        if (movement.moveDirection.magnitude > 0.1f)
        {
            isFarming = false;
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(farmPoint.position, farmRange);
    }

    public void StartFarm()
    {
        // Debug.Log("Start Farming");
    }

    public void StopFarm()
    {
        isFarming = false;
        // Debug.Log("Stop Farming");
    }

    IEnumerator PerformFarmRoutine()
    {
        yield return null;
        // isFarming = true;
        // animator.SetTrigger("farm");
        // Debug.Log("Farming");
        // yield return new WaitForSeconds(0.1f);

        // CheckHit();

        // yield return new WaitForSeconds(0.4f);
        // isFarming = false;
    }

    void CheckHit()
    {
        if (!IsOwner) return;
        Debug.Log("Checking hit");
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

        hitColliders = Physics.OverlapSphere(
            farmPoint.position,
            farmRange,
            playerLayer
        );

        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Player") && hit.gameObject != gameObject)
            {
                // hit.GetComponent<Movement>().GetHit();
                PlayerHitServerRpc(new NetworkObjectReference(hit.GetComponent<NetworkObject>()));
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerHitServerRpc(NetworkObjectReference playerRef)
    {
        PlayerHitClientRpc(playerRef);
    }

    [ClientRpc]
    private void PlayerHitClientRpc(NetworkObjectReference playerRef)
    {
        if (playerRef.TryGet(out NetworkObject playerObj))
        {
            if (playerObj.IsOwner) playerObj.GetComponent<Movement>().GetHit();
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

    public void PlayFarmAnimation()
    {
        SoundManager.Instance.PlayAtPosition("TreeChop", transform.position);
    }

    public void DisableMovement()
    {
        movement.SetMovement(false);
    }

    public void EnableMovement()
    {
        movement.SetMovement(true);
    }
}
