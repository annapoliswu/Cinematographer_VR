using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SILvr.Avatars
{
    [CreateAssetMenu(fileName = "PoseRecognitionData", menuName = "SILvr/Avatars/Pose Recognition Data", order = 3)]
    public class PoseRecognitionData : ScriptableObject
    {
        [Header("Pose Information")]

        // The pose associated with this recognition data
        public PoseType poseType;

        // The controller button to press to record the pose
        [HideInInspector] public InputActionReference recordPose;

        [Header("Pose Recognition Data")]

        // The actual recognition data itself
        public Quaternion headRotation;
        public Vector3 leftHandOffset;
        public Quaternion leftHandRotation;
        public Vector3 rightHandOffset;
        public Quaternion rightHandRotation;

        // Snapshot data
        [HideInInspector] public List<Quaternion> headRotationSnapshots = new List<Quaternion>();
        [HideInInspector] public List<Vector3> leftHandOffsetSnapshots = new List<Vector3>();
        [HideInInspector] public List<Quaternion> leftHandRotationSnapshots = new List<Quaternion>();
        [HideInInspector] public List<Vector3> rightHandOffsetSnapshots = new List<Vector3>();
        [HideInInspector] public List<Quaternion> rightHandRotationSnapshots = new List<Quaternion>();

        [Space()]

        // Lower values mean the player's pose must be a more precise match to trigger this pose
        public float threshold = .1f;

        [Header("Multi-User Settings")]

        // Number of players that can participate in this pose
        // TODO: When adding in multi user poses, make these two variables visible in the inspector
        [Min(1)]
        [HideInInspector] public int minPlayers = 1;
        [Min(1)]
        [HideInInspector] public int maxPlayers = 1;

        // Allowed distance between players in the pose
        [Min(0)]
        [HideInInspector] public float minDistance = .1f;
        [Min(0)]
        [HideInInspector] public float maxDistance = 1f;

        // Allowed difference in angle between players in the pose
        [Range(0, 360)]
        [HideInInspector] public float minAngleDifference = 150;
        [Range(0, 360)]
        [HideInInspector] public float maxAngleDifference = 210;

        // Returns true of the player's current pose is recognized as this pose
        public bool PoseRecognized(Transform head, Transform leftHand, Transform rightHand)
        {
            // Format current player data
            FormatPlayerData(head, leftHand, rightHand, out Quaternion currentHeadRotation, out Vector3 currentLeftHandOffset,
                out Quaternion currentLeftHandRotation, out Vector3 currentRightHandOffset, out Quaternion currentRightHandRotation);

            // Compare new and old values to see if the difference is within this pose's threshold
            return ((Quaternion.Angle(currentHeadRotation, headRotation) < threshold * 250)             // Head rotation recognized
                && (Quaternion.Angle(currentLeftHandRotation, leftHandRotation) < threshold * 250)      // Left hand rotation recognized
                && (Quaternion.Angle(currentRightHandRotation, rightHandRotation) < threshold * 250)    // Right hand rotation recognized
                && ((currentLeftHandOffset - leftHandOffset).sqrMagnitude < threshold * threshold)      // Left hand offset recognized
                && ((currentRightHandOffset - rightHandOffset).sqrMagnitude < threshold * threshold));  // Right hand offset recognized
        }

        // Formats player data as relative to the head
        private void FormatPlayerData(Transform head, Transform leftHand, Transform rightHand, out Quaternion headRotation, out Vector3 leftHandOffset, out Quaternion leftHandRotation, out Vector3 rightHandOffset, out Quaternion rightHandRotation)
        {           
            // Offset all rotations by the y rotation of the head, so poses can be enacted when the player is facing any direction
            Quaternion rotOffset = Quaternion.Euler(0, -head.eulerAngles.y, 0);
            // Save the head rotation
            headRotation = rotOffset * head.rotation;

            // Save left hand rotation and position
            leftHandOffset = rotOffset * (leftHand.position - head.position);
            leftHandRotation = rotOffset * leftHand.rotation;

            // Save right hand rotation and position
            rightHandOffset = rotOffset * (rightHand.position - head.position);
            rightHandRotation = rotOffset * rightHand.rotation;
        }

        // Records pose data
        public void RecordSnapshot()
        {
            // Attempt to find the player
            GameObject playerObj = GameObject.Find("SILvrAvatarManager/XR Origin");

            if (playerObj)
            {
                // Get the relevant player parts
                Transform head = playerObj.transform.Find("Camera Offset/Main Camera");
                Transform leftHand = playerObj.transform.Find("Camera Offset/LeftHand Controller");
                Transform rightHand = playerObj.transform.Find("Camera Offset/RightHand Controller");

                // Create local variables
                Quaternion headRotation;
                Vector3 leftHandOffset;
                Quaternion leftHandRotation;
                Vector3 rightHandOffset;
                Quaternion rightHandRotation;

                // Format data
                FormatPlayerData(head, leftHand, rightHand, out headRotation, out leftHandOffset, 
                    out leftHandRotation, out rightHandOffset, out rightHandRotation);

                // Add data to snapshots
                headRotationSnapshots.Add(headRotation);
                leftHandOffsetSnapshots.Add(leftHandOffset);
                leftHandRotationSnapshots.Add(leftHandRotation);
                rightHandOffsetSnapshots.Add(rightHandOffset);
                rightHandRotationSnapshots.Add(rightHandRotation);
            }
            else // Log an error if no player was found
            {
                Debug.LogError("No OVRPlayerController found!");
            }
        }

        public void FinalizeRecording()
        {
            // Set position data to the average of all snapshots
            headRotation = AverageQuaternions(headRotationSnapshots);
            leftHandOffset = AverageVectors(leftHandOffsetSnapshots);
            leftHandRotation = AverageQuaternions(leftHandRotationSnapshots);
            rightHandOffset = AverageVectors(rightHandOffsetSnapshots);
            rightHandRotation = AverageQuaternions(rightHandRotationSnapshots);

            // Clear snapshot data
            headRotationSnapshots.Clear();
            leftHandOffsetSnapshots.Clear();
            leftHandRotationSnapshots.Clear();
            rightHandOffsetSnapshots.Clear();
            rightHandRotationSnapshots.Clear();
        }

        // Returns the average of all the given vectors
        private Vector3 AverageVectors(List<Vector3> vectors)
        {
            Vector3 avg = Vector3.zero;
            foreach (Vector3 vector in vectors)
            {
                avg += vector;
            }
            return avg / vectors.Count;
        }

        // Returns the average of all the given quaternions
        private Quaternion AverageQuaternions(List<Quaternion> quaternions)
        {
            Quaternion result = Quaternion.identity;
            Vector4 cumulative = new Vector4();
            for (int i = 0; i < quaternions.Count; i++)
            {
                result = AverageQuaternion(ref cumulative, quaternions[i], quaternions[0], i + 1);
            }
            return result;
        }

        // The following functions are taken from: http://wiki.unity3d.com/index.php/Averaging_Quaternions_and_Vectors
        //Get an average (mean) from more then two quaternions (with two, slerp would be used).
        //Note: this only works if all the quaternions are relatively close together.
        //Usage: 
        //-Cumulative is an external Vector4 which holds all the added x y z and w components.
        //-newRotation is the next rotation to be added to the average pool
        //-firstRotation is the first quaternion of the array to be averaged
        //-addAmount holds the total amount of quaternions which are currently added
        //This function returns the current average quaternion
        private static Quaternion AverageQuaternion(ref Vector4 cumulative, Quaternion newRotation, Quaternion firstRotation, int addAmount)
        {
            float w = 0.0f;
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;

            //Before we add the new rotation to the average (mean), we have to check whether the quaternion has to be inverted. Because
            //q and -q are the same rotation, but cannot be averaged, we have to make sure they are all the same.
            if (!AreQuaternionsClose(newRotation, firstRotation))
            {

                newRotation = InverseSignQuaternion(newRotation);
            }

            //Average the values
            float addDet = 1f / (float)addAmount;
            cumulative.w += newRotation.w;
            w = cumulative.w * addDet;
            cumulative.x += newRotation.x;
            x = cumulative.x * addDet;
            cumulative.y += newRotation.y;
            y = cumulative.y * addDet;
            cumulative.z += newRotation.z;
            z = cumulative.z * addDet;

            //note: if speed is an issue, you can skip the normalization step
            return NormalizeQuaternion(x, y, z, w);
        }

        private static Quaternion NormalizeQuaternion(float x, float y, float z, float w)
        {
            float lengthD = 1.0f / (w * w + x * x + y * y + z * z);
            w *= lengthD;
            x *= lengthD;
            y *= lengthD;
            z *= lengthD;

            return new Quaternion(x, y, z, w);
        }

        //Changes the sign of the quaternion components. This is not the same as the inverse.
        private static Quaternion InverseSignQuaternion(Quaternion q)
        {
            return new Quaternion(-q.x, -q.y, -q.z, -q.w);
        }

        //Returns true if the two input quaternions are close to each other. This can
        //be used to check whether or not one of two quaternions which are supposed to
        //be very similar but has its component signs reversed (q has the same rotation as
        //-q)
        private static bool AreQuaternionsClose(Quaternion q1, Quaternion q2)
        {
            float dot = Quaternion.Dot(q1, q2);

            return (dot < 0.0f);
        }
    }

    // Custom editor for the Avatar
#if UNITY_EDITOR
    [CustomEditor(typeof(PoseRecognitionData))]
    public class PoseRecognitionEditor : Editor
    {
        // The script this editor is attached to
        private PoseRecognitionData script;


        private SerializedProperty minDistance;
        private SerializedProperty maxDistance;

        private SerializedProperty minAngleDifference;
        private SerializedProperty maxAngleDifference;

        private SerializedProperty recordPose;

        // Whether the button is toggled or not
        private bool recording;

        public void OnEnable()
        {
            // Get a reference to the script being modified
            script = (PoseRecognitionData)target;

            minDistance = serializedObject.FindProperty("minDistance");
            maxDistance = serializedObject.FindProperty("maxDistance");

            minAngleDifference = serializedObject.FindProperty("minAngleDifference");
            maxAngleDifference = serializedObject.FindProperty("maxAngleDifference");

            recordPose = serializedObject.FindProperty("recordPose");
       
            // Subscribe to update
            EditorApplication.update += Update;
        }
        void OnDisable()
        {
            EditorApplication.update -= Update;
        }

        public override void OnInspectorGUI()
        {
            // Update all serialized properties
            serializedObject.Update();

            // Make fields for the properties
            DrawDefaultInspector();

            // If this is a multi-player pose, reveal parameters for angle difference
            if (script.minPlayers > 1 || script.maxPlayers > 1)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(minDistance, new GUIContent("Min Distance", "The minimum distance between at least 2 players attempting to pose together"));
                EditorGUILayout.PropertyField(maxDistance, new GUIContent("Max Distance", "The maximum distance between at least 2 players attempting to pose together"));

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(minAngleDifference, new GUIContent("Min Angle Difference", "The minimum angle difference between at least 2 players attempting to pose together"));
                EditorGUILayout.PropertyField(maxAngleDifference, new GUIContent("Max Angle Difference", "The maximum angle difference between at least 2 players attempting to pose together"));
            }

            // Space
            EditorGUILayout.Space();

            // Recording data
            EditorGUILayout.LabelField(new GUIContent("Pose Recording Settings"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(recordPose, new GUIContent("Snapshot Action", "When recording is active, triggering this action will take a snapshot of the player's pose"));
            // Record button
            recording = EditorGUILayout.Toggle(new GUIContent("Recording Active", "When recording is active, a player can take snapshots of their pose. When recording is turned off, all snapshots will be averaged and the data saved"), recording);
            
            // Info about recording:
            EditorGUILayout.HelpBox("While recording is active, pressing the snapshot button will record a snapshot of the player's pose. When recording is disabled, all the snapshots will be averaged, and the result stored as pose data. This will only work using Oculus Link.", MessageType.Info);

            // Apply modified serialized properties
            serializedObject.ApplyModifiedProperties();
        }

        void Update()
        {
            if (recording && script.recordPose.action.WasPerformedThisFrame())
            {
                script.RecordSnapshot();
            }
            else if (!recording && script.headRotationSnapshots.Count > 0)
            {
                script.FinalizeRecording();
            }
        }
    }
#endif
}
