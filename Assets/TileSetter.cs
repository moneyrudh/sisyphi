using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSetter : MonoBehaviour
{
    public GameObject tilePrefab;
    private float curX; 
    private float curY;
    private float curZ;
    // Start is called before the first frame update

    void Start()
    {
        curX = 0;
        curY = 0;
        curZ = 0;
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                float bound = tilePrefab.gameObject.GetComponent<BoxCollider>().bounds.size.x;
                float x = bound * i;
                float y = curY;
                float z = bound * j;
                GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, z), Quaternion.identity);
                tile.transform.SetParent(transform);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
