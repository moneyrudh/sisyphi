using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BuildingSystem : NetworkBehaviour
{
    [Header("Building Settings")]
    public float snapDistance = 1f;
    public float maxBuildDistance = 1000f;
    public LayerMask tileLayer;

    [Header("References")]
    private GameObject rampPreview;
    public GameObject rampPrefab;
    public GameObject rampGhostPrefab;

    private Camera mainCamera;
    private RampObject rampPreviewComponent;
    private TileEdge currentEdge;
    private bool inBuildMode = false;

    private Movement movement;
    
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        rampPreview = Instantiate(rampGhostPrefab);
        if (rampPreview != null)
        {
            // Make it semi-transparent for debugging
            var renderers = rampPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material previewMaterial = new Material(renderer.material);
                previewMaterial.color = new Color(1f, 1f, 0f, 0.5f); // Yellow semi-transparent
                renderer.material = previewMaterial;
            }
            
            // For debugging, we'll keep it visible
            rampPreview.SetActive(true);
            rampPreviewComponent = rampPreview.GetComponent<RampObject>();
            
            // Ensure it doesn't have a NetworkObject component
            NetworkObject netObj = rampPreview.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                Destroy(netObj);
            }
        }

        movement = GetComponent<Movement>();
    }

    // Update is called once per frame
    void Update()
    {
        // if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }

        // if (!inBuildMode) return;

        HandleMouseDetection();

        if (Input.GetMouseButtonDown(1))
        {
            // TryPlaceRamp();
        }
    }

    private void HandleMouseDetection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, tileLayer))
        {
            // Debug.Log("Hit");
            Debug.DrawLine(ray.origin, hit.point, Color.red);

            // if (Vector3.Distance(transform.position, hit.point) > maxBuildDistance)
            // {
            //     HideRampPreview();
            //     return;
            // }

            TileEdges tile = hit.collider.GetComponent<TileEdges>();
            if (tile != null)
            {
                FindClosestEdge(tile, hit.point);
            }
        }
        else
        {
            // Debug.Log("No hit");
            // HideRampPreview();
        }
    }

    private void FindClosestEdge(TileEdges tile, Vector3 hitPoint)
    {
        float closestDistance = snapDistance;
        TileEdge closestEdge = null;

        foreach (TileEdge edge in tile.edges)
        {
            if (edge.isOccupied) continue;

            float distance = PointToLineDistance(hitPoint, edge.startPoint, edge.endPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEdge = edge;
            }

        }

        if (closestEdge != null)
        {
            if (currentEdge != closestEdge)
            {
                currentEdge = closestEdge;
                UpdateRampPreview(closestEdge);
            }
        }
        else
        {
            HideRampPreview();
            currentEdge = null;
        }
    }

    private float PointToLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        Vector3 lineDirection = line / lineLength;

        Vector3 pointToStart = point - lineStart;
        float projection = Vector3.Dot(pointToStart, lineDirection);

        projection = Mathf.Clamp(projection, 0f, lineLength);

        Vector3 closestPoint = lineStart + lineDirection * projection;
        return Vector3.Distance(point, closestPoint);
    }

    private void UpdateRampPreview(TileEdge edge)
    {
        if (rampPreview == null) return;

        rampPreview.SetActive(true);

        Vector3 position = (edge.startPoint + edge.endPoint) / 2f;
        Quaternion rotation = GetRampRotation(edge.direction);
        Debug.Log("Edge Direction: " + edge.direction);
        Debug.Log("Position: " + position);
        Debug.Log("Rotation: " + rotation.eulerAngles);
        rampPreview.transform.position = position;
        rampPreview.transform.rotation = rotation;
    }

    private Quaternion GetRampRotation(EdgeDirection direction)
    {
        switch (direction)
        {
            case EdgeDirection.North:
                return Quaternion.Euler(0, 0, 0);
            case EdgeDirection.East:
                return Quaternion.Euler(0, 90, 0);
            case EdgeDirection.South:
                return Quaternion.Euler(0, 180, 0);
            case EdgeDirection.West:
                return Quaternion.Euler(0, 270, 0);
            default:
                return Quaternion.identity;
        }
    }

    // private void TryPlaceRamp()
    // {
    //     if (currentEdge == null) return;

    //     // Check if player has enough wood

    //     // Check movement related logic

    //     PlaceRampServerRpc(
    //         rampPreview.transform.position,
    //         rampPreview.transform.rotation,
    //         NetworkBehaviourReference.Create(currentEdge.GetComponent<NetworkBehaviour>())
    //     );
    // }

    [ServerRpc]
    private void PlaceRampServerRpc(Vector3 position, Quaternion rotation, NetworkBehaviourReference edgeRef)
    {
        GameObject ramp = Instantiate(rampPrefab, position, rotation);
        NetworkObject networkObject = ramp.GetComponent<NetworkObject>();
        networkObject.Spawn();

        if (edgeRef.TryGet(out NetworkBehaviour edge))
        {
            // UpdateEdgeStateClientRpc(NetworkBehaviourReference.Create(edge));
        }

        // Update inventory
    }

    [ClientRpc]
    private void UpdateEdgeStateClientRpc(NetworkBehaviourReference edgeRef)
    {
        if (edgeRef.TryGet(out NetworkBehaviour edgeBehaviour))
        {
            TileEdge edge = edgeBehaviour.GetComponent<TileEdge>();
            if (edge != null)
            {
                edge.isOccupied = true;
            }
        }
    }

    [ClientRpc]
    private void UpdateInventoryClientRpc(ClientRpcParams rpcParams = default)
    {
        if (IsOwner)
        {
            // Update inventory
        }
    }

    private bool CanPlayerBuild()
    {
        // Update later
        return true;
    }

    public void ToggleBuildMode()
    {
        if (!IsOwner) return;

        if (!inBuildMode && !CanPlayerBuild())
        {
            return;
        }

        inBuildMode = !inBuildMode;

        if (!inBuildMode)
        {
            HideRampPreview();
        }
    }

    private void HideRampPreview()
    {
        if (rampPreview != null)
        {
            rampPreview.SetActive(false);
        }
    }
}
