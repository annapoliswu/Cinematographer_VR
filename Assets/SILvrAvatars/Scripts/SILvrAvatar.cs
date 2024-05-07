using UnityEngine;
using System.Collections;
using Normal.Utility;
using Normal.Realtime;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SILvr.Avatars
{
    [ExecutionOrder(-94)] // Execute right after Normal
    public class SILvrAvatar : RealtimeComponent<SILvrAvatarModel>
    {
        public static SILvrAvatar localAvatar;

        [Tooltip("This avatar's kit")]
        public Kit kit;

        [Tooltip("Transform representing the head of the avatar, not the player")]
        public Transform headMarker;
        [Tooltip("Transform representing the left hand of the avatar, not the player")]
        public Transform leftHandMarker;
        [Tooltip("Transform representing the right hand of the avatar, not the player")]
        public Transform rightHandMarker;

        // List of parameter values that will be applied to the avatar when instantiated
        public int[] parameterValues;

        // Reference to the instantiated avatar and avatar data from the kit
        public GameObject avatarObject;
        public Kit.AvatarData avatarData;

        private Dictionary<PoseType, Pose> poseMap; // Mapped pose types to poses from the kit
        public Pose currentPose; // The avatar's current pose

        private XROrigin origin; // Players XR origin
        private TeleportationProvider teleportProvider; // The local player's teleport provider

        private Coroutine coroutine; // currently running coroutine

        // Reference to the realtime avatar, and it's variables
        private RealtimeAvatar realtimeAvatar;
        public RealtimeAvatar.LocalPlayer localPlayer { get { return GetLocalPlayer();  } }
        public Transform playerRoot { get { return GetPlayerRoot(); } }
        public Transform playerHead { get { return GetPlayerHead(); } }
        public Transform playerLeftHand { get { return GetPlayerLeftHand(); } }
        public Transform playerRightHand { get { return GetPlayerRightHand(); } }

        protected virtual RealtimeAvatar.LocalPlayer GetLocalPlayer() { return realtimeAvatar.localPlayer; }
        protected virtual Transform GetPlayerRoot() { return realtimeAvatar.transform; }
        protected virtual Transform GetPlayerHead() { return realtimeAvatar.head; }
        protected virtual Transform GetPlayerLeftHand() { return realtimeAvatar.leftHand; }
        protected virtual Transform GetPlayerRightHand() { return realtimeAvatar.rightHand; }

        // Reference to the avatar manager
        private SILvrAvatarManager avatarManager;

        // Some poses may wish to have the camera view the avatar in third person. If so, this variable tracks the third person offset;
        private Vector3 cameraOffset = Vector3.zero;

        // Distance player head must be from avatar head to trigger third person view;
        private float thirdPersonDistance = .2f;
        // Leeway in distance between player pos and required pos for pose switching
        private float epsilon = .01f;

        private void Start()
        {
            // Add this avatar to the registry
            avatarManager = SILvrAvatarManager.Instance;
            avatarManager.AddAvatar(this, model.ownerIDInHierarchy);

            // Get components
            realtimeAvatar = GetComponent<RealtimeAvatar>();

            // Instantiate avatar and save a reference to it and it's data
            kit.InstantiateAvatar(this, out avatarObject, out avatarData);

            // Apply parameters to the instantiated avatar
            RandomizeParameters();
            kit.ApplyParameters(this);

            // Set up pose map and initialize poses
            InitializePoses();

            // Do local player stuff
            if (localPlayer != null)
            {
                // Remember the local ID
                avatarManager.localID = model.ownerIDInHierarchy;

                // Remember that this is the local avatar
                localAvatar = this;

                // Get local components
                origin = localPlayer.root.GetComponent<XROrigin>();
                teleportProvider = localPlayer.root.GetComponent<TeleportationProvider>();

                // Attach teleport callback
                teleportProvider.endLocomotion += OnTeleport;
            }
            else
            {
                // Set up lipsync, if enabled for this kit
                if (kit.lipsync)
                {
                    StartCoroutine(InitializeLipsync());
                }
            }

            // Perform initial data model setup
            OnPoseChangeEvent();

            // Set first person or not, depending on if this is the local avatar
            kit.SetFirstPersonIfNecessary(this, localPlayer != null);
        }

        void FixedUpdate()
        {
            UpdatePlayerTransforms();

            // Check if pose should change to a recognizable pose
            // Occurs on fixed update so it doesn't check every frame
            CheckRecognizablePoses();

            UpdateMarkerTransforms();
        }

        void Update()
        {
            UpdatePlayerTransforms();

            // Update pose
            currentPose?.Stay(this);

            UpdateMarkerTransforms();
        }

        void LateUpdate()
        {
            UpdatePlayerTransforms();

            // Update pose
            currentPose?.LateStay(this);

            UpdateMarkerTransforms();
        }

        private void OnDestroy()
        {
            // Remove this avatar from the registry
            if (avatarManager)
            {
                SILvrAvatarManager.Instance.RemoveAvatar(this);
            }

            // When disconnected, also remove the avatar
            Destroy(avatarObject);
        }

        private void OnApplicationQuit()
        {
            // Disconnect from the room if the local user
            if (localPlayer != null)
            {
                realtime.Disconnect();
            }
        }

        // Sets up the pose map, and initializes poses
        private void InitializePoses()
        {
            // Construct the pose dictionary
            poseMap = new Dictionary<PoseType, Pose>();
            foreach (Pose pose in kit.poses)
            {
                // Copy the pose, so local changes don't affect all avatars using the pose
                Pose poseCopy = Instantiate(pose);

                // Initialize the pose, and add it to the map
                poseCopy.Init(this);
                poseMap.Add(pose.poseType, poseCopy);
            }

            // Print error if avatar does not have an idle pose
            if (!poseMap[PoseType.Idle])
            {
                Debug.LogError("Avatar kit " + kit.name + " must have an Idle pose specified");
            }
        }

        // Coroutine to allow lipsync context to be created after audio output is created
        // This is because both scripts use OnAudioFilterRead, which is called in the same order as the scripts on the gameobject
        // By instantiating the lipsync context second, we ensure it's filter is called second
        private IEnumerator InitializeLipsync()
        {
            // Wait for audio oputput script to be instantiated
            while (!headMarker.GetComponent<AudioOutput>())
            {
                yield return null;
            }

            // Instantiate lipsync context after audio output is created
            OVRLipSyncContext lipSyncContext = headMarker.gameObject.AddComponent<OVRLipSyncContext>();
            lipSyncContext.audioLoopback = true;

            // Instantiate lipsync target
            OVRLipSyncContextMorphTarget lipSyncTarget = headMarker.gameObject.AddComponent<OVRLipSyncContextMorphTarget>();
            lipSyncTarget.skinnedMeshRenderer = kit.GetMouthRenderer(this);
            lipSyncTarget.visemeToBlendTargets = kit.visemeToBlendTargets;
            lipSyncTarget.laughterBlendTarget = -1;
        }

        // Make marker transforms match avatar transforms
        private void UpdateMarkerTransforms()
        {
            headMarker.Match(avatarData.head);
            leftHandMarker.Match(avatarData.leftHand);
            rightHandMarker.Match(avatarData.rightHand);
        }

        // Separate from posing, this function makes necessary updates to player transforms
        private void UpdatePlayerTransforms()
        {
            // Ensure we are working with the local player
            if (localPlayer == null)
                return;

            // Check if the player's head position exceeds maximum avatar height
            float delta = localPlayer.head.position.y - localPlayer.root.position.y;
            if (delta > currentPose.GetMaxHeight(this))
            {
                // If player exceeds max height, lower player to max height
                Vector3 adjustment = new Vector3(0, currentPose.GetMaxHeight(this) - delta, 0);
                playerHead.position += adjustment;
                playerLeftHand.position += adjustment;
                playerRightHand.position += adjustment;
            }

            // Apply camera offset if necessary
            if (cameraOffset.sqrMagnitude > 0)
            {
                // Counteract the offset applied to the localPlayer
                // This way, the player transforms stay in the same position, and only the XROrigin moves 
                playerHead.position -= cameraOffset;
                playerLeftHand.position -= cameraOffset;
                playerRightHand.position -= cameraOffset;
            }

            // Make the avatar third person if player is sufficiently distanced from the avatar
            kit.SetFirstPersonIfNecessary(this, (localPlayer.head.position - avatarData.head.position).sqrMagnitude <= thirdPersonDistance * thirdPersonDistance);
        }

        // Set the current camera offset;
        public void SetCameraOffset(Vector3 newOffset)
        {
            if (localPlayer != null)
            {
                localPlayer.root.position -= cameraOffset;
                localPlayer.root.position += newOffset;
                cameraOffset = newOffset;
            }
        }

        // Add to the current camera offset;
        public void AddCameraOffset(Vector3 additionalOffset)
        {
            if (localPlayer != null)
            {
                cameraOffset += additionalOffset;
                localPlayer.root.position += additionalOffset;
            }
        }

        // Runs through recognizable poses for this avatar's kit, and if any are recognized, switch to that pose
        private void CheckRecognizablePoses()
        {
            if (currentPose && currentPose.poseType == PoseType.Idle)
            {
                foreach (PoseRecognitionData data in kit.recognizablePoses)
                {
                    if (data.PoseRecognized(playerHead, playerLeftHand, playerRightHand))
                    {
                        TriggerPoseChangeEvent(data.poseType);
                        break;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (localPlayer != null)
            {
                // Remove teleport callback
                teleportProvider.endLocomotion -= OnTeleport;
            }
        }

        // Called locally when user teleports
        private void OnTeleport(LocomotionSystem locomotionSystem)
        {
            SetPlayerHeight();
        }

        // Sets player height to match avatar height
        public void SetPlayerHeight()
        {
            if (localPlayer != null)
            {
                float offset = currentPose.GetDefaultHeight(this) - (localPlayer.head.position.y - localPlayer.root.position.y);
                // Adjust camera floor offset object to account for the difference
                origin.CameraFloorOffsetObject.transform.position += new Vector3(0, offset, 0);
            }
        }

        // Reset and randomize avatar's parameter values
        public void RandomizeParameters()
        {
            parameterValues = new int[kit.parameters.Count];
            for (int i = 0; i < kit.parameters.Count; i++)
            {
                AvatarParameter parameter = kit.parameters[i];
                parameterValues[i] = UnityEngine.Random.Range(parameter.min, parameter.max + 1);
            }
        }

        // Handle changes in model
        protected override void OnRealtimeModelReplaced(SILvrAvatarModel previousModel, SILvrAvatarModel currentModel)
        {
            if (previousModel != null)
            {
                // Unregister from events
                previousModel.onPoseChangeEvent -= OnPoseChangeEvent;
            }

            if (currentModel != null)
            {
                // If this is a model that has no data set on it, populate it with default values
                if (currentModel.isFreshModel)
                {
                    currentModel.poseType = PoseType.Idle;
                    currentModel.requirePosition = false;
                    currentModel.position = Vector3.zero;
                }

                // Register to events
                currentModel.onPoseChangeEvent += OnPoseChangeEvent;
            }
        }

        // Trigger pose change event with unspecified position
        public void TriggerPoseChangeEvent(PoseType poseType)
        {
            model.TriggerPoseChangeEvent(poseType);

            // Cheat and set the pose early for the local player
            SetPose(poseType);
        }

        // Trigger pose change event with specified position
        public void TriggerPoseChangeEvent(PoseType poseType, Vector3 position)
        {
            model.TriggerPoseChangeEvent(poseType, position);

            // Cheat and set the pose early for the local player, if in position
            if (PlayerIsInPosition(position))
            {
                SetPose(poseType);
            }
        }

        // Sets the avatar's pose. Not networked
        private void SetPose(PoseType poseType)
        {
            // Only switch pose if we have one for this type, and it isn't the current pose
            if (poseMap != null && poseMap.ContainsKey(poseType))
            {
                // Set random seed to match across clients
                UnityEngine.Random.InitState((int)realtime.room.time);

                currentPose?.Exit(this);
                currentPose = poseMap[poseType];
                currentPose.Enter(this);

                // Set height to match new pose
                SetPlayerHeight();

                // Set whether teleporting is allowed while in this pose
                if (teleportProvider)
                {
                    teleportProvider.enabled = currentPose.canTeleport;
                }
            }
        }

        // Called when the networked model is changed and all changes have been deserialized
        private void OnPoseChangeEvent()
        {
            // Make sure avatar has been instantiated
            if (avatarObject)
            {
                // Reset previous coroutine
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }

                // If player does not have to be in a particular position to be in this pose, or if they are already in this position, set the avatar pose
                // Otherwise, wait until they are in position
                if (!model.requirePosition || PlayerIsInPosition(model.position))
                {
                    SetPose(model.poseType);
                }
                else
                {
                    // Start new coroutine
                    coroutine = StartCoroutine(SetPoseWhenInPosition(model.poseType, model.position));
                }
            }
                 
        }

        // Waits for player to enter given position before activating the given pose
        private IEnumerator SetPoseWhenInPosition(PoseType poseType, Vector3 position)
        {
            // Wait for player to get into position
            while (!PlayerIsInPosition(position))
            {
                yield return null;
            }

            // Set the pose
            SetPose(poseType);
        }

        // Helper method that returns true if player root is at the given position;
        private bool PlayerIsInPosition(Vector3 position)
        {
            return (playerRoot.position - position).sqrMagnitude <= epsilon * epsilon;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SILvrAvatar))]
    public class SILvrAvatarEditor : Editor
    {
        public SILvrAvatar script;

        // Properties
        SerializedProperty kit;
        SerializedProperty headMarker;
        SerializedProperty leftHandMarker;
        SerializedProperty rightHandMarker;
        SerializedProperty parameterValues;

        // Whether to show the parameters or not (set by a toggle)
        private bool showParameters = false;

        public void OnEnable()
        {
            script = (SILvrAvatar)target;

            // Get properties
            kit = serializedObject.FindProperty("kit");
            headMarker = serializedObject.FindProperty("headMarker");
            leftHandMarker = serializedObject.FindProperty("leftHandMarker");
            rightHandMarker = serializedObject.FindProperty("rightHandMarker");
            parameterValues = serializedObject.FindProperty("parameterValues");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Field for the kit
            EditorGUILayout.PropertyField(kit);

            EditorGUILayout.Space();

            // Field for markers
            EditorGUILayout.PropertyField(headMarker);
            EditorGUILayout.PropertyField(leftHandMarker);
            EditorGUILayout.PropertyField(rightHandMarker);

            // Serialize parameter changes
            Undo.RecordObject(script, "Changed parameters");

            // Parameter stuff
            if (kit.objectReferenceValue)
            {
                EditorGUILayout.Space();

                // Create a dropdown for parameters if the kit was provided
                showParameters = EditorGUILayout.Foldout(showParameters, "Parameters");
    
                if (showParameters)
                {
                    EditorGUI.indentLevel++;

                    // Get kit info as a serialized object
                    SerializedObject kitObject = new SerializedObject(kit.objectReferenceValue);
                    // Get a reference to the parameters
                    SerializedProperty parameters = kitObject.FindProperty("parameters");

                    // Get proper number of parameters
                    int parameterCount = parameters.arraySize;

                    // Make sure parameter list is the correct size
                    if (parameterValues.arraySize != parameterCount)
                    {
                        parameterValues.arraySize = parameterCount;
                    }

                    // Create the parameter fields
                    for (int i = 0; i < parameterCount; i++)
                    {
                        // Get parameter as a scriptable object, and get info on it
                        SerializedObject parameterObject = new SerializedObject(parameters.GetArrayElementAtIndex(i).objectReferenceValue);
                        int min = parameterObject.FindProperty("min").intValue;
                        int max = parameterObject.FindProperty("max").intValue;
                        string name = parameterObject.FindProperty("m_Name").stringValue;
                        // Get property to modify
                        SerializedProperty value = parameterValues.GetArrayElementAtIndex(i);
                        // Create slider
                        EditorGUILayout.IntSlider(value, min, max, new GUIContent(name));
                    }

                    EditorGUI.indentLevel--;
                }
            }
            // Reset parameters if no kit
            else if (parameterValues.arraySize > 0)
            {
                parameterValues.arraySize = 0;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
