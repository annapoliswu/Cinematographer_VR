using System;
using System.Collections.Generic;
using UnityEngine;
using SILvr.Avatars;
using Normal.Realtime;
using System.IO;
using UnityEngine.Events;
using Python.Runtime;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using TMPro;
#if !UNITY_ANDROID
    using SimpleFileBrowser;
#endif



// Manages writing of data to a 3d recording json file and reading/replay of that data
public class DataManager : MonoBehaviour
{
    #region data collection variables
    public enum State { Record, Replay, WriteLabel, Inactive, PausedReplay, PausedWriteLabel, PausedPredict, Random, PausedRandom, Predict};
    public UnityEvent onDataStateChange;
    [SerializeField]
    private State state = State.Inactive;

    public float delaySeconds = 10f;
    public float invokeInterval = 1f;
    public char splitChar = '|';

    [SerializeField]
    private string fileName;
    private string fileType = ".json";
    private string path;
    private FileStream fileStream;
    private StreamWriter writer;
    private StreamReader reader;
    private string[] strings;
    private int dataIndex;
    private sList<int> labels;

    #endregion

    #region environment objects
    [Header("Environment objects")]
    public CameraManager cameraManager;
    PythonInterfacer pythonInterfacer;
    SILvrAvatarManager avatarManager;
    public GameObject editorCamera; // for viewing the game ui
    public GameObject replayAvatar;
    public List<SILvrAvatar> avatars;
    private List<ReplayAvatar> replayAvatars;
    public TextMeshProUGUI setFileButtonText;
    #endregion

    #region prediction variables
    [SerializeField]
    int prevCameraLabel;
    private float prevCamStartTime;
    List<int> replayLabels;
    public List<QueueItem> historyQueue;
    private string[] transformVariables = {"avatar0.voiceVolume", "avatar0.head.position.x", "avatar0.head.position.y", "avatar0.head.position.z", "avatar0.head.position.w", "avatar0.head.rotation.x", "avatar0.head.rotation.y", "avatar0.head.rotation.z", "avatar0.head.rotation.w", "avatar0.leftHand.position.x", "avatar0.leftHand.position.y", "avatar0.leftHand.position.z", "avatar0.leftHand.position.w", "avatar0.leftHand.rotation.x", "avatar0.leftHand.rotation.y", "avatar0.leftHand.rotation.z", "avatar0.leftHand.rotation.w", "avatar0.rightHand.position.x", "avatar0.rightHand.position.y", "avatar0.rightHand.position.z", "avatar0.rightHand.position.w", "avatar0.rightHand.rotation.x", "avatar0.rightHand.rotation.y", "avatar0.rightHand.rotation.z", "avatar0.rightHand.rotation.w", "avatar0.root.position.x", "avatar0.root.position.y", "avatar0.root.position.z", "avatar0.root.position.w", "avatar0.root.rotation.x", "avatar0.root.rotation.y", "avatar0.root.rotation.z", "avatar0.root.rotation.w", "avatar1.voiceVolume", "avatar1.head.position.x", "avatar1.head.position.y", "avatar1.head.position.z", "avatar1.head.position.w", "avatar1.head.rotation.x", "avatar1.head.rotation.y", "avatar1.head.rotation.z", "avatar1.head.rotation.w", "avatar1.leftHand.position.x", "avatar1.leftHand.position.y", "avatar1.leftHand.position.z", "avatar1.leftHand.position.w", "avatar1.leftHand.rotation.x", "avatar1.leftHand.rotation.y", "avatar1.leftHand.rotation.z", "avatar1.leftHand.rotation.w", "avatar1.rightHand.position.x", "avatar1.rightHand.position.y", "avatar1.rightHand.position.z", "avatar1.rightHand.position.w", "avatar1.rightHand.rotation.x", "avatar1.rightHand.rotation.y", "avatar1.rightHand.rotation.z", "avatar1.rightHand.rotation.w", "avatar1.root.position.x", "avatar1.root.position.y", "avatar1.root.position.z", "avatar1.root.position.w", "avatar1.root.rotation.x", "avatar1.root.rotation.y", "avatar1.root.rotation.z", "avatar1.root.rotation.w"};
    Dictionary<string, float> featureDict;

