using UnityEngine;
using Unity.Netcode;

public class NetworkedSoundManager : NetworkBehaviour
{
    public static NetworkedSoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called by server to play sound on all clients
    [ClientRpc]
    public void PlaySoundClientRpc(string soundName, Vector3 position, bool is3D)
    {
        if (is3D)
        {
            SoundManager.Instance.PlayAtPosition(soundName, position);
        }
        else
        {
            SoundManager.Instance.Play(soundName);
        }
    }

    [ClientRpc]
    public void AttachContinuousSoundClientRpc(string soundName, NetworkObjectReference networkObjectRef)
    {
        if (networkObjectRef.TryGet(out NetworkObject netObj))
        {
            AudioSource source = SoundManager.Instance.AttachSound(soundName, netObj.gameObject);
            if (source != null)
            {
                // source.Play();
            }
        }
    }

    // Called by server to stop continuous sound on a networked object
    [ClientRpc]
    public void StopContinuousSoundClientRpc(NetworkObjectReference networkObjectRef)
    {
        if (networkObjectRef.TryGet(out NetworkObject netObj))
        {
            AudioSource source = netObj.GetComponent<AudioSource>();
            if (source != null)
            {
                // source.Stop();
                // Destroy(source);
                SoundManager.Instance.DetachSound(source, 0.5f);
            }
        }
    }

    // Called by client to request sound playing
    [ServerRpc(RequireOwnership = false)]
    public void RequestPlaySoundServerRpc(string soundName, Vector3 position, bool is3D)
    {
        // Server receives request and broadcasts to all clients
        PlaySoundClientRpc(soundName, position, is3D);
    }

    // For local sounds that don't need network sync
    public void PlayLocalSound(string soundName, bool is3D = false, Vector3? position = null)
    {
        if (is3D && position.HasValue)
        {
            SoundManager.Instance.PlayAtPosition(soundName, position.Value);
        }
        else
        {
            SoundManager.Instance.Play(soundName);
        }
    }
}