using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Linq;

public class BuildingSystem : NetworkBehaviour
{
    [Header("Building Types")]
    public BuildableType currentBuildType = BuildableType.Ramp;

    [Header("Ramp Prefabs")]
    public GameObject rampPrefab;
    public GameObject rampGhostPrefab;

    [Header("Ramp Prefabs")]
    public GameObject connectorPrefab;
    public GameObject connectorGhostPrefab;

    [Header("Ramp Prefabs")]
    public GameObject platformPrefab;
    public GameObject platformGhostPrefab;

    [Header("Preview Materials")]
    public Material validPreviewMaterial;
    public Material invalidPreviewMaterial;
    public Material noWoodPreviewMaterial;

    [Header("Building Settings")]
    public float snapDistance = 1f;
    public float maxBuildDistance = 10f;
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
    private ulong targetBuildableObjectId;
    private BuildableObject ghostBuildableObject;
    public bool inBuildMode = false;
    private Movement movement;
    private PlayerInventory inventory;

    [Header("Validity")]
    public bool isValidPlacement = true;
    private List<Renderer> previewRenderers = new List<Renderer>();
    private HashSet<Collider> overlappingColliders = new HashSet<Collider>();

    public event System.Action<bool> OnBuildModeChanged;

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
        
        inventory = GetComponent<PlayerInventory>();
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
            buildPreview.SetActive(false);
            ghostBuildableObject = buildPreview.GetComponent<BuildableObject>();
            rampPreviewComponent = buildPreview.GetComponent<RampObject>();

            previewRenderers.AddRange(buildPreview.GetComponentsInChildren<Renderer>());

            foreach (Collider col in buildPreview.GetComponentsInChildren<Collider>())
            {
                col.isTrigger = true;
            }

