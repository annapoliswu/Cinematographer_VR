using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SILvr.Avatars;

public class InputActionControls : MonoBehaviour
{

    [Tooltip("t")]
    public InputActionReference primaryRightRef;
    public InputActionReference secondaryRightRef;
    public InputActionReference primaryLeftRef;
    public InputActionReference secondaryLeftRef;

    public SILvrAvatar avatar;
    public DataManager dataManager;
    public GameObject recordingIndicator;
    private bool isRecording;
    private bool isReplaying;


    private void Start()
    {
        dataManager = FindObjectOfType<DataManager>();
        avatar = this.GetComponent<SILvrAvatar>();
        isRecording = false;
        isReplaying = false;

        // Attach callback to button press
        if (avatar.isOwnedLocallyInHierarchy)
        {
            primaryRightRef.action.performed += PrimaryRightAction;
            secondaryRightRef.action.performed += SecondaryRightAction;
        }
    }

    private void PrimaryRightAction(InputAction.CallbackContext context)
    {
        if (avatar.isOwnedLocallyInHierarchy)
        {
            if (isRecording) //toggle 
            {
                dataManager.StopInvoking();
                isRecording = false;
            }
            else
            {
                dataManager.InvokeRecordData();
                isRecording = true;
            }

            recordingIndicator.gameObject.SetActive(isRecording);

        }
    }

    private void SecondaryRightAction(InputAction.CallbackContext context)
    {
        if (avatar.isOwnedLocallyInHierarchy)
        {
            if (isReplaying) //toggle 
            {
                dataManager.StopInvoking();
                isReplaying = false;
            }
            else
            {
                dataManager.InvokeReplayData();
                isReplaying = true;
            }
        }
    }
}
