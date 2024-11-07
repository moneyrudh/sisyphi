using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class BuildingSystem : NetworkBehaviour
{
    [Header("Building Settings")]
    public float snapDistance = 1f;
    public float maxBuildDistance = 1000f;
    public LayerMask tileLayer;

    [Header("Input")]
    public InputActionReference buildToggle;
    public InputActionReference buildConfirm;

    [Header("References")]
    private GameObject rampPreview;
    public GameObject rampPrefab;
    public GameObject rampGhostPrefab;

    // private Camera mainCamera;
    private Camera playerCamera;
    private RampObject rampPreviewComponent;
    private TileEdge currentEdge;
    private bool inBuildMode = false;

    private Movement movement;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"BuildingSystem spawned - IsHost: {IsHost} | IsClient: {IsClient} | IsOwner: {IsOwner} | IsServer: {IsServer}");
    
        if (IsOwner)
        {
            // InitializePreview();
            StartCoroutine(InitializeWithDelay());
            EnableInputs();
        }
    }

    private void OnEnable()
    {
        if (IsOwner)
        {
            EnableInputs();
        }
    }

    private void OnDisable()
    {
        if (IsOwner)
        {
            DisableInputs();
        }
    }

    private void EnableInputs()
    {
        if (buildToggle != null)
        {
            buildToggle.action.Enable();
            buildToggle.action.started += HandleBuildToggle;
        }
        
        if (buildConfirm != null)
        {
            buildConfirm.action.Enable();
            buildConfirm.action.started += HandleBuildConfirm;
        }
    }

    private void DisableInputs()
    {
        if (buildToggle != null)
        {
            buildToggle.action.Disable();
            buildToggle.action.started -= HandleBuildToggle;
        }
        
        if (buildConfirm != null)
        {
            buildConfirm.action.Disable();
            buildConfirm.action.started -= HandleBuildConfirm;
        }
    }

    private void HandleBuildToggle(InputAction.CallbackContext context)
    {
        ToggleBuildMode();
    }

    private void HandleBuildConfirm(InputAction.CallbackContext context)
    {
        TryPlaceRamp();
    }

    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForEndOfFrame();
        InitializePreview();
    }

    // Start is called before the first frame update
    private void InitializePreview()
    {
        Debug.Log("Initialize Preview");
        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError($"Could not find camera for player {OwnerClientId}");
            return;
        }


        rampPreview = Instantiate(rampGhostPrefab);
        if (rampPreview != null)
        {
            rampPreview.SetActive(true);
            rampPreviewComponent = rampPreview.GetComponent<RampObject>();
            
        }

        movement = GetComponent<Movement>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || playerCamera == null) return;

        if (inBuildMode)
        {
            HandleMouseDetection();
        }
    }

    private void HandleMouseDetection()
    {
        if (!IsOwner || playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);
        if (Physics.Raycast(ray, out hit, 1000f, tileLayer))
        {
            // Debug.Log("Hit");
            // Debug.DrawLine(ray.origin, hit.point, Color.red);

            // if (Vector3.Distance(transform.position, hit.point) > maxBuildDistance)
            // {
            //     HideRampPreview();
            //     return;
            // }

            TileEdges tile = hit.collider.GetComponent<TileEdges>();
            if (tile != null)
            {
                // Debug.Log($"[{Time.frameCount}] FindClosestEdge called by: {(IsHost ? "Host" : "Client")} | IsOwner: {IsOwner}");
                FindClosestEdge(tile, hit.point);
            }
        }
        else
        {
            // Debug.Log("No hit");
            HideRampPreview();
            currentEdge = null;
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

            // Debug.Log("Tryna find closest edge by: " + (IsHost ? "Host" : "Client") + " | IsOwner: " + IsOwner + " | Distance: " + distance);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEdge = edge;
            }

        }

        if (closestEdge != null && currentEdge != closestEdge)
        {
            currentEdge = closestEdge;
            UpdateRampPreview(closestEdge);
        }
        else if (closestEdge == null)
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
        // Debug.Log($"UpdateRampPreview called by: {(IsHost ? "Host" : "Client")} | IsOwner: {IsOwner} | IsServer: {IsServer} | IsClient: {IsClient}");
    
        if (rampPreview == null || !inBuildMode) return;

        rampPreview.SetActive(true);

        Vector3 position = (edge.startPoint + edge.endPoint) / 2f;
        Quaternion rotation = GetRampRotation(edge.direction);

        position += Vector3.up * 0.01f;

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

    private void TryPlaceRamp()
    {
        if (currentEdge == null) return;

        // Check if player has enough wood

        // Check movement related logic

        PlaceRampServerRpc(
            rampPreview.transform.position,
            rampPreview.transform.rotation,
            currentEdge.startPoint,
            currentEdge.endPoint,
            currentEdge.direction
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceRampServerRpc(Vector3 position, Quaternion rotation, Vector3 edgeStart, Vector3 edgeEnd, EdgeDirection direction)
    {
        Debug.Log($"PlaceRampServerRpc called by: {(IsHost ? "Host" : "Client")} | IsOwner: {IsOwner} | IsServer: {IsServer} | IsClient: {IsClient}");
        GameObject ramp = Instantiate(rampPrefab, position, rotation);
        NetworkObject networkObject = ramp.GetComponent<NetworkObject>();
        networkObject.Spawn();

        UpdateEdgeStateClientRpc(edgeStart, edgeEnd, direction);
        // if (edgeRef.TryGet(out NetworkBehaviour edge))
        // {
        // }

        // Update inventory
    }

    [ClientRpc]
    private void UpdateEdgeStateClientRpc(Vector3 edgeStart, Vector3 edgeEnd, EdgeDirection direction)
    {
        Collider[] colliders = Physics.OverlapSphere(edgeStart, 0.1f, tileLayer);
        foreach (Collider collider in colliders)
        {
            TileEdges tile = collider.GetComponent<TileEdges>();
            if (tile != null)
            {
                foreach (TileEdge edge in tile.edges)
                {
                    if (Vector3.Distance(edge.startPoint, edgeStart) < 0.1f &&
                        Vector3.Distance(edge.endPoint, edgeEnd) < 0.1f &&
                        edge.direction == direction)
                    {
                        edge.isOccupied = true;
                        break;
                    }
                }
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
