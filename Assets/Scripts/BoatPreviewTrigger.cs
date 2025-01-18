using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatPreviewTrigger : MonoBehaviour
{
    private BoatPlacementSystem placementSystem;
    private BoxCollider triggerCollider;
    private HashSet<Collider> overlappingColliders = new HashSet<Collider>();
    private LineRenderer[] debugLines;
    private Material lineMaterial;
    private bool isOverWater = false;
    private Vector3 originalExtents; // Store original box collider size

    [Header("Validation Settings")]
    private LayerMask waterLayer;
    private Vector3 lastValidWaterPosition;
    private const float waterCheckOffset = 0.1f;

    public void Initialize(BoatPlacementSystem system)
    {
        placementSystem = system;
        triggerCollider = GetComponent<BoxCollider>();
        waterLayer = system.waterLayer;
        originalExtents = triggerCollider.size * 1.4f; // Store the original size
        // CreateDebugLines();
    }
    
    private void CreateDebugLines()
    {
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        debugLines = new LineRenderer[12];

        for (int i=0; i<12; i++)
        {
            GameObject lineObj = new GameObject($"BoatDebugLine_{i}");
            lineObj.transform.SetParent(transform);

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.material = lineMaterial;
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.positionCount = 2;

            debugLines[i] = line;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (placementSystem == null) return;

        ValidatePlacement();
        // UpdateDebugVisualization();
    }

    private void ValidatePlacement()
    {
        Vector3 center = transform.TransformPoint(triggerCollider.center);
        
        // Water check
        RaycastHit waterHit;
        isOverWater = Physics.Raycast(
            center + Vector3.up,
            Vector3.down,
            out waterHit,
            10f,
            waterLayer
        );

        if (isOverWater)
        {
            lastValidWaterPosition = waterHit.point;

            // Use local space dimensions for consistent box check
            Collider[] hitColliders = Physics.OverlapBox(
                center,
                originalExtents,
                transform.rotation,
                placementSystem.obstructionLayer
            );

            bool hasObstruction = hitColliders.Length > 0;
            placementSystem.SetPlacementValidity(!hasObstruction);

            overlappingColliders.Clear();
            if (hasObstruction)
            {
                foreach (var collider in hitColliders)
                {
                    overlappingColliders.Add(collider);
                }
            }
        }
        else
        {
            placementSystem.SetPlacementValidity(false);
        }
    }

    private void UpdateDebugVisualization()
    {
        Vector3 center = transform.TransformPoint(triggerCollider.center);
        
        // Use original extents for visualization
        Vector3[] corners = new Vector3[8];
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(transform.rotation);
        
        corners[0] = center + rotationMatrix.MultiplyVector(new Vector3(-originalExtents.x, -originalExtents.y, -originalExtents.z));
        corners[1] = center + rotationMatrix.MultiplyVector(new Vector3(originalExtents.x, -originalExtents.y, -originalExtents.z));
        corners[2] = center + rotationMatrix.MultiplyVector(new Vector3(-originalExtents.x, originalExtents.y, -originalExtents.z));
        corners[3] = center + rotationMatrix.MultiplyVector(new Vector3(originalExtents.x, originalExtents.y, -originalExtents.z));
        corners[4] = center + rotationMatrix.MultiplyVector(new Vector3(-originalExtents.x, -originalExtents.y, originalExtents.z));
        corners[5] = center + rotationMatrix.MultiplyVector(new Vector3(originalExtents.x, -originalExtents.y, originalExtents.z));
        corners[6] = center + rotationMatrix.MultiplyVector(new Vector3(-originalExtents.x, originalExtents.y, originalExtents.z));
        corners[7] = center + rotationMatrix.MultiplyVector(new Vector3(originalExtents.x, originalExtents.y, originalExtents.z));

        Color lineColor = (isOverWater && overlappingColliders.Count == 0) ? Color.green : Color.red;
        
        // Bottom face
        SetLinePositions(debugLines[0], corners[0], corners[1], lineColor);
        SetLinePositions(debugLines[1], corners[1], corners[5], lineColor);
        SetLinePositions(debugLines[2], corners[5], corners[4], lineColor);
        SetLinePositions(debugLines[3], corners[4], corners[0], lineColor);

        // Top face
        SetLinePositions(debugLines[4], corners[2], corners[3], lineColor);
        SetLinePositions(debugLines[5], corners[3], corners[7], lineColor);
        SetLinePositions(debugLines[6], corners[7], corners[6], lineColor);
        SetLinePositions(debugLines[7], corners[6], corners[2], lineColor);

        // Vertical edges
        SetLinePositions(debugLines[8], corners[0], corners[2], lineColor);
        SetLinePositions(debugLines[9], corners[1], corners[3], lineColor);
        SetLinePositions(debugLines[10], corners[4], corners[6], lineColor);
        SetLinePositions(debugLines[11], corners[5], corners[7], lineColor);
    }

    private void SetLinePositions(LineRenderer line, Vector3 start, Vector3 end, Color color)
    {
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startColor = color;
        line.endColor = color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == waterLayer) return;
        overlappingColliders.Add(other);
        placementSystem.SetPlacementValidity(false);
    }

    private void OnTriggerExit(Collider other)
    {
        overlappingColliders.Remove(other);
        if (overlappingColliders.Count == 0 && isOverWater)
        {
            placementSystem.SetPlacementValidity(true); // Fixed: Set to true when valid
        }
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}
