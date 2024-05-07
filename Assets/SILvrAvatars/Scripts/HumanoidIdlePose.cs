using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    [CreateAssetMenu(fileName = "Humanoid Idle Pose", menuName = "SILvr/Avatars/Poses/Humanoid Idle Pose", order = 3)]
    public class HumanoidIdlePose : HumanoidPose
    {
        // Distance below avatar max height the player defaults to
        private float heightAdjustment = .03f;
        // The maximum difference between the head and waist angle
        protected float waistAngleDif = 60;

        private Vector3 neckOffset; // Offset from head to neck
        private Vector3 torsoOffset; // Offset from neck to hips

        public override void Init(SILvrAvatar avatar)
        {
            base.Init(avatar);

            // Convert avatar data into humanoid data
            HumanoidKit.HumanoidAvatarData data = (HumanoidKit.HumanoidAvatarData)avatar.avatarData;

            // Calculate distance from head to neck, and neck to hips
            neckOffset = data.head.position - data.head.parent.position;
            torsoOffset = data.head.position - data.hips.position - neckOffset;
        }

        // Called every frame this pose is active
        public override void Stay(SILvrAvatar avatar)
        {
            base.Stay(avatar);

            // Convert avatar data into humanoid data
            HumanoidKit.HumanoidAvatarData data = (HumanoidKit.HumanoidAvatarData)avatar.avatarData;
            HumanoidKit kit = (HumanoidKit)avatar.kit;

            // Make avatar match player
            // No need to match hands or head, they are controlled by IK
            data.root.Match(avatar.playerRoot);

            // Lower hips approprately, so torso is straight
            data.hips.position = avatar.playerHead.position - torsoOffset - avatar.playerHead.rotation * (neckOffset + kit.headOffset);

            // Modify the model's spine rotation
            // Get the player's head's y Euler angle
            float angle = avatar.playerHead.eulerAngles.y;
            // Get the difference between the current waist rotation and the head rotation rotation in both directions
            float right = WrapAngle(360 - data.hips.eulerAngles.y + angle);
            float left = WrapAngle(data.hips.eulerAngles.y - angle);
            // If the difference exceeds the threshold, adjust the rotation of the waist
            if (right <= left && right >= waistAngleDif) // turn right
                data.hips.rotation = Quaternion.Euler(0, angle - waistAngleDif, 0);
            else if (left < right && left >= waistAngleDif) // turn left
                data.hips.rotation = Quaternion.Euler(0, angle + waistAngleDif, 0);
        }

        // Default height is slightly below max height
        public override float GetDefaultHeight(SILvrAvatar avatar)
        {
            return headHeight - heightAdjustment;
        }

        // Keeps an angle between 0-360 degrees
        private float WrapAngle(float angle)
        {
            if (angle < 0)
                return angle + 360;
            if (angle > 360)
                return angle - 360;
            else
                return angle;
        }
    }
}