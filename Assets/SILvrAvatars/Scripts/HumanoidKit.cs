using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DitzelGames.FastIK;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SILvr.Avatars
{
    [CreateAssetMenu(fileName = "Humanoid Kit", menuName = "SILvr/Avatars/Kits/Humanoid Kit", order = 2)]
    public class HumanoidKit : Kit
    {
        [Header("Humanoid Info")]
        [Tooltip("Name of game object with all customizable parts as a child")]
        public string partParent;
        [Tooltip("Prefix before all bones in the skeleton. Ex. for the bone 'Character1_Hips', 'Character1' is the prefix")]
        public string skeletonPrefix;
        [Tooltip("Name of the mouth part in the part hierarchy, ex. 'mouth'")]
        [SerializeField] private string mouthPart = "mouth";
        [Tooltip("Offset from the head bone that the player's head should be placed at. In other words, the offset between the head bone and eyes")]
        public Vector3 headOffset;
        [Tooltip("Whether this avatar model is a smoothskin or not")]
        public bool smoothSkin;

        // Smooth skin body group masks (and corresponding meshes)
        [SerializeField] private List<Mesh> meshes;
        [SerializeField] private List<BodyGroup> bodyGroups;
        [SerializeField] private List<Texture2D> bodyGroupMasks;

        // Mouth parameter
        [SerializeField] private int mouthParameterIndex;
        [SerializeField] private PartParameter mouthParameter;

        public enum Limb { Head, LeftHand, RightHand, LeftFoot, RightFoot }
        public enum BodyGroup { Head, Torso, Arms, Legs }

        // List of all bone paths (except hands) in the humanoid skeleton
        private string[] bonePaths = new string[]
        {
            "Reference/Hips",
            "Reference/Hips/Spine",
            "Reference/Hips/Spine/Spine1",
            "Reference/Hips/Spine/Spine1/LeftShoulder",
            "Reference/Hips/Spine/Spine1/RightShoulder",

            "Reference/Hips/Spine/Spine1/Neck",
            "Reference/Hips/Spine/Spine1/Neck/Head",

            "Reference/Hips/Spine/Spine1/LeftShoulder/LeftArm",
            "Reference/Hips/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm",
            "Reference/Hips/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand",

            "Reference/Hips/Spine/Spine1/RightShoulder/RightArm",
            "Reference/Hips/Spine/Spine1/RightShoulder/RightArm/RightForeArm",
            "Reference/Hips/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand",

            "Reference/Hips/LeftUpLeg",
            "Reference/Hips/LeftUpLeg/LeftLeg",
            "Reference/Hips/LeftUpLeg/LeftLeg/LeftFoot",
            "Reference/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase",

            "Reference/Hips/RightUpLeg",
            "Reference/Hips/RightUpLeg/RightLeg",
            "Reference/Hips/RightUpLeg/RightLeg/RightFoot",
            "Reference/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase",
        };

        // Indices in bone paths for important bones
        private int headIndex = 6;
        private int leftHandIndex = 9;
        private int rightHandIndex = 12;
        private int leftFootIndex = 15;
        private int rightFootIndex = 19;

        public class HumanoidAvatarData : AvatarData
        {
            public Transform hips;
            public Transform leftFoot;
            public Transform rightFoot;

            public FastIKFabric headIK;
            public FastIKFabric leftHandIK;
            public FastIKFabric rightHandIK;
            public FastIKFabric leftFootIK;
            public FastIKFabric rightFootIK;

            public Transform headTarget;
            public Transform leftFootTarget;
            public Transform rightFootTarget;

            public Transform[] bones; // List of bones

            public List<MaterialPropertyBlock> blocks; // Material property blocks for the avatar
            public SkinnedMeshRenderer[] renderers; // Renderers for this avatar

            public Animator animator; // The animator for this avatar
        }

        // Returns list of parameters for this avatar kit
        public override void GenerateParameters()
        {
            parameters = new List<AvatarParameter>();

            // Find parent of customizable parts
            Transform parent = avatarPrefab.transform.Find(partParent);

            // Get max number of options for each part, and the first transform in the part hierarchy for that option list
            Transform[] options = new Transform[parent.childCount];
            string[] paths = new string[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
            {
                options[i] = parent.GetChild(i);
                paths[i] = partParent + "/" + options[i].name;
            }

            // Recursively get parameters
            GetParametersRecursive(parameters, options, paths);
        }

        // Helper to recursively get parameters
        private void GetParametersRecursive(List<AvatarParameter> parameters, Transform[] groups, string[] groupPaths)
        {
            // Check if this part is a mouth
            bool isMouth = false;
            if (lipsync)
            {
                foreach (Transform group in groups)
                {
                    // If this renderer shares a name with the mouth part, this up-and-coming parameter must be the mouth parameter
                    if (group.name.Equals(mouthPart))
                    {
                        isMouth = true;
                    }
                }
            }

            // Construct list of valid paths for the parameter (not all groups are always valid)
            List<string> paths = new List<string>();
            for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                // Path needs a mesh renderer to be valid
                SkinnedMeshRenderer renderer = groups[groupIndex].GetComponent<SkinnedMeshRenderer>();
                if (renderer)
                {
                    // Add path to the parameter
                    paths.Add(groupPaths[groupIndex]);

                    // Create blendshape parameters for each blendshape on this renderer's mesh
                    Mesh mesh = renderer.sharedMesh;
                    for (int blendshapeIndex = 0; blendshapeIndex < mesh.blendShapeCount; blendshapeIndex++)
                    {
                        // Make sure we don't create a parameter for lipsync visemes
                        if (!isMouth || !visemeToBlendTargets.Contains(blendshapeIndex))
                        {
                            string name = groups[groupIndex].name + " " + mesh.GetBlendShapeName(blendshapeIndex);
                            parameters.Add(BlendshapeParameter.Create(this, name, groupPaths[groupIndex], blendshapeIndex));
                        }                  
                    }
                }
            }

            // Create the parameter from valid paths
            if (paths.Count > 1)
            {
                PartParameter parameter = PartParameter.Create(this, groups[0].name, paths.ToArray());

                // If this is the mouth parameter, remember it
                if (isMouth)
                {
                    mouthParameterIndex = parameters.Count;
                    mouthParameter = parameter;
                }

                parameters.Add(parameter);
            }

            // Iterate through children of the template
            for (int childIndex = 0; childIndex < groups[0].childCount; childIndex++)
            {
                // Construct a new list of options for this child
                Transform[] newOptionGroups = new Transform[groups.Length];
                string[] newGroupPaths = new string[groupPaths.Length];

                // Iterate through previous options, getting their equivalent of this child
                for (int optionIndex = 0; optionIndex < groups.Length; optionIndex++)
                {
                    // Make sure index is valid, and log error if not
                    if (childIndex < groups[optionIndex].childCount)
                    {
                        newOptionGroups[optionIndex] = groups[optionIndex].GetChild(childIndex);
                        newGroupPaths[optionIndex] = groupPaths[optionIndex] + "/" + newOptionGroups[optionIndex].name;
                    }
                    else
                    {
                        Debug.LogError("All part option hierachies must match!");
                    }
                }

                // Recursively get parameters for that child
                GetParametersRecursive(parameters, newOptionGroups, newGroupPaths);
            }
        }

        // Returns an instantiated version of the given avatar
        public override void InstantiateAvatar(SILvrAvatar avatar, out GameObject avatarObject, out AvatarData avatarData)
        {
            // Instantiate the avatar
            avatarObject = Instantiate(avatarPrefab);

            // Construct data object for this avatar
            HumanoidAvatarData data = new HumanoidAvatarData();

            // Get a reference to the bones
            data.root = avatarObject.transform;
            data.hips = GetBoneFromPath(data.root, bonePaths[0]);
            data.head = GetBoneFromPath(data.root, bonePaths[headIndex]);
            data.leftHand = GetBoneFromPath(data.root, bonePaths[leftHandIndex]);
            data.rightHand = GetBoneFromPath(data.root, bonePaths[rightHandIndex]);
            data.leftFoot = GetBoneFromPath(data.root, bonePaths[leftFootIndex]);
            data.rightFoot = GetBoneFromPath(data.root, bonePaths[rightFootIndex]);

            // Create feet targets
            data.headTarget = CreateHeadTarget(avatar.playerHead);
            data.leftFootTarget = CreateFootTarget(data, Limb.LeftFoot);
            data.rightFootTarget = CreateFootTarget(data, Limb.RightFoot);

            // Add IK to limbs
            data.headIK = AddIK(data, Limb.Head, data.headTarget, 3);
            data.leftHandIK = AddIK(data, Limb.LeftHand, avatar.playerLeftHand, 2);
            data.rightHandIK = AddIK(data, Limb.RightHand, avatar.playerRightHand, 2);
            data.leftFootIK = AddIK(data, Limb.LeftFoot, data.leftFootTarget, 2);
            data.rightFootIK = AddIK(data, Limb.RightFoot, data.rightFootTarget, 2);

            // Get bones
            data.bones = GetBones(data.root);

            // Get reference to the animator
            data.animator = avatarObject.GetComponent<Animator>();
            data.animator.enabled = false;

            // Create property blocks
            CreatePropertyBlocks(avatar, out data.blocks, out data.renderers);

            avatarData = data;
        }

        // Creates property blocks for this avatar
        protected void CreatePropertyBlocks(SILvrAvatar avatar, out List<MaterialPropertyBlock> blocks, out SkinnedMeshRenderer[] renderers)
        {
            // Instantiate data
            blocks = new List<MaterialPropertyBlock>();
            renderers = avatar.avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            // Set blocks
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                // Create block for this renderer
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                if (smoothSkin) // Smooth skin specific parameters
                {
                    // Set body group mask
                    int index = meshes.IndexOf(renderer.sharedMesh);
                    if (index >= 0)
                    {
                        block.SetTexture("_BodyGroupMask", bodyGroupMasks[index]);
                    }
                }

                // Set and save property block
                renderer.SetPropertyBlock(block);
                blocks.Add(block);
            }
        }

        // Creates head target gameobject
        protected Transform CreateHeadTarget(Transform playerHead)
        {
            GameObject target = new GameObject("Head Target");
            target.transform.parent = playerHead;
            target.transform.localPosition = -headOffset;
            target.transform.localRotation = Quaternion.identity;

            return target.transform;
        }

        // Creates foot target gameobject
        protected Transform CreateFootTarget(HumanoidAvatarData avatarData, Limb limb)
        {
            GameObject target = new GameObject(limb.ToString() + " Target");
            target.transform.parent = avatarData.root;
            target.transform.localPosition = Vector3.zero;
            target.transform.localRotation = Quaternion.identity;

            FootPlacer.CreateComponent(target, GetLimbTransform(avatarData, limb), 2, limb == Limb.LeftFoot);

            return target.transform;
        }

        // Adds IK to the given transform
        protected FastIKFabric AddIK(HumanoidAvatarData avatarData, Limb limb, Transform target, int chainLength)
        {
            // Get limb transform
            Transform transform = GetLimbTransform(avatarData, limb);

            // Disable game object to prevent the IK script's Awake method from being called before we set the target and other variables
            transform.gameObject.SetActive(false);

            // Attach and format IK script
            FastIKFabric ik = transform.gameObject.AddComponent<FastIKFabric>();
            ik.Target = target;
            ik.ChainLength = chainLength;

            // Add IK pole, if necessary
            if (limb != Limb.Head)
            {
                GameObject pole = new GameObject(limb.ToString() + " Pole");
                pole.transform.parent = avatarData.root;
                ik.Pole = pole.transform;

                // Pole controller depends on limb type
                if (limb == Limb.LeftHand || limb == Limb.RightHand)
                {
                    HandIKPoleController.CreateComponent(pole, transform, chainLength, (limb == Limb.LeftHand) ? Hand.Left : Hand.Right);
                }
                else
                {
                    FootIKPoleController.CreateComponent(pole, transform, chainLength);
                }
            }

            // Reenable the game object
            transform.gameObject.SetActive(true);

            return ik;
        }

        // Returns the transform from the data that corresponds to the given limb
        protected Transform GetLimbTransform(HumanoidAvatarData avatarData, Limb limb)
        {
            switch (limb)
            {
                case (Limb.Head):
                    return avatarData.head;
                case (Limb.LeftHand):
                    return avatarData.leftHand;
                case (Limb.RightHand):
                    return avatarData.rightHand;
                case (Limb.LeftFoot):
                    return avatarData.leftFoot;
                case (Limb.RightFoot):
                    return avatarData.rightFoot;
            }
            return null;
        }

        // Returns bone along the given path from the root
        protected Transform GetBoneFromPath(Transform root, string path)
        {
            Transform bone = root;
            foreach (string name in path.Split("/"))
            {
                bone = bone.Find(skeletonPrefix + "_" + name);
            }
            return bone;
        }

        // Get a list of bones
        protected Transform[] GetBones(Transform root)
        {
            // Create array of bones
            Transform[] bones = new Transform[bonePaths.Length];

            // Iterate through list of bone paths, creating a bone for each path
            for (int i = 0; i < bonePaths.Length; i++)
            {
                bones[i] = GetBoneFromPath(root, bonePaths[i]);
            }

            return bones;
        }

        // Return active mouth
        public override SkinnedMeshRenderer GetMouthRenderer(SILvrAvatar avatar)
        {
            // Mouth data (find active mouth, and set the data.mouth to be the renderer on the active mouth)
            int selectedMouth = avatar.parameterValues[mouthParameterIndex];
            return avatar.avatarObject.transform.Find(mouthParameter.paths[selectedMouth])?.GetComponent<SkinnedMeshRenderer>();
        }

        // Changes avatar based on if it is being viewed in first or third person
        public override void SetFirstPerson(SILvrAvatar avatar, bool active)
        {
            // Cast parameters to proper subclass
            HumanoidAvatarData data = (HumanoidAvatarData)avatar.avatarData;
            HumanoidPose currentPose = (HumanoidPose)avatar.currentPose;

            // Set visibility for each body part for each renderer
            if (smoothSkin)
            {
                // Iterate through renderers
                for (int i = 0; i < data.renderers.Length; i++)
                {

                    // Set visibility to first person visibility if in first person, or 1 if in third person
                    MaterialPropertyBlock block = data.blocks[i];
                    block.SetFloat("_HeadVisibility",  (!active || currentPose.headVisibleInFirstPerson) ? 1 : 0);
                    block.SetFloat("_TorsoVisibility", (!active || currentPose.torsoVisibleInFirstPerson) ? 1 : 0);
                    block.SetFloat("_ArmsVisibility",  (!active || currentPose.armsVisibleInFirstPerson) ? 1 : 0);
                    block.SetFloat("_LegsVisibility",  (!active || currentPose.legsVisibleInFirstPerson) ? 1 : 0);
                    data.renderers[i].SetPropertyBlock(block);
                }
            }
            else
            {
                // Iterate through renderers
                foreach (SkinnedMeshRenderer renderer in data.renderers)
                {
                    // Get index of body group for the current renderer's mesh
                    int index = meshes.IndexOf(renderer.sharedMesh);
                    if (index >= 0)
                    {
                        // Get whether body group should be visible in first person
                        bool visible = false;
                        switch (bodyGroups[index]) {
                            case BodyGroup.Head:
                                visible = currentPose.headVisibleInFirstPerson;
                                break;
                            case BodyGroup.Torso:
                                visible = currentPose.torsoVisibleInFirstPerson;
                                break;
                            case BodyGroup.Arms:
                                visible = currentPose.armsVisibleInFirstPerson;
                                break;
                            case BodyGroup.Legs:
                                visible = currentPose.legsVisibleInFirstPerson;
                                break;
                        }

                        // If in third person, or body group is visible in first person, make it visible
                        if (!active || visible)
                        {
                            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        }
                        else // Otherwise, hide it
                        {
                            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                        }
                    }
                }
            }
        }

        public void GenerateBodyGroupData()
        {
            if (smoothSkin)
            {
                // Generate body group masks and save them in the kit
                BodyGroupClassifier.GenerateBodyGroupMasks(avatarPrefab, out meshes, out bodyGroupMasks);
                bodyGroups = null;

            }
            else
            {
                // Generate body group classifications and save them in the kit
                BodyGroupClassifier.GenerateBodyGroups(avatarPrefab, out meshes, out bodyGroups);
                bodyGroupMasks = null;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(HumanoidKit))]
    public class HumanoidKitEditor : KitEditor
    {
        protected SerializedProperty partParent;
        protected SerializedProperty skeletonPrefix;
        protected SerializedProperty headOffset;
        protected SerializedProperty smoothSkin;

        protected SerializedProperty mouthPart;

        public override void OnEnable()
        {
            base.OnEnable();

            partParent = serializedObject.FindProperty("partParent");
            skeletonPrefix = serializedObject.FindProperty("skeletonPrefix");
            headOffset = serializedObject.FindProperty("headOffset");
            smoothSkin = serializedObject.FindProperty("smoothSkin");

            mouthPart = serializedObject.FindProperty("mouthPart");
        }

        public override void OnInspectorGUI()
        {
            // Render base GUI
            base.OnInspectorGUI();

            // Humanoid Info
            EditorGUILayout.PropertyField(partParent);
            EditorGUILayout.PropertyField(skeletonPrefix);
            EditorGUILayout.PropertyField(headOffset);
            EditorGUILayout.PropertyField(smoothSkin);

            EditorGUILayout.Space();

            // Give body group info if smoothskin
            if (smoothSkin.boolValue)
            {
                EditorGUILayout.HelpBox("Automatic body group mask generation requires the avatar model to be UV unwrapped, no UV overlap, and all UV coordinates to be between (0,0) and (1,1).", MessageType.Warning);
            }
            // Button and  to generate body group classifications or masks
            if (GUILayout.Button("Generate body groups"))
            {
                HumanoidKit humanoidKit = (HumanoidKit)kit;
                humanoidKit.GenerateBodyGroupData();
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void LipsyncGUI()
        {
            base.LipsyncGUI();

            EditorGUILayout.PropertyField(mouthPart);
        }
    }
#endif
}

