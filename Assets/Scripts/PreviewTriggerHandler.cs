using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewTriggerHandler : MonoBehaviour
{
    private BuildingSystem buildingSystem;
    private BoxCollider triggerCollider;
    private HashSet<Collider> overlappingColliders = new HashSet<Collider>();
    private LineRenderer[] debugLines;
    private Material lineMaterial;

    public void Initialize(BuildingSystem system)
    {
        buildingSystem = system;
        triggerCollider = GetComponent<BoxCollider>();
        CreateDebugLines();
    }

    private void CreateDebugLines()
    {
        // Create a material for the lines
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        
        // We need 12 lines to make a box (4 for top, 4 for bottom, 4 connecting them)
        debugLines = new LineRenderer[12];
        
        for (int i = 0; i < 12; i++)
        {
            GameObject lineObj = new GameObject($"DebugLine_{i}");
            lineObj.transform.SetParent(transform);
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.material = lineMaterial;
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.positionCount = 2; // Each line has 2 points
            
            debugLines[i] = line;
        }
    }

    private void Update()
    {
        if (buildingSystem == null || triggerCollider == null) return;

        Vector3 center = transform.TransformPoint(triggerCollider.center);
        Vector3 halfExtents = Vector3.Scale(triggerCollider.size * 0.5f, transform.lossyScale);
        
        UpdateDebugBox(center, halfExtents, transform.rotation);
        
        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, transform.rotation);
        
        bool isColliding = false;
        Collider validCollider = null;
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform.IsChildOf(transform)) continue;
            if (hitCollider.isTrigger) continue;
            
            isColliding = true;
            validCollider = hitCollider;
            break; // No need to check more once we find any valid collision
        }
        
        buildingSystem.HandleCollisionEnter(isColliding, validCollider);
    }

    private void UpdateDebugBox(Vector3 center, Vector3 halfExtents, Quaternion rotation)
    {
        Color lineColor = overlappingColliders.Count > 0 ? Color.red : Color.green;
        foreach (var line in debugLines)
        {
            line.startColor = lineColor;
            line.endColor = lineColor;
        }

        Vector3[] corners = new Vector3[8];
        corners[0] = center + rotation * new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
        corners[1] = center + rotation * new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);
        corners[2] = center + rotation * new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
        corners[3] = center + rotation * new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
        corners[4] = center + rotation * new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z);
        corners[5] = center + rotation * new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z);
        corners[6] = center + rotation * new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z);
        corners[7] = center + rotation * new Vector3(halfExtents.x, halfExtents.y, halfExtents.z);

        // Bottom face
        SetLinePositions(debugLines[0], corners[0], corners[1]);
        SetLinePositions(debugLines[1], corners[1], corners[5]);
        SetLinePositions(debugLines[2], corners[5], corners[4]);
        SetLinePositions(debugLines[3], corners[4], corners[0]);

        // Top face
        SetLinePositions(debugLines[4], corners[2], corners[3]);
        SetLinePositions(debugLines[5], corners[3], corners[7]);
        SetLinePositions(debugLines[6], corners[7], corners[6]);
        SetLinePositions(debugLines[7], corners[6], corners[2]);

        // Vertical edges
        SetLinePositions(debugLines[8], corners[0], corners[2]);
        SetLinePositions(debugLines[9], corners[1], corners[3]);
        SetLinePositions(debugLines[10], corners[4], corners[6]);
        SetLinePositions(debugLines[11], corners[5], corners[7]);
    }

    private void SetLinePositions(LineRenderer line, Vector3 start, Vector3 end)
    {
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }


    // private void OnTriggerStay(Collider other)
    // {
    //     Debug.Log("Trigger entered: " + other.name);
    //     if (buildingSystem == null)
    //     {
    //         Debug.LogError("BuildingSystem not initialized");
    //         return;
    //     }

    //     buildingSystem.HandleCollisionEnter(true, other);
    // }

    private void OnTriggerExit(Collider other)
    {
        if (buildingSystem == null) return; 

        buildingSystem.HandleCollisionEnter(false, other);
    }
}
