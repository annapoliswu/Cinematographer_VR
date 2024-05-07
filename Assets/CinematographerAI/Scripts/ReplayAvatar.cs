using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Utility;
using Normal.Realtime;
using SILvr.Avatars;

public class ReplayAvatar : SILvrAvatar
{
    #region variables
    public Transform rootOverride;
    public Transform headOverride;
    public Transform leftHandOverride;
    public Transform rightHandOverride;
    public Transform hatTransform;
    public Transform labelTransform;
    public ArrayList meshRenderers;
    private float voiceThreshold = .2f;
    int? index = null;
    #endregion
    protected override RealtimeAvatar.LocalPlayer GetLocalPlayer(){ return null; }
    protected override Transform GetPlayerRoot() { return rootOverride; }
    protected override Transform GetPlayerHead() { return headOverride; }
    protected override Transform GetPlayerLeftHand() { return leftHandOverride; }
    protected override Transform GetPlayerRightHand() { return rightHandOverride; }


    //set transform, pose, and display talking
    public void SetAvatar(AvatarData data, int newIndex)
    {
        SetAvatarTransform(data.rootTransform, data.headTransform, data.leftHandTransform, data.rightHandTransform);
        PoseType newPose = (PoseType)data.poseType;
        if (this.currentPose?.poseType != newPose) //currentPose may be null sometimes..
        {
            this.TriggerPoseChangeEvent((PoseType)data.poseType, rootOverride.position);
        }
        bool isTalking = data.voiceVolume >= voiceThreshold;
        avatarObject.GetComponent<ReplayModelDisplay>().SetTalking(isTalking); //must getcomponent for some reason

        if(index == null) //set only once
        {
            index = newIndex;
            string label = newIndex == 0 ? "A" : "B";
            avatarObject.GetComponent<ReplayModelDisplay>().SetLabelText(label, labelTransform);
            //avatarObject.GetComponent<ReplayModelDisplay>().SetHat(newIndex, hatTransform);
            // probably a race condition happening?
        }

    }

    public void SetAvatarTransform(sTransform root, sTransform head, sTransform left, sTransform right)
    {
        MatchTransform(rootOverride, root);
        MatchTransform(headOverride, head);
        MatchTransform(leftHandOverride, left);
        MatchTransform(rightHandOverride, right);
    }

    public void MatchTransform(Transform transform, sTransform sTrans)
    {
        transform.position = sTrans.position;
        transform.rotation = sTrans.rotation;
    }

}
