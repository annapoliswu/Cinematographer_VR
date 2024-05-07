using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Normal.Realtime;

public class CameraManager : MonoBehaviour
{
    public Camera mainCamera;
    [SerializeField]
    public List<ShotCamera> cameras;
    public float aspectRatio = 1f;

    public int currentCamera = 0;
    public GameObject replayCameraPrefab;
    private List<ReplayCamera> replayCameras;

    public GameObject gridCanvas;
    public GameObject replayScreenPrefab;
    private List<GameObject> replayScreens;
    public GameObject highlightScreen;

    public Camera displayCam; //view for raycast
    public LayerMask mask; //for raycast
    public Slider slider;
    private Vector3 highlightInitPosition;


    private KeyCode[] keyCodes = {
         KeyCode.Alpha1,
         KeyCode.Alpha2,
         KeyCode.Alpha3,
         KeyCode.Alpha4,
         KeyCode.Alpha5,
         KeyCode.Alpha6,
         KeyCode.Alpha7,
         KeyCode.Alpha8,
         KeyCode.Alpha9,
     };

    void Start()
    {
        cameras = new List<ShotCamera>();
        GetCamerasInScene();

        replayCameras = new List<ReplayCamera>();
        replayScreens = new List<GameObject>();

        highlightInitPosition = highlightScreen.transform.position;
        highlightScreen.SetActive(false);
        gridCanvas.SetActive(false);
        Random.InitState(1);

    }

    void Update()
    {

        // Draw Ray
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 100f;
        mousePos = displayCam.ScreenToWorldPoint(mousePos);
        Debug.DrawRay(displayCam.transform.position, mousePos - displayCam.transform.position, Color.blue);

        if (Input.GetMouseButtonDown(0)) 
        {
            Ray ray = displayCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100, mask))
            { 
                
                for (int i = 0; i < replayScreens.Count; i++)
                {
                    if (replayScreens[i] == hit.transform.gameObject)
                    {
                        //Debug.Log(hit.transform.name);
                        ChangeReplayCameraTo(i);
                    }
                }
            }
        }
    }

    private void GetCamerasInScene()
    {
        cameras.Clear();
        cameras.AddRange(FindObjectsOfType<ShotCamera>());
    }

    //create cameras and display screens, shows canvas, LOCAL ONLY
    public void CreateReplayCameras(int numCams)
    {
        currentCamera = 0;
        highlightScreen.transform.position = highlightInitPosition;
        highlightScreen.SetActive(true);
        gridCanvas.SetActive(true);
        for(int i = 0; i < numCams; i++)
        {
            //NOTE: Weird error if cameras are instantiated realtime where other players camera view swapped with last made camera, replay doesn't work networked either
            GameObject replayCameraGO = Instantiate(replayCameraPrefab); //Realtime.Instantiate(replayCameraPrefab.name, new Realtime.InstantiateOptions()); 
            replayCameras.Add(replayCameraGO.GetComponent<ReplayCamera>());
            GameObject replayScreenGO = Instantiate(replayScreenPrefab); //Realtime.Instantiate(replayScreenPrefab.name, new Realtime.InstantiateOptions());
            replayScreenGO.transform.position = gridCanvas.transform.position - new Vector3(0, 0, .05f);
            replayScreenGO.transform.SetParent(gridCanvas.transform);

            RenderTexture rt = new RenderTexture( (int) (256*aspectRatio) , 256, 16, RenderTextureFormat.ARGB32);
            rt.Create();
            replayCameraGO.GetComponent<Camera>().targetTexture = rt; 

            Material mat = new Material(Shader.Find("Unlit/Texture")); //also not sure if this is easily networkable
            mat.mainTexture = rt;
            replayScreenGO.GetComponent<Image>().material = mat;
            replayScreens.Add(replayScreenGO);
        }
    }

    //clear cameras and screens, hides canvas
    public void DeleteReplayCameras()
    {
        for (int i = 0; i < replayCameras.Count; i++)
        {
            Destroy(replayCameras[i].gameObject);
            Destroy(replayScreens[i]);
        }
        replayCameras.Clear();
        replayScreens.Clear();
        gridCanvas.SetActive(false);
        highlightScreen.SetActive(false);
    } 

    public void SetReplayCameras(StateData data)
    {
        for (int i = 0; i < replayCameras.Count; i++)
        {
            ReplayCamera replayCamera = replayCameras[i];
            CameraData cameraData = data.cameraData.cameras[i];

            replayCamera.unityCamera.fieldOfView = cameraData.fov;
            replayCamera.transform.SetPositionAndRotation(cameraData.transform.position, cameraData.transform.rotation);
        }
    }

    //rework later: designate a mainCamera to output video from
    public bool ChangeReplayCameraTo(int index)
    {
        if(index < replayCameras.Count)
        {
            replayCameras[currentCamera].SetCameraOn(false); //turn old cam off

            currentCamera = index;
            Vector3 pos = replayCameras[currentCamera].transform.position;
            Quaternion rot = replayCameras[currentCamera].transform.rotation;
            float fov = replayCameras[currentCamera].unityCamera.fieldOfView; 
            highlightScreen.transform.position = replayScreens[currentCamera].transform.position;
            mainCamera.transform.SetPositionAndRotation(pos, rot); //change main camera here, should be instantiated after the player loads
            mainCamera.fieldOfView = fov;

            replayCameras[currentCamera].SetCameraOn(true);//turn new cam on
            return true;
        }
        else
        {
            return false;
        }
        
    }

    //change to random camera that is not previous camera
    public void ChangeRandomCamera()
    {
        int randomInt = Random.Range(0, replayCameras.Count);

        while (currentCamera == randomInt)
        {
            randomInt = Random.Range(0, replayCameras.Count); // reroll
        }

        ChangeReplayCameraTo(randomInt);
    }

}
