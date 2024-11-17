using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum EdgeDirection
{
    North,
    East,
    South,
    West
}

[System.Serializable]
public class TileEdge
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public EdgeDirection direction;
    public bool isOccupied;
}

public class TileEdges : NetworkBehaviour
{
    public TileEdge[] edges = new TileEdge[4];
    private float sizeMultiplier = 2f;
    private float demultiplier = 0.91f;

    // private void Awake()
    // {
    //     InitializeEdges();
    // }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            InitializeEdges();
        }
    }

    private void InitializeEdges()
    {
        Vector3 tilePos = transform.position;
        float tileSize;
        if (GetComponent<Renderer>() == null) tileSize = 2f * sizeMultiplier * demultiplier;
        else tileSize = GetComponent<Renderer>().bounds.size.x * sizeMultiplier * demultiplier;
        float halfSize = tileSize / 4f;

        // North
        edges[0] = new TileEdge
        {
            startPoint = new Vector3(tilePos.x - halfSize, tilePos.y + halfSize, tilePos.z + halfSize),
            endPoint = new Vector3(tilePos.x + halfSize, tilePos.y + halfSize, tilePos.z + halfSize),
            direction = EdgeDirection.North
        };

        // East
        edges[1] = new TileEdge
        {
            startPoint = new Vector3(tilePos.x + halfSize, tilePos.y + halfSize, tilePos.z + halfSize),
            endPoint = new Vector3(tilePos.x + halfSize, tilePos.y + halfSize, tilePos.z - halfSize),
            direction = EdgeDirection.East
        };

        // South
        edges[2] = new TileEdge
        {
            startPoint = new Vector3(tilePos.x - halfSize, tilePos.y + halfSize, tilePos.z - halfSize),
            endPoint = new Vector3(tilePos.x + halfSize, tilePos.y + halfSize, tilePos.z - halfSize),
            direction = EdgeDirection.South
        };

        // West
        edges[3] = new TileEdge
        {
            startPoint = new Vector3(tilePos.x - halfSize, tilePos.y + halfSize, tilePos.z - halfSize),
            endPoint = new Vector3(tilePos.x - halfSize, tilePos.y + halfSize, tilePos.z + halfSize),
            direction = EdgeDirection.West
        };

        if (IsServer)
        {
            SyncEdgesClientRpc(tilePos, halfSize);
        }
    }

    [ClientRpc]
    private void SyncEdgesClientRpc(Vector3 tilePos, float halfSize)
    {
        if (IsHost) return;

        // North
        edges[0] = new TileEdge
        {
            startPoint = new Vector3(tilePos.x - halfSize, tilePos.y + halfSize, tilePos.z + halfSize),
            endPoint = new Vector3(tilePos.x + halfSize, tilePos.y + halfSize, tilePos.z + halfSize),
            direction = EdgeDirection.North
        };

        // East
        edges[1] = new TileEdge
        {
            startPoint = new Vector3(tilePos.x + halfSize, tilePos.y + halfSize, tilePos.z + halfSize),
            endPoint = new Vector3(tilePos.x + halfSize, tilePos.y + halfSize, tilePos.z - halfSize),
            direction = EdgeDirection.East
        };

        // South
        edges[2] = new TileEdge
        {
            startPoint = new Vector3(tilePos.x - halfSize, tilePos.y + halfSize, tilePos.z - halfSize),
            endPoint = new Vector3(tilePos.x + halfSize, tilePos.y + halfSize, tilePos.z - halfSize),
            direction = EdgeDirection.South
        };

        // West
        edges[3] = new TileEdge
        {
            startPoint = new Vector3(tilePos.x - halfSize, tilePos.y + halfSize, tilePos.z - halfSize),
            endPoint = new Vector3(tilePos.x - halfSize, tilePos.y + halfSize, tilePos.z + halfSize),
            direction = EdgeDirection.West
        };
    }

    private void OnDrawGizmos()
    {
        if (edges == null || edges.Length == 0) return;

        Gizmos.color = Color.red;
        foreach (var edge in edges)
        {
            if (edge != null)
            {
                Gizmos.DrawLine(edge.startPoint, edge.endPoint);
            }
        }
    }
}
