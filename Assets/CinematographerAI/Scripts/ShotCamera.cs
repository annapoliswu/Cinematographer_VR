using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotCamera : MonoBehaviour
{
    // Start is called before the first frame update
    public Camera unityCamera;
    void Start()
    {
        unityCamera = GetComponent<Camera>();
    }

}
