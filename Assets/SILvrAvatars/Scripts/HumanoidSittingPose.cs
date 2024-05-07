using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    [CreateAssetMenu(fileName = "Humanoid Sitting Pose", menuName = "SILvr/Avatars/Poses/Humanoid Sitting Pose", order = 4)]
    public class HumanoidSittingPose : HumanoidPose
    {
        private float sittingHeight;

        public override void Enter(SILvrAvatar avatar)
        {
            ResetSkeleton(avatar);

            base.Enter(avatar);

            // Convert avatar data into humanoid data
            HumanoidKit.HumanoidAvatarData data = (HumanoidKit.HumanoidAvatarData)avatar.avatarData;
            HumanoidKit kit = (HumanoidKit)avatar.kit;

            // Place player's hips on the seat
            Vector3 delta = data.root.position - data.hips.position;
            data.root.position += delta;

            // Set max height to difference between head and hips
            sittingHeight = (data.head.position.y + kit.headOffset.y) - data.hips.position.y;
        }

        // Max height should be the sitting height while in this pose
        public override float GetMaxHeight(SILvrAvatar avatar)
        {
            return sittingHeight;
        }
    }
}