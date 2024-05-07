using DitzelGames.FastIK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    [CreateAssetMenu(fileName = "Humanoid Pose", menuName = "SILvr/Avatars/Poses/Humanoid Pose", order = 2)]
    public class HumanoidPose : Pose
    {
        [Header("Animation Data")]
        [Tooltip("A random name from this list will be chosen as the animation to play")]
        public List<string> animationNames = new List<string> { "Default" };
        public float animationSpeed = 1;
        public enum AnimationStartFrame { First, Random }
        public AnimationStartFrame animationStartFrame = AnimationStartFrame.First;
        [Tooltip("Whether animation motion is added on top of current transforms or not")]
        public bool additive;

        [Header("First Person Visibility Data")]
        [Tooltip("Whether head is visible while in first person in this pose")]
        public bool headVisibleInFirstPerson = false;
        [Tooltip("Whether torso is visible while in first person in this pose")]
        public bool torsoVisibleInFirstPerson = false;
        [Tooltip("Whether arms are visible while in first person in this pose")]
        public bool armsVisibleInFirstPerson = true;
        [Tooltip("Whether legs are visible while in first person in this pose")]
        public bool legsVisibleInFirstPerson = false;

        [Header("Inverse Kinematics Data")]
        [Tooltip("Whether head IK is active while in the pose")]
        public bool headIKActive = true;
        [Tooltip("Whether left hand IK is active while in the pose")]
        public bool leftHandIKActive = true;
        [Tooltip("Whether right hand IK is active while in the pose")]
        public bool rightHandIKActive = true;
        [Tooltip("Whether left foot IK is active while in the pose")]
        public bool leftFootIKActive = true;
        [Tooltip("Whether right foot IK is active while in the pose")]
        public bool rightFootIKActive = true;

        // Reference to transform of bones in the previous frame
        private TransformValue[] prevBones;

        public override void Init(SILvrAvatar avatar)
        {

            // Convert avatar data into humanoid data
            HumanoidKit.HumanoidAvatarData data = (HumanoidKit.HumanoidAvatarData)avatar.avatarData;
            HumanoidKit kit = (HumanoidKit)avatar.kit;

            // Calculate head height
            headHeight = data.head.position.y - data.root.position.y + kit.headOffset.y;

            // Remember previous rotations of bones
            prevBones = new TransformValue[data.bones.Length];
            for (int i = 0; i < prevBones.Length; i++)
            {
                prevBones[i] = new TransformValue(data.bones[i]);
            }
        }

        public override void Enter(SILvrAvatar avatar)
        {
            base.Enter(avatar);

            // Convert avatar data into humanoid data
            HumanoidKit.HumanoidAvatarData data = (HumanoidKit.HumanoidAvatarData)avatar.avatarData;

            // Make sure avatar is in the right place
            MatchPlayer(avatar);
            // Reenable first person to ensure visibility of body parts is updated
            avatar.kit.SetFirstPerson(avatar, avatar.avatarData.firstPerson);
            // Set animation data
            SetAnimation(data);
            // Enable or disable IKs as necessary
            SetIKs(data);
            // Set max height
        }

        // Called every frame this pose is active
        public override void Stay(SILvrAvatar avatar)
        {
            // Convert avatar data into humanoid data
            HumanoidKit.HumanoidAvatarData data = (HumanoidKit.HumanoidAvatarData)avatar.avatarData;

            // Update animator, if required
            if (data.animator)
            {
                // Apply animation on top of any other transform changes
                if (additive)
                {
                    // Remember current position, before animation resets all transforms
                    TransformValue[] transforms = new TransformValue[data.bones.Length];
                    for (int i = 0; i < data.bones.Length; i++)
                    {
                        transforms[i] = new TransformValue(data.bones[i]);
                    }

                    // Update animation
                    data.animator.Update(Time.deltaTime);

                    // Go back and fix things the animation reset
                    for (int i = 0; i < data.bones.Length; i++)
                    {
                        // Get difference in animation between this frame and the last
                        Vector3 deltaPosition = data.bones[i].localPosition - prevBones[i].localPosition;
                        Quaternion deltaRotation = data.bones[i].localRotation * Quaternion.Inverse(prevBones[i].localRotation);
                        Vector3 deltaScale = data.bones[i].localScale - prevBones[i].localScale;

                        // Remember current rotation for next frame
                        prevBones[i] = new TransformValue(data.bones[i]);

                        // Apply differences to original tranform
                        data.bones[i].localPosition = transforms[i].localPosition + deltaPosition;
                        data.bones[i].localRotation = deltaRotation * transforms[i].localRotation;
                        data.bones[i].localScale = transforms[i].localScale + deltaScale;
                    }
                }
                // Update without worrying about overriding other info
                else
                {
                    data.animator.Update(Time.deltaTime);
                }
                
             }
        }

        public override void LateStay(SILvrAvatar avatar)
        {
            // Fix hand rotation, due to conflict between rig orientation and XR Toolkit hand orientation
            if (leftHandIKActive)
            {
                avatar.avatarData.leftHand.rotation *= Quaternion.Euler(270, 90, 0);
            }
            if (rightHandIKActive)
            {
                avatar.avatarData.rightHand.rotation *= Quaternion.Euler(270, -90, 0);
            }
        }

        // Sets animation settings
        protected void SetAnimation(HumanoidKit.HumanoidAvatarData data)
        {
            if (data.animator)
            {
                // Set the speed
                data.animator.speed = animationSpeed;
                // Play the animation either at the start, or on a random frame
                data.animator.Play(animationNames[Random.Range(0, animationNames.Count)], -1, (animationStartFrame == AnimationStartFrame.First) ? 0f : Random.Range(0f, 1f));
                // Force the animator to update, so we can later reference the updated positions
                data.animator.Update(0);
            }  
        }

        // Set which IKs are enabled
        protected void SetIKs(HumanoidKit.HumanoidAvatarData data)
        {
            data.headIK.enabled = headIKActive;
            data.leftHandIK.enabled = leftHandIKActive;
            data.rightHandIK.enabled = rightHandIKActive;
            data.leftFootIK.enabled = leftFootIKActive;
            data.rightFootIK.enabled = rightFootIKActive;
        }

        // Resets rotation of all bones in the given avatar's skeleton
        protected void ResetSkeleton(SILvrAvatar avatar)
        {
            // Convert avatar data into humanoid data
            HumanoidKit.HumanoidAvatarData data = (HumanoidKit.HumanoidAvatarData)avatar.avatarData;

            // Reset each bone
            foreach (Transform bone in data.bones)
            {
                bone.localRotation = Quaternion.identity;
            }
        }
    }
}