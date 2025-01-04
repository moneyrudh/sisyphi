using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Netcode;
using UnityEngine;

public class PlacementModeManager : NetworkBehaviour
{
    private BoatPlacementSystem boatSystem;
    private BuildingSystem buildSystem;

    private void Start()
    {
        boatSystem = GetComponent<BoatPlacementSystem>();
        buildSystem = GetComponent<BuildingSystem>();

        if (IsOwner)
        {
            if (boatSystem != null)
            {
                boatSystem.OnPlacementModeChanged += HandleBoatPlacementModeChanged;
            }
            if (buildSystem != null)
            {
                buildSystem.OnBuildModeChanged += HandleBuildModeChanged;
            }
        }
    }
    
    private void HandleBoatPlacementModeChanged(bool isActive)
    {
        if (isActive && buildSystem != null && buildSystem.inBuildMode)
        {
            buildSystem.ToggleBuildMode();
        }
    }

    private void HandleBuildModeChanged(bool isActive)
    {
        if (isActive && boatSystem != null && boatSystem.inPlacementMode)
        {
            boatSystem.TogglePlacementMode();
        }
    }

    private void OnDestroy()
    {
        if (boatSystem != null)
        {
            boatSystem.OnPlacementModeChanged -= HandleBoatPlacementModeChanged;
        }
        if (buildSystem != null)
        {
            buildSystem.OnBuildModeChanged -= HandleBuildModeChanged;
        }
    }
}
