using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SILvr.Avatars
{
    [CreateAssetMenu(fileName = "Kit", menuName = "SILvr/Avatars/Kits/Kit", order = 1)]
    public class Kit : ScriptableObject
    {
        [Tooltip("The prefab this kit should instantiate")]
        public GameObject avatarPrefab;
        [Header("Pose Info")]
        [Tooltip("List of pose objects this avatar kit should use")]
        public List<Pose> poses;
        [Tooltip("Pose data this kit should recognize")]
        public List<PoseRecognitionData> recognizablePoses;
        [Header("Lipsync Info")]
        [Tooltip("Whether or not to use lipsync")]
        public bool lipsync;
        [Tooltip("The indices that correspond to the blendshapes for lipsync visemes")]
        [HideInInspector] public int[] visemeToBlendTargets = Enumerable.Range(0, OVRLipSync.VisemeCount).ToArray();

        // The list of parameters for this kit (the values for these parameters are managed by the SILvrAvatar instance using this kit)
        [SerializeField] public List<AvatarParameter> parameters;

        // Kits that inherit from this kit can also make a class that inherits from this data
        // Avatar data is useful for info that should persist between poses
        public class AvatarData
        {
            public Transform root;
            public Transform head; // Must always be the position of the avatar's head (where voice comes from)
            public Transform leftHand; // Must always be the position of the avatar's left hand (where things are held)
            public Transform rightHand; // Must always be the position of the avatar's right hand (where things are held)
            public SkinnedMeshRenderer mouth; // The mouth renderer for lipsync
            public bool firstPerson;
        }

        // Returns an instantiated version of the given avatar
        public virtual void InstantiateAvatar(SILvrAvatar avatar, out GameObject avatarObject, out AvatarData avatarData)
        {
            // Instantiate the avatar
            avatarObject = Instantiate(avatarPrefab);

            // Construct data object for this avatar
            AvatarData data = new AvatarData();
            data.root = avatarObject.transform;
            data.head = avatarObject.transform.Find("Head");
            data.leftHand = avatarObject.transform.Find("LeftHand");
            data.rightHand = avatarObject.transform.Find("RightHand");

            // Try to find the mouth
            data.mouth = null;

            avatarData = data;
        }

        // Returns list of parameters for this avatar kit
        public virtual void GenerateParameters()
        {
            parameters = new List<AvatarParameter>();
        }

        // Apply all parameters to the instantiated avatar
        public void ApplyParameters(SILvrAvatar avatar)
        {
            if (avatar.avatarObject) // Ensure there is an instantiated avatar
            {
                // Apply every parameter
                for (int i = 0; i < parameters.Count; i++)
                {
                    AvatarParameter parameter = parameters[i];
                    int value = (avatar.parameterValues.Length > i) ? avatar.parameterValues[i] : parameter.min;
                    parameter.Apply(avatar, value); 
                }
            }
        }

        // Changes avatar based on if it is being viewed in first or third person, only if not already in the given state
        public void SetFirstPersonIfNecessary(SILvrAvatar avatar, bool active)
        {
            // Only update if current state is not requested state
            if (avatar.avatarData.firstPerson != active)
            {
                // Remember current state
                avatar.avatarData.firstPerson = active;

                // Set first person
                SetFirstPerson(avatar, active);             
            }         
        }

        // Changes avatar based on if it is being viewed in first or third person
        public virtual void SetFirstPerson(SILvrAvatar avatar, bool active)
        {
            // Default behavior is just to hide head when in first person
            if (active)
            {
                avatar.avatarData.head.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                avatar.avatarData.head.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        // Return the mouth renderer for this avatar
        public virtual SkinnedMeshRenderer GetMouthRenderer(SILvrAvatar avatar)
        {
            return avatar.avatarData.mouth;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Kit))]
    public class KitEditor : Editor
    {
        public Kit kit;

        protected SerializedProperty avatarPrefab;
        protected SerializedProperty poses;
        protected SerializedProperty recognizablePoses;
        protected SerializedProperty lipsync;
        protected SerializedProperty visemeToBlendTargets;

        public virtual void OnEnable()
        {
            kit = (Kit)target;

            avatarPrefab = serializedObject.FindProperty("avatarPrefab");
            poses = serializedObject.FindProperty("poses");
            recognizablePoses = serializedObject.FindProperty("recognizablePoses");
            lipsync = serializedObject.FindProperty("lipsync");
            visemeToBlendTargets = serializedObject.FindProperty("visemeToBlendTargets");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw prefab stuff
            EditorGUILayout.PropertyField(avatarPrefab);
            if (GUILayout.Button("Generate customization parameters"))
            {
                kit.GenerateParameters();
            }

            // Draw pose stuff
            EditorGUILayout.PropertyField(poses);
            EditorGUILayout.PropertyField(recognizablePoses);

            // Draw lipsync stuff
            EditorGUILayout.PropertyField(lipsync);
            if (lipsync.boolValue)
            {
                LipsyncGUI();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Renders UI for lipsync data
        protected virtual void LipsyncGUI()
        {
            EditorGUILayout.PropertyField(visemeToBlendTargets);
        }
    }
#endif
}
