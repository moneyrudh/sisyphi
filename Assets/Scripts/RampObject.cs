using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RampObject : MonoBehaviour
{
    [SerializeField] Transform snapPoint;
    public Vector3 snapDirection;
    public float rampLength;

    private void Awake()
    {
        CalculateSnapPoint();
    }

    private void CalculateSnapPoint()
    {
        // snapPoint = transform.position;
        snapDirection = transform.forward;
        rampLength = GetComponentInChildren<MeshFilter>().mesh.bounds.size.z;
        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        Debug.Log("Ramp Length: " + rampLength);
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(snapPoint.position, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(snapPoint.position, snapDirection * rampLength);
    }
}
