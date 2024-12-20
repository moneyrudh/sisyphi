using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectorObject : BuildableObject
{
    [SerializeField] Transform[] edgePoints;
    public Vector3 snapDirection;

    private void Awake()
    {
        type = BuildableType.Connector;
        InitializeEdges();
    }

    public override void InitializeEdges()
    {
        snapDirection = transform.forward;
        connectableEdges = new List<BuildableEdge>();
        
        // North edge
        connectableEdges.Add(new BuildableEdge{
            localPosition = Vector3.zero,
            direction = EdgeDirection.South,
            allowedConnections = new[] { BuildableType.Ramp, BuildableType.Platform, BuildableType.Connector},
            isOccupied = true
        });
        
        if (edgePoints != null)
        {
            for (int i=0; i<edgePoints.Length; i++)
            {
                if (edgePoints[i] != null)
                {
                    EdgeDirection direction;
                    switch(i)
                    {
                        case 0: direction = EdgeDirection.West; break;
                        case 1: direction = EdgeDirection.North; break;
                        case 2: direction = EdgeDirection.East; break;
                        default: direction = EdgeDirection.South; break;
                    }
 
                    connectableEdges.Add(new BuildableEdge
                    {
                        localPosition = edgePoints[i].localPosition,
                        direction = direction,
                        allowedConnections = new[] { BuildableType.Ramp, BuildableType.Platform, BuildableType.Connector },
                        isOccupied = false
                    });
                }
            }
            // foreach (var edge in connectableEdges)
            // {
            //     Debug.Log($"Edge direction: {edge.direction}");
            // }
        }
    }

    private EdgeDirection DetermineEdgeDirection(Vector3 localPos)
    {
        float xAbs = Mathf.Abs(localPos.x);
        float zAbs = Mathf.Abs(localPos.z);

        if (xAbs > zAbs)
        {
            return localPos.x > 0 ? EdgeDirection.East : EdgeDirection.West;
        }
        else
        {
            return localPos.z > 0 ? EdgeDirection.North : EdgeDirection.South;
        }
    }

    private void OnDrawGizmos()
    {
        if (connectableEdges == null || connectableEdges.Count == 0) return;

        Gizmos.color = Color.red;
        foreach (var edge in connectableEdges)
        {
            Vector3 worldPos = GetWorldEdgePosition(edge);
            Gizmos.DrawSphere(worldPos, 0.2f);
            Gizmos.DrawRay(worldPos, transform.TransformDirection(GetDirectionVector(edge.direction)) * 0.5f);
        }
    }

    private Vector3 GetDirectionVector(EdgeDirection direction)
    {
        switch (direction)
        {
            case EdgeDirection.North: return Vector3.forward;
            case EdgeDirection.East: return Vector3.right;
            case EdgeDirection.South: return Vector3.back;
            case EdgeDirection.West: return Vector3.left;
            default: return Vector3.zero;
        }
    }
}