    Dictionary<string, float> prevObservationDict;
    Dictionary<string, float> runningMeanDict;
    Dictionary<string, float> runningVDict;

    int randomDuration = 0; 
    #endregion


    // Start is called before the first frame update
    void Start()
    {
        avatarManager = FindObjectOfType<SILvrAvatarManager>();

        dataIndex = 1;
        labels = new sList<int>();
        replayAvatars= new List<ReplayAvatar>();
        avatars = new List<SILvrAvatar>();
        if (onDataStateChange == null)
            onDataStateChange = new UnityEvent();

        //path = Application.persistentDataPath + "/";

#if !UNITY_ANDROID
            path = Application.absoluteURL + "Assets/CinematographerAI/Output/";
            editorCamera.SetActive(true);
            pythonInterfacer = FindObjectOfType<PythonInterfacer>();
            UnityEngine.Random.InitState(1);
#endif


    }

    void Update()
    {
        //keyboard shortcuts for convenient testing
#if !UNITY_ANDROID
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePauseReplay();
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                InvokeReplayAndWriteLabel();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                InvokeReplayData();
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                InvokeRecordData();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                InvokeReplayAndPredict();
            }else if (Input.GetKeyDown(KeyCode.O))
            {
                InvokeReplayRandom();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                StopInvoking();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                pythonInterfacer.ChangeModel(PythonInterfacer.ModelType.Researcher);
            }
             else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                pythonInterfacer.ChangeModel(PythonInterfacer.ModelType.Expert);
            }
