using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    [CreateAssetMenu(fileName = "Humanoid Animation Pose", menuName = "SILvr/Avatars/Poses/Humanoid Animation Pose", order = 5)]
    public class HumanoidAnimationPose : HumanoidPose
    {
        [Header("Third Person Data")]
        [Tooltip("The third-person offset applied in this pose")]
        public Vector3 cameraOffset = new Vector3(0, .125f, -.5f);
        [Tooltip("The time the offset takes")]
        public float offsetTime = .1f;

        public override void Enter(SILvrAvatar avatar)
        {
            base.Enter(avatar);

            // Reset skeleton before animating, in case any bone isn't keyed
            ResetSkeleton(avatar);

            // Convert avatar data into humanoid data
            HumanoidKit.HumanoidAvatarData data = (HumanoidKit.HumanoidAvatarData)avatar.avatarData;

            // Rotate whole avatar in head's direction
            data.root.rotation = Quaternion.Euler(0, avatar.playerHead.eulerAngles.y, 0);
            data.hips.rotation = data.root.rotation;

        }

        public override void Stay(SILvrAvatar avatar)
        {
            base.Stay(avatar);

            // Convert avatar data into humanoid data
            HumanoidKit.HumanoidAvatarData data = (HumanoidKit.HumanoidAvatarData)avatar.avatarData;

            // Get animation info
            AnimatorStateInfo info = data.animator.GetCurrentAnimatorStateInfo(data.animator.layerCount - 1);

            // Offset local player into third person if there is one
            if (avatar.localPlayer != null)
            {
                // Calculate current time of the animation
                float time = info.normalizedTime * info.length;

                // As pose starts, move the local player into third person
                if (time <= offsetTime)
                {
                    avatar.AddCameraOffset(data.root.rotation * cameraOffset / offsetTime * Time.deltaTime);
                }
                // As the pose ends, move the local player back into first person
                else if (time >= info.length - offsetTime)
                {
                    avatar.AddCameraOffset(data.root.rotation * -cameraOffset / offsetTime * Time.deltaTime);
                }
            }

            // Check if animation is complete 
            if (info.normalizedTime >= 1)
            {
                // If animation is complete, return to idle pose
                avatar.TriggerPoseChangeEvent(PoseType.Idle);
            }
        }

        public override void Exit(SILvrAvatar avatar)
        {
            base.Exit(avatar);

            // Reset any rotational changes
            ResetSkeleton(avatar);
            MatchPlayer(avatar);

            // Reset offset
            avatar.SetCameraOffset(Vector3.zero);
        }
    }
}

