using UnityEngine;
using Unity.Netcode;

public class BoulderSoundController : NetworkBehaviour
{
    private BoulderController boulderController;
    private Rigidbody rb;
    private bool isGrounded = false;
    private bool isSoundPlaying = false;
    
    // Configurable parameters
    [SerializeField] private float minSpeedForSound = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;

    private void Awake()
    {
        boulderController = GetComponent<BoulderController>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"Boulder Sound Controller spawned. IsServer: {IsServer}, IsClient: {IsClient}");
    }

    private void FixedUpdate()
    {
        if (!IsSpawned)
        {
            Debug.Log("Boulder Sound Controller not spawned yet");
            return;
        }

        if (!IsOwner) return;

        float horizontalSpeed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
        bool wasGrounded = isGrounded;
        
        // Check if boulder is grounded using raycast
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, GetComponent<SphereCollider>().radius, groundLayer);
        
        if (isGrounded != wasGrounded)
        {
            Debug.Log($"Ground state changed. IsGrounded: {isGrounded}, Speed: {horizontalSpeed}");
        }
        
        // Determine if the sound should be playing
        bool shouldPlaySound = (isGrounded || wasGrounded) && horizontalSpeed > minSpeedForSound;
        
        if (!wasGrounded && isGrounded)
        {
            // SoundManager.Instance.PlayAtPosition("BoulderContact", transform.position);
            NetworkedSoundManager.Instance.PlaySoundClientRpc("BoulderContact", transform.position, true);
        }

        // If state changed, update sound
        if (shouldPlaySound != isSoundPlaying)
        {
            Debug.Log($"Sound state changing. ShouldPlay: {shouldPlaySound}, IsGrounded: {isGrounded}, Speed: {horizontalSpeed}");
            
            if (shouldPlaySound)
            {
                Debug.Log("STARTING BOULDER ROLLING SOUND");
                StartRollingSoundServerRpc();
            }
            else
            {
                Debug.Log("STOPPING BOULDER ROLLING SOUND");
                StopRollingSoundServerRpc();
            }
            isSoundPlaying = shouldPlaySound;
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize the ground check ray
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * GetComponent<SphereCollider>().radius);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartRollingSoundServerRpc()
    {
        // Tell all clients to start the rolling sound
        CheckExistingSourcesClientRpc();
        NetworkedSoundManager.Instance.AttachContinuousSoundClientRpc("Boulder", new NetworkObjectReference(gameObject));
    }

    [ClientRpc]
    private void CheckExistingSourcesClientRpc()
    {
        if (TryGetComponent<AudioSource>(out AudioSource _source))
        {
            Destroy(_source);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StopRollingSoundServerRpc()
    {
        // Tell all clients to stop the rolling sound
        NetworkedSoundManager.Instance.StopContinuousSoundClientRpc(new NetworkObjectReference(gameObject));
    }

    // Optional: Adjust volume based on speed
    [ClientRpc]
    private void UpdateSoundVolumeClientRpc(float normalizedSpeed)
    {
        AudioSource source = GetComponent<AudioSource>();
        if (source != null)
        {
            source.volume = Mathf.Lerp(0.2f, 1.0f, normalizedSpeed);
        }
    }
}