#endif
    }


    //when dragging slider during replay, pause and set 3d video timestamp to slider place
    public void OnSliderDrag()
    {
        if ( state == State.PausedReplay || state == State.PausedWriteLabel || state == State.Replay || state == State.WriteLabel ) //is replaying
        {
            PauseReplay();
            SetDataIndex((int)cameraManager.slider.value);
        }
    }

    //make sure event is broadcast when state changes
    private void ChangeState(State newState)
    {
        if(state != newState) { 
            state = newState;
            onDataStateChange?.Invoke();
        }
    }
    public State GetState()
    {
        return state;
    }

    //on stop button: reset everything, additional cleanup
    public void StopInvoking()
    {
        dataIndex = 1;
        cameraManager.slider.value = dataIndex;

        foreach (ReplayAvatar avatar in replayAvatars)
        {
            Realtime.Destroy(avatar.gameObject);
        }
        replayAvatars.Clear();
        cameraManager.DeleteReplayCameras();

        strings = null;
        replayLabels = null;
        randomDuration = 0;
        if (labels.list.Count > 0)
        {
            string jsonString = JsonUtility.ToJson(labels);
            writer.Write(jsonString); //any label obj output here
            labels.list.Clear();
        }
        writer?.Close();
        fileStream?.Close();
        writer = null;
        fileStream = null;

        CancelInvoke(nameof(RecordData));  CancelInvoke(nameof(ReplayData)); CancelInvoke(nameof(ReplayAndWriteLabel)); CancelInvoke(nameof(ReplayAndPredict));
        CancelInvoke(nameof(ReplayRandom));

        ChangeState(State.Inactive);
    }

    /*--------------------------------------------------- Functions Called Upon Outside Trigger ---------------------------------------------------------------------*/

    //get avatars in scene, setup writer, invoke writing
    public void InvokeRecordData()
    {
        if (state == State.Inactive)
        {
            avatars.Clear();
            avatars.AddRange(FindObjectsOfType<SILvrAvatar>());
            fileName = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
            SetWriter(fileName);
            EnvironmentData envData = new EnvironmentData(invokeInterval, cameraManager.cameras.Count, avatars.Count);
            writer.Write(JsonUtility.ToJson(envData) + splitChar);

            InvokeRepeating(nameof(RecordData), delaySeconds, invokeInterval);
            ChangeState(State.Record);
        }
    }

    //read file, invoke replay
    public void InvokeReplayData()
    {
        if (state == State.Inactive && SetFile(fileName)) //if choose label file, sets file to label file
        {
            if (fileName.Contains("Labels")) 
            {
                replayLabels = JsonUtility.FromJson<sList<int>>(ReadEntireStream(fileStream)).list;
            }

            if (path.Contains("Labels"))
            {
                using (FileStream tempFilestream = GetNewFileStream(path + "../Data/" + fileName.Split("_")[0] + ".json"))
                {
                    strings = ReadEntireStream(tempFilestream).Split(splitChar);
                }
            }
            else
            {
                strings = ReadEntireStream(fileStream).Split(splitChar);
            }
            
            

            EnvironmentData envData = JsonUtility.FromJson<EnvironmentData>(strings[0]);
            for (int i = 0; i < envData.numAvatars; i++)
            {
                GameObject go = Realtime.Instantiate(replayAvatar.name, new Realtime.InstantiateOptions()); ///this causes network issues where voice volume stops recording and or transmitting??
                replayAvatars.Add(go.GetComponent<ReplayAvatar>());
            }
            cameraManager.CreateReplayCameras(envData.numCameras);
            cameraManager.slider.minValue = 1;
            cameraManager.slider.maxValue = (strings.Length - 1);

            InvokeRepeating(nameof(ReplayData), delaySeconds, envData.invokeInterval);
            ChangeState(State.Replay);

        }
    }

    //read file, invoke replay and write (one invoke function to ensure execution order)
    public void InvokeReplayAndWriteLabel()
    {
        if (state == State.Inactive && SetFile(fileName))
        {
            strings = ReadEntireStream(fileStream).Split(splitChar);
            EnvironmentData envData = JsonUtility.FromJson<EnvironmentData>(strings[0]);

            int fileIndex = PlayerPrefs.GetInt("FileIndex", 0);
            SetWriter(fileName + "_Labels"); //set name

            for (int i = 0; i < envData.numAvatars; i++) //instantiate replay stuff
            {
                GameObject go = Realtime.Instantiate(replayAvatar.name, new Realtime.InstantiateOptions());
                replayAvatars.Add(go.GetComponent<ReplayAvatar>());
            }

            for(int i = 1; i < (strings.Length-1); i++)
            {
                labels.list.Add(-1);
            }
            
            cameraManager.CreateReplayCameras(envData.numCameras);
            cameraManager.slider.minValue = 1;
            cameraManager.slider.maxValue = (strings.Length - 1);

            InvokeRepeating(nameof(ReplayAndWriteLabel), delaySeconds, envData.invokeInterval);
            ChangeState(State.WriteLabel);
        }
    }

    public void InvokeReplayAndPredict()
    {
        if (state == State.Inactive && SetFile(fileName))
        {

            //create feature vector dictionary
            featureDict = new Dictionary<string, float>();
            runningMeanDict = new Dictionary<string, float>();
            runningVDict = new Dictionary<string, float>();
            prevObservationDict = new Dictionary<string, float>();
            prevCameraLabel = -1;

            featureDict.Add("camera.prev", -1);
            featureDict.Add("camera.prevDuration", 0);
            featureDict.Add("camera.mean", -1);
            featureDict.Add("camera.var", 0);
            runningMeanDict.Add("camera", 0);
            runningVDict.Add("camera", 0);
            foreach (string tVar in transformVariables)
            {
                featureDict.Add(tVar + ".mean", -1);
                featureDict.Add(tVar + ".var", 0);
                runningMeanDict.Add(tVar, 0);
                runningVDict.Add(tVar, 0);
            }
            //------------------------------------

            strings = ReadEntireStream(fileStream).Split(splitChar);
            EnvironmentData envData = JsonUtility.FromJson<EnvironmentData>(strings[0]);
            for (int i = 0; i < envData.numAvatars; i++)
            {
                GameObject go = Realtime.Instantiate(replayAvatar.name, new Realtime.InstantiateOptions());
                replayAvatars.Add(go.GetComponent<ReplayAvatar>());
            }
            historyQueue = new List<QueueItem>();
            cameraManager.CreateReplayCameras(envData.numCameras);
            cameraManager.slider.minValue = 1;
            cameraManager.slider.maxValue = (strings.Length - 1);

            InvokeRepeating(nameof(ReplayAndPredict), delaySeconds, envData.invokeInterval);
            ChangeState(State.Predict);

        }
    }

    public void InvokeReplayRandom()
    {
        if (state == State.Inactive && SetFile(fileName))
        {
            strings = ReadEntireStream(fileStream).Split(splitChar);
            EnvironmentData envData = JsonUtility.FromJson<EnvironmentData>(strings[0]);
            for (int i = 0; i < envData.numAvatars; i++)
            {
                GameObject go = Realtime.Instantiate(replayAvatar.name, new Realtime.InstantiateOptions());
                replayAvatars.Add(go.GetComponent<ReplayAvatar>());
            }

            cameraManager.CreateReplayCameras(envData.numCameras);
            cameraManager.slider.minValue = 1;
            cameraManager.slider.maxValue = (strings.Length - 1);

            randomDuration = 0;
            InvokeRepeating(nameof(ReplayRandom), delaySeconds, envData.invokeInterval);

            ChangeState(State.Random);
        }
    }


    private void ReplayRandom()
    {
        if (dataIndex < strings.Length - 1)
        {
            StateData data = JsonUtility.FromJson<StateData>(strings[dataIndex]);
            SetSceneWithData(data);

            if (randomDuration == 0)
            {
                cameraManager.ChangeRandomCamera();
                randomDuration = UnityEngine.Random.Range(50, 400);
                print("rand: " + randomDuration);
            }
            else
            {
                randomDuration--;
            }
        }
        else
        {
            StopInvoking();
        }
    }

    /*-------------------------------------------------------- Functions Called in Invoke ---------------------------------------------------------------------*/

    private void RecordData()
    {
        StateData stateData = new StateData(cameraManager, avatars);
        string jsonString = JsonUtility.ToJson(stateData) + splitChar;  //will leave one extra , before end. move to format after
        writer.Write(jsonString);
    }

    private void ReplayData()
    {
        if (dataIndex < strings.Length - 1)
        {
            StateData data = JsonUtility.FromJson<StateData>(strings[dataIndex]);
            SetSceneWithData(data);
            
            if(replayLabels != null)
            {
                int cameraLabel = replayLabels[dataIndex - 1];
                cameraManager.ChangeReplayCameraTo(cameraLabel);
            }
        }
        else
        {
            StopInvoking();
        }
    }

    private void ReplayAndWriteLabel()
    {
        if (dataIndex < strings.Length - 1)
        {
            int cameraLabel = cameraManager.currentCamera;
            labels.list[dataIndex - 1] = cameraLabel;
            StateData data = JsonUtility.FromJson<StateData>(strings[dataIndex]);
            SetSceneWithData(data);
        }
        else
        {  
            StopInvoking();
        }
    }

    private void AddAllTransformsToDict(Dictionary<string, float> dict, int prevCameraLabel, StateData prevStateData)
    {
        dict.Add("camera", prevCameraLabel);
        AvatarData[] avatars = { prevStateData.avatarData.avatars[0], prevStateData.avatarData.avatars[1] };
        for (int a = 0; a < avatars.Length; a++)
        {
            dict.Add("avatar" + a + ".voiceVolume", avatars[a].voiceVolume);
            AddTransformToDict(dict, avatars[a].headTransform, "avatar" + a + ".head");
            AddTransformToDict(dict, avatars[a].leftHandTransform, "avatar" + a + ".leftHand");
            AddTransformToDict(dict, avatars[a].rightHandTransform, "avatar" + a + ".rightHand");
            AddTransformToDict(dict, avatars[a].rootTransform, "avatar" + a + ".root");
        }
    }

    private void AddTransformToDict(Dictionary<string, float> dict, sTransform sTransf, string avatarPartName)
    { 
        string[] transformPartNames = { ".position.x", ".position.y", ".position.z", ".position.w", ".rotation.x", ".rotation.y", ".rotation.z", ".rotation.w" };
        float[] transformPartValue = { sTransf.position.x, sTransf.position.y, sTransf.position.z, sTransf.position.w, sTransf.rotation.x, sTransf.rotation.y, sTransf.rotation.z, sTransf.rotation.w };
        for (int i = 0; i < transformPartNames.Length; i++)
        {
            dict.Add(avatarPartName + transformPartNames[i], transformPartValue[i]);
        }
    }

    private void ReplayAndPredict()
    {
        if (dataIndex < strings.Length - 1)
        {
            StateData firstData = JsonUtility.FromJson<StateData>(strings[1]);
            StateData data = JsonUtility.FromJson<StateData>(strings[dataIndex]);
            SetSceneWithData(data);
            int index = dataIndex - 2;


            int chunkSize = 100; /***************************************  IMPORTANT REPLACE **************************************************/

            if (index == 0) //set duration start time once at beginning
            {
                prevCamStartTime = data.timeElapsed;
            }
            else  //once we have 1 existing item in our history queue, can start calculating running variables
            {
                StateData prevData = JsonUtility.FromJson<StateData>(strings[dataIndex - 1]);
                prevObservationDict = new Dictionary<string, float>();
                AddAllTransformsToDict(prevObservationDict, prevCameraLabel, prevData); //record all observations for index-1
                historyQueue.Add(new QueueItem(prevObservationDict));

                if (index < chunkSize) { 
                    foreach (string key in prevObservationDict.Keys)
                    {
                        //need to use index+1 for divisor here (see formula) , index+1 = history.Count 
                        float delta = prevObservationDict[key] - runningMeanDict[key]; 
                        featureDict[key + ".mean"] = runningMeanDict[key] += delta / historyQueue.Count;
                        runningVDict[key] += delta * (prevObservationDict[key] - runningMeanDict[key]);
                        if (index >= 2)
                        {
                            featureDict[key + ".var"] = runningVDict[key] / (historyQueue.Count - 1);
                        }
                    }
                }
                else   // index >= chunkSize
                {
                    QueueItem firstItem = historyQueue.First();
                    foreach (string key in prevObservationDict.Keys)
                    {
                        float obs = prevObservationDict[key];
                        float firstObs = firstItem.prevObservationDict[key]; 
                        float oldMean = runningMeanDict[key];

                        runningMeanDict[key] += (obs - firstObs) / chunkSize;
                        float mean = featureDict[key + ".mean"] = runningMeanDict[key];
                        runningVDict[key] += (obs - oldMean) * (obs - mean) - (firstObs - oldMean) * (firstObs - mean);
                        featureDict[key + ".var"] = runningVDict[key] / (chunkSize - 1);
                    }
                }
            }

            //if previous camera is different from the one before it, reset the duration start time
            //starts at 3 to skip the first change from prevCam -1
            if (index >= 3) 
            {
                if (historyQueue[^1].prevObservationDict["camera"] != historyQueue[^2].prevObservationDict["camera"])
                {
                    StateData prevData = JsonUtility.FromJson<StateData>(strings[dataIndex - 1]);
                    prevCamStartTime = prevData.timeElapsed;
                }
            }
            featureDict["camera.prevDuration"] = data.timeElapsed - prevCamStartTime;



            /***************************************   format list correctly and input into model  ************************************************/

            //add to pylist in order
            PyList features = new PyList();
            features.Append(new PyFloat(data.timeElapsed - firstData.timeElapsed)); //time since record
            features.Append(new PyFloat(featureDict["camera.prevDuration"]));
            features.Append(new PyFloat(featureDict["camera.var"]));
            features.Append(new PyFloat(data.avatarData.avatars[0].voiceVolume));
            features.Append(new PyFloat(data.avatarData.avatars[1].voiceVolume));

            for (int j = 0; j < transformVariables.Length; j++) {
               string entryKey = transformVariables[j];
               features.Append(new PyFloat(featureDict[entryKey + ".mean"]));
               features.Append(new PyFloat(featureDict[entryKey + ".var"])); 
            }

            PyList features2D = new PyList();
            features2D.Append(features);
            int cameraLabel = pythonInterfacer.ModelPrediction(features2D);


            //if output label is -1, prediction confidence is below threshold, don't switch camera
            if (cameraLabel == -1) 
            {
                cameraLabel = prevCameraLabel;
            }
            cameraManager.ChangeReplayCameraTo(cameraLabel);


            /***************************************   residual changes for the next loop  ************************************************/

            if (index >= chunkSize)
            {
                historyQueue.RemoveAt(0); //remove first in queue
            }

            prevCameraLabel = cameraLabel; // must be at end


            //print("prev: " + featureDict["camera.prevDuration"]);
            /*
            print("count: " + index);
            print("cam: " + cameraLabel);
            print("mean: " + featureDict["camera.mean"]);
            print("var: " + featureDict["camera.var"] + "\n");
            */


        }
        else
        {
            StopInvoking();
        }
    }


    /*------------------------------------------------------------- Helper Functions --------------------------------------------------------------------------*/



    //takes data of state and sets scene to match
    public void SetSceneWithData(StateData data)
    {
        cameraManager.SetReplayCameras(data);
        //need for loop for avatars
        for (int i = 0; i < replayAvatars.Count; i++)
        {
            AvatarData avatarData = data.avatarData.avatars[i];
            replayAvatars[i].SetAvatar(avatarData, i);
        }
        dataIndex++;
        cameraManager.slider.value = dataIndex;
    }


    //turns everything in the file into one string and returns
    public string ReadEntireStream(FileStream file)
    {
        reader = new StreamReader(file);
        string strings = reader.ReadToEnd();
        reader.Close();
        reader = null;
        return strings;
    }

    //sets the filestream to the specified file, returns true if path exists, false if failed
    public bool SetFile(string fName)
    {
        fileName = fName;
        string fullPath = path + fileName + fileType;

        if (File.Exists(fullPath))
        {
            fileStream?.Close();
            fileStream = File.Open(fullPath, FileMode.Open);
            return true;
        }
        else
        {
            Debug.Log("ERROR: file path does not exist. Cannot set file.");
            Debug.Log("FullPath: " + fullPath);
            return false;
        }
    }

    public FileStream GetNewFileStream(string fullPath)
    {
        FileStream returnFileStream = null;
        if (File.Exists(fullPath))
        {
            returnFileStream?.Close();
            returnFileStream = File.Open(fullPath, FileMode.Open);
        }
        return returnFileStream;
    }

    public string GetFileName()
    {
        return fileName;
    }
    private void SetWriter(string fname)
    {
        writer?.Close();
        writer = new StreamWriter(path + fname + fileType);
        writer.AutoFlush = true;
    }


    /*------------------------------------------------------------- For pausing and writing label UI ---------------------------------------------------------------------*/

    //in editor only
    IEnumerator ShowLoadDialogCoroutine()
    {
        // Show a load file dialog and wait for a response from user
        // Load file/folder: both, Allow multiple selection: true
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load Recording File", "Load");

        // Dialog is closed
        // Print whether the user has selected some files/folders or cancelled the operation (FileBrowser.Success)
        Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            string returnPath = FileBrowser.Result[0];
            Debug.Log("returned: " + returnPath);
            string fullPath = returnPath.Replace('\\', '/');
            string[] subStrings = fullPath.Split('/', '.');
            if (subStrings.Length > 2)
            {
                fileName = subStrings[subStrings.Length - 2];
                setFileButtonText.text = fileName;
                Debug.Log("fileName:" + fileName);
                path = String.Join("/", subStrings[0..(subStrings.Length - 2)]) + "/";
                Debug.Log("path:"+ path);
            }
        }
    }

    public void OpenFileBrowser()
    {
        #if !UNITY_ANDROID
            FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON recordings", ".json", ".JSON"));
            FileBrowser.SetDefaultFilter(".json");
            StartCoroutine(ShowLoadDialogCoroutine());
        #endif
    }
    public void TogglePauseReplay()
    {
        if (state == State.PausedReplay || state == State.PausedWriteLabel || state == State.PausedPredict || state == State.PausedRandom) //want to unpase
        {
            UnpauseReplay();
        }
        else
        {
            PauseReplay();
        }
    }

    public void PauseReplay()
    {
        if (state == State.WriteLabel)
        {
            ChangeState(State.PausedWriteLabel);
            CancelInvoke(nameof(ReplayAndWriteLabel));
        }
        else if(state == State.Replay )
        {
            ChangeState(State.PausedReplay);
            CancelInvoke(nameof(ReplayData));
        }
        else if (state == State.Predict)
        {
            ChangeState(State.PausedPredict);
            CancelInvoke(nameof(ReplayAndPredict));
        }
        else if(state == State.Random)
        {
            ChangeState(State.PausedRandom);
            CancelInvoke(nameof(ReplayRandom));
        }
    }

    public void UnpauseReplay()
    {
        if (state == State.PausedWriteLabel)
        {
            ChangeState(State.WriteLabel);
            EnvironmentData envData = JsonUtility.FromJson<EnvironmentData>(strings[0]);
            InvokeRepeating(nameof(ReplayAndWriteLabel), 0, envData.invokeInterval);
        }
        else if (state == State.PausedReplay)
        {
            ChangeState(State.Replay);
            EnvironmentData envData = JsonUtility.FromJson<EnvironmentData>(strings[0]);
            InvokeRepeating(nameof(ReplayData), 0, envData.invokeInterval);
        }
        else if (state == State.PausedPredict)
        {
            ChangeState(State.Predict);
            EnvironmentData envData = JsonUtility.FromJson<EnvironmentData>(strings[0]);
            InvokeRepeating(nameof(ReplayAndPredict), 0, envData.invokeInterval);
        }
        else if (state == State.PausedRandom)
        {
            ChangeState(State.Random);
            EnvironmentData envData = JsonUtility.FromJson<EnvironmentData>(strings[0]);
            InvokeRepeating(nameof(ReplayRandom), 0, envData.invokeInterval);
        }
    }

    public void SetDataIndex(int i)
    {
        if (i > 0 && i < dataIndex && i < strings.Length - 1)
        {
            dataIndex = i;
            StateData data = JsonUtility.FromJson<StateData>(strings[dataIndex]);
            SetSceneWithData(data); //update scene
        }
    }

    public int GetDataIndex()
    {
        return dataIndex;
    }


}







