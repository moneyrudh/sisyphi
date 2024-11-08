using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RampObject : BuildableObject
{
    [SerializeField] Transform snapPoint;
    [SerializeField] Transform topEdgePoint;
    public Vector3 snapDirection;
    public float rampLength;

    private void Awake()
    {
        type = BuildableType.Ramp;
        CalculateSnapPoint();
        InitializeEdges();
    }

    private void CalculateSnapPoint()
    {
        // snapPoint = transform.position;
        snapDirection = transform.forward;
        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            rampLength = meshFilter.mesh.bounds.size.z;
            Debug.Log($"Ramp Length calculated for {gameObject.name}: {rampLength}");
        }
        else
        {
            Debug.LogError("No MeshFilter found on " + gameObject.name);
        }
    }

    public override void InitializeEdges()
    {
        if (topEdgePoint == null)
        {
            Debug.LogError("Top edge point not assigned on " + gameObject.name);
            return;
        }

        connectableEdges = new List<BuildableEdge>();
        connectableEdges.Add(new BuildableEdge{
            localPosition = topEdgePoint.localPosition,
            direction = EdgeDirection.North,
            allowedConnections = new[] { BuildableType.Connector, BuildableType.Platform }
        });
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.2f);
        if (snapPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(snapPoint.position, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(snapPoint.position, snapDirection * rampLength);
        }

        if (topEdgePoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(topEdgePoint.position, 0.15f);
        }
    }
}
