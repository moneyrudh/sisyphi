using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformObject : BuildableObject
{
    [SerializeField] private Transform[] edgePoints;
    public Vector3 snapDirection;

    private void Awake()
    {
        type = BuildableType.Platform;
        InitializeEdges();
    }

    public override void InitializeEdges()
    {
        snapDirection = transform.forward;
        connectableEdges = new List<BuildableEdge>();

        connectableEdges.Add(new BuildableEdge
        {
            localPosition = Vector3.zero,
            direction = EdgeDirection.North,
            allowedConnections = new[] { BuildableType.Ramp, BuildableType.Platform, BuildableType.Connector }
        });

        if (edgePoints != null)
        {
            foreach (Transform edge in edgePoints)
            {
                if (edge != null)
                {
                    connectableEdges.Add(new BuildableEdge
                    {
                        localPosition = edge.localPosition,
                        direction = DetermineEdgeDirection(edge.localPosition),
                        allowedConnections = new[] { BuildableType.Ramp, BuildableType.Platform, BuildableType.Connector }
                    });
                }
            }
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

        Gizmos.color = Color.blue;
        foreach (BuildableEdge edge in connectableEdges)
        {
            Vector3 worldPos = GetWorldEdgePosition(edge);
            Gizmos.DrawSphere(worldPos, 0.2f);
            Gizmos.DrawRay(worldPos, transform.TransformDirection(GetDirectionVector(edge.direction)));
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