/*------------------------------------------------------------- Data Structures --------------------------------------------------------------------------*/

public class QueueItem{
    public Dictionary<string, float> prevObservationDict;
    public QueueItem(Dictionary<string, float> pod)
    {
        this.prevObservationDict = new Dictionary<string, float>(pod);
    }
}

[System.Serializable]
public class EnvironmentData
{
    public float invokeInterval;
    public int numCameras;
    public int numAvatars;

    public EnvironmentData(float ii, int numCam, int numAva)
    {
        invokeInterval = ii;
        numCameras = numCam;
        numAvatars = numAva;
    }
}


[System.Serializable]
public class StateDataList
{
    public List<StateData> stateData = new List<StateData>();
}

[System.Serializable]
public class StateData
{
    public float timeElapsed = 0;
    public CameraDataList cameraData;
    public AvatarDataList avatarData;

    public StateData(CameraManager cameraManager, List<SILvrAvatar> avatars)
    {
        timeElapsed = Time.time;
        avatarData = new AvatarDataList(avatars);
        cameraData = new CameraDataList(cameraManager.cameras, avatars);
    }
    
}


[System.Serializable]
public class CameraDataList
{
    public List<CameraData> cameras = new List<CameraData>();

    public CameraDataList(List<ShotCamera> cams, List<SILvrAvatar> sAvatars)
    {
        foreach (ShotCamera camera in cams)
        {
            cameras.Add(new CameraData(camera, sAvatars));
        }
    }
}

