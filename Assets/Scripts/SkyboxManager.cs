using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxManager : MonoBehaviour
{
    public Material day;
    public Material night;
    public Material blend;
    // Start is called before the first frame update
    void Start()
    {
        int skyboxIndex = Random.Range(0, 3);
        switch (skyboxIndex)
        {
            case 0:
                RenderSettings.skybox = day;
                break;
            case 1:
                RenderSettings.skybox = night;
                break;
            case 2:
                RenderSettings.skybox = blend;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
