using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class BuildingSystem : NetworkBehaviour
{
    [Header("Building Types")]
    public BuildableType currentBuildType = BuildableType.Ramp;

    [Header("Building Prefabs")]
    public GameObject rampPrefab;
    public GameObject rampGhostPrefab;
    public GameObject connectorPrefab;
    public GameObject connectorGhostPrefab;

    [Header("Building Settings")]
    public float snapDistance = 1f;
    public float maxBuildDistance = 1000f;
    public LayerMask tileLayer;
    public LayerMask buildableLayer;

    [Header("Input")]
    public InputActionReference buildToggle;
    public InputActionReference buildConfirm;

    [Header("References")]
    private GameObject buildPreview;
    private Camera playerCamera;
    private RampObject rampPreviewComponent;
    private TileEdge currentTileEdge;
    private BuildableEdge currentBuildableEdge;
    private BuildableObject targetBuildableObject;
    private BuildableObject ghostBuildableObject;
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
        TryPlaceBuild();
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


        buildPreview = Instantiate(rampGhostPrefab);
        if (buildPreview != null)
        {
            buildPreview.SetActive(true);
            ghostBuildableObject = buildPreview.GetComponent<BuildableObject>();
            rampPreviewComponent = buildPreview.GetComponent<RampObject>();
        }

        movement = GetComponent<Movement>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || playerCamera == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetBuildType(BuildableType.Ramp);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetBuildType(BuildableType.Connector);
        }

        if (inBuildMode)
        {
            HandleMouseDetection();

            if (Input.GetKeyDown(KeyCode.R))
            {
                InvertRampDirection();
            }
        }
    }

    private void SetBuildType(BuildableType type)
    {
        if (currentBuildType != type)
        {
            currentBuildType = type;
            UpdateBuildPreview();
        }
    }

    private void UpdateBuildPreview()
    {
        if (buildPreview != null)
        {
            Destroy(buildPreview);
        }

        switch (currentBuildType)
        {
            case BuildableType.Ramp:
                buildPreview = Instantiate(rampGhostPrefab);
                rampPreviewComponent = buildPreview.GetComponent<RampObject>();
                break;
            case BuildableType.Connector:
                buildPreview = Instantiate(connectorGhostPrefab);
                break;
        }

        if (buildPreview != null)
        {
            buildPreview.SetActive(false);
            ghostBuildableObject = buildPreview.GetComponent<BuildableObject>();
        }
    }

    private void HandleMouseDetection()
    {
        // if (!IsOwner || playerCamera == null) return;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
    
        switch (currentBuildType)
        {
            case BuildableType.Ramp:
                HandleRampPlacement(ray);
                break;
            case BuildableType.Connector:
                HandleConnectorPlacement(ray);
                break;
        }
        // Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);
    }

    private void HandleRampPlacement(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f, tileLayer))
        {
            // if (Vector3.Distance(transform.position, hit.point) > maxBuildDistance)
            // {
            //     HideBuildPreview();
            //     return;
            // }

            TileEdges tile = hit.collider.GetComponent<TileEdges>();
            if (tile != null && currentBuildType == BuildableType.Ramp)
            {
                // Debug.Log($"[{Time.frameCount}] FindClosestEdge called by: {(IsHost ? "Host" : "Client")} | IsOwner: {IsOwner}");
                FindClosestEdge(tile, hit.point);
            }
        }
        else
        {
            // Debug.Log("No hit");
            HideBuildPreview();
            currentTileEdge = null;
        }
    }

    private void HandleConnectorPlacement(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f, buildableLayer))
        {
            BuildableObject targetObject = hit.collider.GetComponentInParent<BuildableObject>();
            if (targetObject != null)
            {
                FindClosestBuildableEdge(targetObject, hit.point);
            }
            else
            {
                HideBuildPreview();
                currentBuildableEdge = null;
                targetBuildableObject = null;
            }
        }
    }

    private void FindClosestBuildableEdge(BuildableObject target, Vector3 hitPoint)
    {
        Debug.Log("FindClosestBuildableEdge called at hitpoint: " + hitPoint);
        float closestDistance = snapDistance;
        BuildableEdge closestEdge = null;

        foreach (BuildableEdge edge in target.GetAvailableEdges())
        {
            Vector3 edgeWorldPos = target.GetWorldEdgePosition(edge);
            float distance = Vector3.Distance(hitPoint, edgeWorldPos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEdge = edge;
            }
        }

        if (closestEdge != null && (currentBuildableEdge != closestEdge || targetBuildableObject != target))
        {
            currentBuildableEdge = closestEdge;
            targetBuildableObject = target;
            UpdateConnectorPreview(target, closestEdge);
        }
        else if (closestEdge == null)
        {
            HideBuildPreview();
            currentBuildableEdge = null;
            targetBuildableObject = null;
        }
    }

    private void UpdateConnectorPreview(BuildableObject target, BuildableEdge edge)
    {
        if (buildPreview == null || !inBuildMode) return;

        buildPreview.SetActive(true);
        Vector3 edgeWorldPos = target.GetWorldEdgePosition(edge);
        EdgeDirection oppositeDirection = GetOppositeDirection(edge.direction);

        Quaternion targetRotation = target.transform.rotation;
        Quaternion connectorRotation = GetConnectorRotation(oppositeDirection);

        buildPreview.transform.position = edgeWorldPos;
        buildPreview.transform.rotation = targetRotation * connectorRotation;
    }

    private EdgeDirection GetOppositeDirection(EdgeDirection direction)
    {
        switch (direction)
        {
            case EdgeDirection.North: return EdgeDirection.South;
            case EdgeDirection.East: return EdgeDirection.West;
            case EdgeDirection.South: return EdgeDirection.North;
            case EdgeDirection.West: return EdgeDirection.East;
            default: return direction;
        }
    }

    private Quaternion GetConnectorRotation(EdgeDirection direction)
    {
        switch (direction)
        {
            case EdgeDirection.North:
                return Quaternion.Euler(0, 180, 0);
            case EdgeDirection.East:
                return Quaternion.Euler(0, 270, 0);
            case EdgeDirection.South:
                return Quaternion.Euler(0, 0, 0);
            case EdgeDirection.West:
                return Quaternion.Euler(0, 90, 0);
            default:
                return Quaternion.identity;
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

        if (closestEdge != null && currentTileEdge != closestEdge)
        {
            currentTileEdge = closestEdge;
            UpdateRampPreview(closestEdge);
        }
        else if (closestEdge == null)
        {
            HideBuildPreview();
            currentTileEdge = null;
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
    
        if (buildPreview == null || !inBuildMode) return;

        buildPreview.SetActive(true);

        Vector3 position = (edge.startPoint + edge.endPoint) / 2f;
        Quaternion rotation = GetRampRotation(edge.direction);

        // position += Vector3.up * 0.01f;

        buildPreview.transform.position = position;
        buildPreview.transform.rotation = rotation;
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

    private Quaternion InvertRampRotation(EdgeDirection direction)
    {
        switch (direction)
        {
            case EdgeDirection.North:
                currentTileEdge.direction = EdgeDirection.South;
                return Quaternion.Euler(0, 180, 0);
            case EdgeDirection.East:
                currentTileEdge.direction = EdgeDirection.West;
                return Quaternion.Euler(0, 270, 0);
            case EdgeDirection.South:
                currentTileEdge.direction = EdgeDirection.North;
                return Quaternion.Euler(0, 0, 0);
            case EdgeDirection.West:
                currentTileEdge.direction = EdgeDirection.East;
                return Quaternion.Euler(0, 90, 0);
            default:
                return Quaternion.identity;
        }
    }

    private void TryPlaceBuild()
    {
        switch (currentBuildType)
        {
            case BuildableType.Ramp:
                TryPlaceRamp();
                break;
            case BuildableType.Connector:
                TryPlaceConnector();
                break;
        }
    }

    private void TryPlaceRamp()
    {

        if (currentTileEdge == null) return;

        // Check if player has enough wood

        // Check movement related logic

        PlaceRampServerRpc(
            buildPreview.transform.position,
            buildPreview.transform.rotation,
            currentTileEdge.startPoint,
            currentTileEdge.endPoint,
            currentTileEdge.direction
        );
    }

    private void TryPlaceConnector()
    {
        if (currentBuildableEdge == null || targetBuildableObject == null) return;

        PlaceConnectorServerRpc(
            buildPreview.transform.position,
            buildPreview.transform.rotation,
            targetBuildableObject.NetworkObjectId,
            currentBuildableEdge.localPosition
        );
    }

    private void InvertRampDirection()
    {
        if (currentTileEdge == null || buildPreview == null) return;
        if (currentBuildType != BuildableType.Ramp) return;

        Quaternion rotation = InvertRampRotation(currentTileEdge.direction);
        buildPreview.transform.rotation = rotation;
        // UpdateRampPreview(currentTileEdge);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceRampServerRpc(Vector3 position, Quaternion rotation, Vector3 edgeStart, Vector3 edgeEnd, EdgeDirection direction)
    {
        Debug.Log($"PlaceRampServerRpc called by: {(IsHost ? "Host" : "Client")} | IsOwner: {IsOwner} | IsServer: {IsServer} | IsClient: {IsClient}");
        GameObject ramp = Instantiate(rampPrefab, position, rotation);
        NetworkObject networkObject = ramp.GetComponent<NetworkObject>();
        networkObject.Spawn();

        UpdateEdgeStateClientRpc(edgeStart, edgeEnd, direction);
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

    [ServerRpc(RequireOwnership = false)]
    private void PlaceConnectorServerRpc(Vector3 position, Quaternion rotation, ulong targetObjectId, Vector3 targetEdgeLocalPos)
    {
        GameObject connector = Instantiate(connectorPrefab, position, rotation);
        NetworkObject networkObject = connector.GetComponent<NetworkObject>();
        networkObject.Spawn();

        NetworkObject targetNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetObjectId];
        BuildableObject targetBuildable = targetNetObj.GetComponent<BuildableObject>();

        UpdateBuildableEdgeStateClientRpc(targetObjectId, targetEdgeLocalPos);
    }

    [ClientRpc]
    private void UpdateBuildableEdgeStateClientRpc(ulong targetObjectId, Vector3 targetEdgeLocalPos)
    {
        NetworkObject targetNetObj = null;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(targetObjectId))
        {
            targetNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetObjectId];
            BuildableObject targetBuildable = targetNetObj.GetComponent<BuildableObject>();
            if (targetBuildable != null)
            {
                var edge = targetBuildable.connectableEdges.Find(e => Vector3.Distance(e.localPosition, targetEdgeLocalPos) < 0.1f);
                if (edge != null)
                {
                    edge.isOccupied = true;
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
            HideBuildPreview();
        }
    }

    private void HideBuildPreview()
    {
        if (buildPreview != null)
        {
            buildPreview.SetActive(false);
        }
    }
}
