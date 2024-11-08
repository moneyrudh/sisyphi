using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;


public enum BuildableType
{
    Ramp,
    Connector,
    Platform
}

[System.Serializable]
public class BuildableEdge
{
    public Vector3 localPosition;
    public EdgeDirection direction;
    public bool isOccupied;
    public BuildableType[] allowedConnections;
}

public class BuildableObject : NetworkBehaviour 
{
    public BuildableType type;
    public List<BuildableEdge> connectableEdges;

    public virtual void InitializeEdges()
    {

    }
    
    public Vector3 GetWorldEdgePosition(BuildableEdge edge)
    {
        return transform.TransformPoint(edge.localPosition);
    }

    public List<BuildableEdge> GetAvailableEdges()
    {
        return connectableEdges.Where(edge => !edge.isOccupied).ToList();
    }
}
