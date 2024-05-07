using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayCamera : MonoBehaviour
{
    // Start is called before the first frame update
    public Camera unityCamera;
    [SerializeField]
    private MeshRenderer meshRenderer;
    [SerializeField]
    private Material onMat;
    [SerializeField]
    private Material offMat;

    void Start()
    {
        unityCamera = GetComponent<Camera>();
    }

    public void SetCameraOn(bool isOn)
    {
        meshRenderer.material = isOn? onMat : offMat;  
    }
}
