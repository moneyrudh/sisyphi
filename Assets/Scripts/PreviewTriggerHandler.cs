using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewTriggerHandler : MonoBehaviour
{
    private BuildingSystem buildingSystem;

    public void Initialize(BuildingSystem system)
    {
        buildingSystem = system;
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log("Trigger entered: " + other.name);
        if (buildingSystem == null)
        {
            Debug.LogError("BuildingSystem not initialized");
            return;
        }

        buildingSystem.HandleCollisionEnter(true, other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (buildingSystem == null) return; 

        buildingSystem.HandleCollisionEnter(false, other);
    }
}
