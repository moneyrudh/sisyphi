using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CheckpointAction : MonoBehaviour
{
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private int checkpointIndex;

    public event System.Action<CheckpointAction, int, Vector3> OnCheckpointTriggered;

    private void Start()
    {
        StopFireParticles();
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            PlayerSpawnHandler spawnHandler = collider.GetComponent<PlayerSpawnHandler>();
            if (spawnHandler.GetCurrentCheckpoint() == checkpointIndex) return;
            PlayFireParticles();
            spawnHandler.SetCheckpoint(this, checkpointIndex, transform.position);
        }
    }

    public void StopFireParticles()
    {
        fireParticles.Stop();
    }

    private void PlayFireParticles()
    {
        fireParticles.Play();
    }
}