            PreviewTriggerHandler triggerHandler = null;
            if (currentBuildType != BuildableType.Ramp)
            {
                triggerHandler = buildPreview.AddComponent<PreviewTriggerHandler>();
            }
            else
            {
                triggerHandler = buildPreview.transform.GetChild(0).gameObject.AddComponent<PreviewTriggerHandler>();
            }
            triggerHandler.Initialize(this, buildPreview.GetComponent<BuildableObject>());
        }

        movement = GetComponent<Movement>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || playerCamera == null) return;

        if (inBuildMode)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetBuildType(BuildableType.Ramp);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetBuildType(BuildableType.Connector);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetBuildType(BuildableType.Platform);
            }

            HandleMouseDetection();

            if (Input.GetKeyDown(KeyCode.X))
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
            SoundManager.Instance.PlayOneShot("Toggle");
        }
    }

    private void UpdateBuildPreview()
    {
        if (buildPreview != null)
        {
            Destroy(buildPreview);
        }

        previewRenderers.Clear();
        overlappingColliders.Clear();

        GameObject prefabToSpawn = null;
        switch (currentBuildType)
        {
            case BuildableType.Ramp:
                prefabToSpawn = rampGhostPrefab;
                PlayerHUD.Instance.HandleBuildableToggle(0);
                break;
            case BuildableType.Connector:
                prefabToSpawn = connectorGhostPrefab;
                PlayerHUD.Instance.HandleBuildableToggle(1);
                break;
            case BuildableType.Platform:
                prefabToSpawn = platformGhostPrefab;
                PlayerHUD.Instance.HandleBuildableToggle(2);
                break;
        }

        if (prefabToSpawn != null)
        {
            buildPreview = Instantiate(prefabToSpawn);
            buildPreview.SetActive(true);
            ghostBuildableObject = buildPreview.GetComponent<BuildableObject>();

            previewRenderers.AddRange(buildPreview.GetComponentsInChildren<Renderer>());

            foreach (Collider col in buildPreview.GetComponentsInChildren<Collider>())
            {
                col.isTrigger = true;
            }

            PreviewTriggerHandler triggerHandler = null;
            if (currentBuildType != BuildableType.Ramp)
            {
                triggerHandler = buildPreview.AddComponent<PreviewTriggerHandler>();
            }
            else
            {
                triggerHandler = buildPreview.transform.GetChild(0).gameObject.AddComponent<PreviewTriggerHandler>();
            }
            triggerHandler.Initialize(this, buildPreview.GetComponent<BuildableObject>());
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
            case BuildableType.Platform:
                HandlePlatformPlacement(ray);
                break;
        }
        // Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);
    }

    // private void HandleRampPlacement(Ray ray)
    // {
    //     RaycastHit hit;
    //     bool isValidPlacement = false;
        
    //     if (Physics.Raycast(ray, out hit, maxBuildDistance, tileLayer))
    //     {
    //         TileEdges tile = hit.collider.GetComponent<TileEdges>();
    //         if (tile != null && currentBuildType == BuildableType.Ramp)
    //         {
    //             // Debug.Log($"[{Time.frameCount}] FindClosestEdge called by: {(IsHost ? "Host" : "Client")} | IsOwner: {IsOwner}");
    //             isValidPlacement = true;
    //             FindClosestEdge(tile, hit.point);
    //             return;
    //         }
    //     }

    //     if (!isValidPlacement && Physics.Raycast(ray, out hit, maxBuildDistance, buildableLayer))
    //     {
    //         BuildableObject targetObject = hit.collider.GetComponentInParent<BuildableObject>();
    //         if (targetObject != null)
    //         {
    //             isValidPlacement = true;
    //             FindClosestBuildableEdge(targetObject, hit.point);
    //             return;
    //         }
    //     }
        
    //     if (!isValidPlacement || hit.collider == null)
    //     {
    //         if (currentTileEdge != null) return;
    //         // Debug.Log("No hit");
    //         HideBuildPreview();
    //         currentTileEdge = null;
    //         // currentBuildableEdge = null;
    //         // targetBuildableObject = null;
    //     }
    // }

    private void HandleRampPlacement(Ray ray)
    {
        RaycastHit hit;
        bool hitSomething = false;
        float maxPreviewDistance = maxBuildDistance + 2f;

        if (Physics.Raycast(ray, out hit, maxPreviewDistance, tileLayer | buildableLayer))
        {
            hitSomething = true;
            float distanceToHit = Vector3.Distance(transform.position, hit.point);

            if (distanceToHit > maxBuildDistance) 
            {
                HideBuildPreview();
                currentTileEdge = null;
                currentBuildableEdge = null;
                targetBuildableObject = null;
                targetBuildableObjectId = 0;
                return;
            }
        }

        if (!hitSomething)
        {
            Debug.Log("NOTHING HIT");
            HideBuildPreview();
            currentTileEdge = null;
            currentBuildableEdge = null;
            targetBuildableObject = null;
            targetBuildableObjectId = 0;
            return;
        }

        bool foundValidPlacement = false;

        if (Physics.Raycast(ray, out hit, maxBuildDistance, tileLayer))
        {
            Debug.Log("HIT TILE: " + hit.collider.name);
            TileEdges tile = hit.collider.GetComponent<TileEdges>();
            if (tile != null && currentBuildType == BuildableType.Ramp)
            {
                foundValidPlacement = true;

                if (currentTileEdge == null || !IsSameEdge(currentTileEdge, tile, hit.point))
                {
                    FindClosestEdge(tile, hit.point);
                }
                return;
            }
        }

        if (!foundValidPlacement && Physics.Raycast(ray, out hit, maxBuildDistance, buildableLayer))
        {
            BuildableObject targetObject = hit.collider.GetComponentInParent<BuildableObject>();
            if (targetObject != null)
            {
                if (targetBuildableObject != targetObject ||
                    currentBuildableEdge == null ||
                    !IsSameEdge(currentBuildableEdge, targetObject, hit.point))
                {
                    FindClosestBuildableEdge(targetObject, hit.point);
                }
                return;
            }
        }
    }

    private bool IsSameEdge(TileEdge currentEdge, TileEdges tile, Vector3 hitPoint)
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

        return closestEdge == currentEdge;
    }

    private bool IsSameEdge(BuildableEdge currentEdge, BuildableObject targetObject, Vector3 hitPoint)
    {
        float closestDistance = snapDistance;
        BuildableEdge closestEdge = null;

        foreach (BuildableEdge edge in targetObject.GetAvailableEdges())
        {
            if (!edge.allowedConnections.Contains(currentBuildType)) continue;
            if (edge.isOccupied) continue;

            Vector3 edgeWorldPos = targetObject.GetWorldEdgePosition(edge);
            float distance = Vector3.Distance(hitPoint, edgeWorldPos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEdge = edge;
            }
        }

        return closestEdge == currentEdge;
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
                targetBuildableObjectId = 0;
            }
        }
    }

    private void HandlePlatformPlacement(Ray ray)
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
                targetBuildableObjectId = 0;
            }
        }
    }

    private void FindClosestBuildableEdge(BuildableObject target, Vector3 hitPoint)
    {
        float closestDistance = snapDistance;
        BuildableEdge closestEdge = null;

        foreach (BuildableEdge edge in target.GetAvailableEdges())
        {
            if (!edge.allowedConnections.Contains(currentBuildType)) continue;
            if (edge.isOccupied) continue;

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
            if (closestEdge.isOccupied) return;
            currentBuildableEdge = closestEdge;
            targetBuildableObject = target;
            targetBuildableObjectId = target.NetworkObjectId;

            switch (currentBuildType)
            {
                case BuildableType.Ramp:
                    UpdateRampPreviewOnBuildable(target, closestEdge);
                    break;
                case BuildableType.Connector:
                    UpdateConnectorPreview(target, closestEdge);
                    break;
                case BuildableType.Platform:
                    UpdatePlatformPreview(target, closestEdge);
                    break;
            }
        }
        else if (closestEdge == null)
        {
            HideBuildPreview();
            currentBuildableEdge = null;
            targetBuildableObject = null;
            targetBuildableObjectId = 0;
        }
    }

    private void UpdateRampPreviewOnBuildable(BuildableObject target, BuildableEdge edge)
    {
        if (buildPreview == null || !inBuildMode) return;

        buildPreview.SetActive(true);
        Vector3 edgeWorldPos = target.GetWorldEdgePosition(edge);
        EdgeDirection oppositeDirection = GetOppositeDirection(edge.direction);

        Quaternion targetRotation = target.transform.rotation;
        Quaternion rotation = Quaternion.identity;
        switch (target.type)
        {
            case BuildableType.Ramp:
                rotation = GetRampRotation(oppositeDirection);
                buildPreview.transform.rotation = targetRotation;
                break;
            case BuildableType.Platform:
                rotation = GetRampRotation(oppositeDirection);
                buildPreview.transform.rotation = targetRotation;
                break;
            case BuildableType.Connector:
                buildPreview.transform.rotation = targetRotation;
                buildPreview.transform.Rotate(0, GetConnectorRotationAngle(edge.direction), 0);
                break;
        }

        buildPreview.transform.position = edgeWorldPos;
    }

    private void UpdatePlatformPreview(BuildableObject target, BuildableEdge edge)
    {
        if (buildPreview == null || !inBuildMode) return;

        buildPreview.SetActive(true);
        Vector3 edgeWorldPos = target.GetWorldEdgePosition(edge);
        EdgeDirection oppositeDirection = GetOppositeDirection(edge.direction);

        Quaternion targetRotation = target.transform.rotation;
        Quaternion rotation = Quaternion.identity;
        switch(target.type)
        {
            case BuildableType.Ramp:
                rotation = GetRampRotation(oppositeDirection);
                buildPreview.transform.rotation = targetRotation;
                break;
            case BuildableType.Platform:
                rotation = GetRampRotation(oppositeDirection);
                buildPreview.transform.rotation = targetRotation;
                break;
            case BuildableType.Connector:
                buildPreview.transform.rotation = targetRotation;
                buildPreview.transform.Rotate(0, GetConnectorRotationAngle(edge.direction), 0);
                break;
        }

        buildPreview.transform.position = edgeWorldPos;
    }

    private void UpdateConnectorPreview(BuildableObject target, BuildableEdge edge)
    {
        if (buildPreview == null || !inBuildMode) return;

        buildPreview.SetActive(true);
        Vector3 edgeWorldPos = target.GetWorldEdgePosition(edge);
        EdgeDirection oppositeDirection = GetOppositeDirection(edge.direction);

        buildPreview.transform.position = edgeWorldPos;
        buildPreview.transform.rotation = target.transform.rotation;

        buildPreview.transform.Rotate(0, GetConnectorRotationAngle(edge.direction), 0);
    }

    private float GetConnectorRotationAngle (EdgeDirection direction)
    {
        switch (direction)
        {
            case EdgeDirection.North: return 0;
            case EdgeDirection.South: return 180;
            case EdgeDirection.East: return 90;
            case EdgeDirection.West: return 270;
            default: return 0;
        }
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
            Debug.Log("COULD NOT FIND CLOSEST EDGE. HIDING PREVIEW.");
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
                if (inventory.wood.Value < 2) return;
                TryPlaceRamp();
                break;
            case BuildableType.Connector:
                if (inventory.wood.Value < 1) return;
                TryPlaceConnector();
                break;
            case BuildableType.Platform:
                if (inventory.wood.Value < 2) return;
                TryPlacePlatform();
                break;
        }
    }

    private void TryPlaceRamp()
    {
        if (!isValidPlacement) return;

        if (currentTileEdge != null)
        {
            PlaceRampServerRpc(
                buildPreview.transform.position,
                buildPreview.transform.rotation,
                currentTileEdge.startPoint,
                currentTileEdge.endPoint,
                currentTileEdge.direction
            );
        }
        else if (currentBuildableEdge != null && targetBuildableObject != null)
        {
            PlaceRampOnBuildableServerRpc(
                buildPreview.transform.position,
                buildPreview.transform.rotation,
                // targetBuildableObject.NetworkObjectId,
                targetBuildableObjectId,
                currentBuildableEdge.localPosition
            );
        }

        // Check if player has enough wood

        // Check movement related logic

    }

    private void TryPlaceConnector()
    {
        if (!isValidPlacement) return;
        if (currentBuildableEdge == null || targetBuildableObject == null) return;
        Debug.Log($"TryPlaceConnector - Target Object: {targetBuildableObject.name}, NetworkObjectId: {targetBuildableObject.NetworkObjectId}, IsSpawned: {targetBuildableObject.IsSpawned}");
    
        PlaceConnectorServerRpc(
            buildPreview.transform.position,
            buildPreview.transform.rotation,
            // targetBuildableObject.NetworkObjectId,
            targetBuildableObjectId,
            currentBuildableEdge.localPosition
        );
    }

    private void TryPlacePlatform()
    {
        if (!isValidPlacement) return;
        if (currentBuildableEdge == null || targetBuildableObject == null) return;
        Debug.Log($"TryPlacePlatform - Target Object: {targetBuildableObject.name}, NetworkObjectId: {targetBuildableObject.NetworkObjectId}, IsSpawned: {targetBuildableObject.IsSpawned}");

        PlacePlatformServerRpc(
            buildPreview.transform.position,
            buildPreview.transform.rotation,
            // targetBuildableObject.NetworkObjectId,
            targetBuildableObjectId,
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
        inventory.RemoveWood(2);

        PlaySoundClientRpc("PlaceBuildable", (edgeStart + edgeEnd) / 2f);
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
        inventory.RemoveWood(1);

        NetworkObject targetNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetObjectId];
        BuildableObject targetBuildable = targetNetObj.GetComponent<BuildableObject>();

        PlaySoundClientRpc("PlaceBuildable", position);
        UpdateBuildableEdgeStateClientRpc(targetObjectId, targetEdgeLocalPos);
    }

    [ClientRpc]
    private void UpdateBuildableEdgeStateClientRpc(ulong targetObjectId, Vector3 targetEdgeLocalPos)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId,out NetworkObject targetNetObj)) return;

        BuildableObject targetBuildable = targetNetObj.GetComponent<BuildableObject>();
        if (targetBuildable == null) return;

        BuildableEdge targetEdge = targetBuildable.connectableEdges.Find(e => Vector3.Distance(e.localPosition, targetEdgeLocalPos) < 0.1f);
        if (targetEdge != null)
        {
            targetEdge.isOccupied = true;
            // Vector3 connectionWorldPos = targetBuildable.GetWorldEdgePosition(targetEdge);

            // Collider[] colliders = Physics.OverlapSphere(connectionWorldPos, 0.5f, buildableLayer);
            // foreach (Collider col in colliders)
            // {
            //     BuildableObject nearbyBuildable = col.GetComponentInParent<BuildableObject>();
            //     if (nearbyBuildable != null && nearbyBuildable != targetBuildable)
            //     {
            //         foreach (BuildableEdge edge in nearbyBuildable.connectableEdges)
            //         {
            //             Vector3 edgeWorldPos = nearbyBuildable.GetWorldEdgePosition(edge);
            //             if (Vector3.Distance(edgeWorldPos, connectionWorldPos) < 0.5f)
            //             {
            //                 edge.isOccupied = true;
            //             }
            //         }
            //     }
            // }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlacePlatformServerRpc(Vector3 position, Quaternion rotation, ulong targetObjectId, Vector3 targetEdgeLocalPos)
    {
        GameObject platform = Instantiate(platformPrefab, position, rotation);
        NetworkObject networkObject = platform.GetComponent<NetworkObject>();
        networkObject.Spawn();
        inventory.RemoveWood(2);

        NetworkObject targetNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetObjectId];
        BuildableObject targetBuildable = targetNetObj.GetComponent<BuildableObject>();
        PlaySoundClientRpc("PlaceBuildable", position);

        UpdateBuildableEdgeStateClientRpc(targetObjectId, targetEdgeLocalPos);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceRampOnBuildableServerRpc(Vector3 position, Quaternion rotation, ulong targetObjectId, Vector3 targetEdgeLocalPos)
    {
        GameObject ramp = Instantiate(rampPrefab, position, rotation);
        NetworkObject networkObject = ramp.GetComponent<NetworkObject>();
        networkObject.Spawn();
        inventory.RemoveWood(2);

        NetworkObject targetNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetObjectId];
        BuildableObject targetBuildable = targetNetObj.GetComponent<BuildableObject>();
        PlaySoundClientRpc("PlaceBuildable", position);

        UpdateBuildableEdgeStateClientRpc(targetObjectId, targetEdgeLocalPos);
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
        OnBuildModeChanged?.Invoke(inBuildMode);
        switch (currentBuildType)
        {
            case BuildableType.Ramp:
                PlayerHUD.Instance.HandleBuildToggle(inBuildMode, 0);
                break;
            case BuildableType.Connector:
                PlayerHUD.Instance.HandleBuildToggle(inBuildMode, 1);
                break;
            case BuildableType.Platform:
                PlayerHUD.Instance.HandleBuildToggle(inBuildMode, 2);
                break;
        }

        if (!inBuildMode)
        {
            HideBuildPreview();
            SoundManager.Instance.PlayOneShot("TurnOffSystem");
        }
        else
        {
            SoundManager.Instance.PlayOneShot("ToggleBuildable");
        }
    }

    private void HideBuildPreview()
    {
        if (buildPreview != null)
        {
            UpdatePreviewMaterial(true);
            buildPreview.SetActive(false);
            isValidPlacement = true;
        }
    }

    public void HandleCollisionEnter(bool colliding, Collider other)
    {
        if (other == null) 
        {
            overlappingColliders.Clear();
            UpdatePreviewValidity();
            return;
        }
        // Debug.Log("Target object: " + targetBuildableObject);
        if (targetBuildableObject != null && other.transform.IsChildOf(targetBuildableObject.transform)) return;

        if (currentTileEdge != null)
        {
            var tileEdges = other.GetComponent<TileEdges>();
            if (tileEdges != null && tileEdges.edges.Contains(currentTileEdge)) return;
        }


        if (colliding) overlappingColliders.Add(other);
        else overlappingColliders.Remove(other);

        UpdatePreviewValidity();
    }

    private void UpdatePreviewValidity()
    {
        isValidPlacement = overlappingColliders.Count == 0;
        bool validButNoWood = false;

        if (isValidPlacement)
        {
            switch (currentBuildType)
            {
                case BuildableType.Ramp:
                    if (inventory.wood.Value < 2) validButNoWood = true;
                    break;
                case BuildableType.Connector:
                    if (inventory.wood.Value < 1) validButNoWood = true;
                    break;
                case BuildableType.Platform:
                    if (inventory.wood.Value < 2) validButNoWood = true;
                    break;
            }
        }
        UpdatePreviewMaterial(isValidPlacement, validButNoWood);
    }

    private void UpdatePreviewMaterial(bool isValid, bool validButNoWood = false)
    {
        Material materialToUse = isValid ? (validButNoWood ? noWoodPreviewMaterial : validPreviewMaterial) : invalidPreviewMaterial;
        foreach (Renderer renderer in previewRenderers)
        {
            renderer.material = materialToUse;
        }
    }

    [ClientRpc]
    private void PlaySoundClientRpc(string name, Vector3 position)
    {
        SoundManager.Instance.PlayAtPosition(name, position);
    }
}