[System.Serializable]
public class CameraData
{
    public string name;
    public sTransform transform;
    public float fov;

    public float[] distanceTo;
    public bool[] headObstructed;
    public float[] angleTo;


    public CameraData(ShotCamera camera, List<SILvrAvatar> sAvatars)
    {
        name = camera.unityCamera.name;
        transform = camera.transform;
        fov = camera.unityCamera.fieldOfView;

        int numAvatars = sAvatars.Count;
        headObstructed = new bool[numAvatars];
        distanceTo = new float[numAvatars]; //don't technically need to record these, can calculate later
        angleTo = new float[numAvatars];

        for (int i = 0; i < numAvatars; i++)
        {
            SILvrAvatar avatar = sAvatars[i];
            Vector3 direction = avatar.headMarker.transform.position - camera.transform.position;
            float angle = Vector3.Angle(direction, camera.transform.forward);

            distanceTo[i] = direction.magnitude;
            angleTo[i] = angle;
            Debug.DrawLine(camera.transform.position, avatar.transform.position, Color.white, 1f);
            headObstructed[i] = Physics.Linecast(camera.transform.position, avatar.transform.position); //blocked

        }
    }
}

[System.Serializable]
public class AvatarDataList
{
    public List<AvatarData> avatars = new List<AvatarData>();

