using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    [CreateAssetMenu(fileName = "Pose", menuName = "SILvr/Avatars/Poses/Pose", order = 1)]
    public class Pose : ScriptableObject
    {
        [Tooltip("The pose type for this pose")]
        public PoseType poseType;
        [Tooltip("Whether the player can teleport while in this pose")]
        public bool canTeleport;

        protected float headHeight;

        // Called when avatar is first instantiated
        public virtual void Init(SILvrAvatar avatar)
        {
            headHeight = avatar.avatarData.head.position.y - avatar.avatarData.root.position.y;
        }

        // Called when this pose is first entered
        public virtual void Enter(SILvrAvatar avatar)
        {
        }

        // Called every frame this pose is active
        public virtual void Stay(SILvrAvatar avatar)
        {
            MatchPlayer(avatar);
        }

        // Equivalent of LateUpdate
        public virtual void LateStay(SILvrAvatar avatar)
        {
        }

        // Called when this pose is ended
        public virtual void Exit(SILvrAvatar avatar)
        {

        }

        // Make all avatar transforms match the player
        protected void MatchPlayer(SILvrAvatar avatar)
        {
            // Make avatar match player
            avatar.avatarData.root.Match(avatar.playerRoot);
            avatar.avatarData.head.Match(avatar.playerHead);
            avatar.avatarData.leftHand.Match(avatar.playerLeftHand);
            avatar.avatarData.rightHand.Match(avatar.playerRightHand);
        }

        // Returns max height of head relative to root for this pose
        public virtual float GetMaxHeight(SILvrAvatar avatar)
        {
            return headHeight;
        }

        // Returns default height of head relative to root for this pose
        public virtual float GetDefaultHeight(SILvrAvatar avatar)
        {
            return GetMaxHeight(avatar);
        }
    }
}
