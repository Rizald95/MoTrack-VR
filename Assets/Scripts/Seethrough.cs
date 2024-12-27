using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.PXR;

public class Seethrough : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        PXR_Manager.EnableVideoSeeThrough = true;
    }

    private void OnApplicationPause(bool pause)
    {
        //if not paused, enable seethrough
        PXR_Manager.EnableVideoSeeThrough = !pause;
    }

}