    public AvatarDataList(List<SILvrAvatar> sAvatars)
    {
        foreach (SILvrAvatar a in sAvatars)
        {
            avatars.Add(new AvatarData(a)); //avatars.AddRange(); 
        }
    }
}

[System.Serializable]
public class AvatarData{
    public sTransform headTransform;
    public sTransform rootTransform;
    public sTransform leftHandTransform;
    public sTransform rightHandTransform;
    public int poseType;
    public float voiceVolume;

    public AvatarData(SILvrAvatar avatar)
    {
        headTransform = avatar.playerHead;
        rootTransform = avatar.playerRoot;
        leftHandTransform = avatar.playerLeftHand;
        rightHandTransform = avatar.playerRightHand;

        RealtimeAvatarVoice voice = avatar.headMarker.gameObject.GetComponent<RealtimeAvatarVoice>();
        voiceVolume = (voice == null) ? 0 : voice.voiceVolume;

        poseType = (int)avatar.currentPose.poseType;
    }
}

[System.Serializable]
public class sList<T>
{
    public List<T> list;

    public sList()
    {
        list = new List<T>();
    }

   
}

//<summary> Serializable wrapper class for Vector3 and Vector4 </summary>
[System.Serializable]
public struct sVector
{
    public float x, y, z, w;

    public sVector(float x, float y, float z, float w = 0f)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public static implicit operator sVector(Quaternion a)
    {
        return new sVector(a.x, a.y, a.z, a.w);
    }

    public static implicit operator sVector(Vector3 a)
    {
        return new sVector(a.x, a.y, a.z);
    }

    public static implicit operator Quaternion(sVector a)
    {
        return new Quaternion(a.x, a.y, a.z, a.w);
    }

    public static implicit operator Vector3(sVector a)
    {
        return new Vector3(a.x, a.y, a.z);
    }

}

[System.Serializable]
public struct sTransform
{
    public sVector position;
    public sVector rotation;

    public sTransform(Transform transform)
    {
        position = transform.position;
        rotation = transform.rotation;
    }


    public static implicit operator sTransform(Transform transform)
    {
        return new sTransform(transform);
    }



}

