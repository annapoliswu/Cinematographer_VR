using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using SILvr.Avatars;

namespace SILvr.Interaction
{
    [RequireComponent(typeof(RealtimeTransform))]
    [RequireComponent(typeof(XRBaseInteractable))]
    public class RealtimeInteractable : RealtimeComponent<RealtimeInteractableModel>
    {
        // The realtime transform networking this object's transform
        private RealtimeTransform realtimeTransform;
        // The interactable allowing this object to be manipulated by the player
        private XRBaseInteractable interactable;
        // The attached rigidbody, if it exists
        private Rigidbody body;

        private SILvrAvatarManager avatarManager;

        void OnEnable()
        {
            // Get references to singletons
            avatarManager = SILvrAvatarManager.Instance;

            // Get references to necessary components
            realtimeTransform = GetComponent<RealtimeTransform>();
            interactable = GetComponent<XRBaseInteractable>();
            body = GetComponent<Rigidbody>();

            // Attach callbacks
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
            avatarManager.OnAvatarAdded += OnAvatarAdded;
            avatarManager.OnAvatarRemoved += OnAvatarRemoved;
        }

        void OnDisable()
        {
            // Remove callbacks
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
            avatarManager.OnAvatarAdded -= OnAvatarAdded;
            avatarManager.OnAvatarRemoved -= OnAvatarRemoved;
        }

        private void OnAvatarAdded(SILvrAvatar avatar, int id)
        {
            // If no owner exists yet, make this new avatar the owner
            if (realtimeTransform.isUnownedInHierarchy)
            {
                realtimeTransform.SetOwnership(id);
            }
        }

        // Called when an avatar is removed
        private void OnAvatarRemoved(SILvrAvatar avatar, int id)
        {
            // If the owner of this interactable has been removed, transfer ownership
            if (realtimeTransform.isUnownedInHierarchy || id == realtimeTransform.ownerIDInHierarchy)
            {
                SetRandomOwner(id);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Normally, we don't care who has ownership of the model, as long as someone does (so the rigidbody can update)
            // However, when a player is explicitely controlling this object, they should be the owner
            realtimeTransform.SetOwnership(avatarManager.localID);
            model.selected = true;
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            model.selected = false;
        }

        // Handle changes in model
        protected override void OnRealtimeModelReplaced(RealtimeInteractableModel previousModel, RealtimeInteractableModel currentModel)
        {
            if (previousModel != null)
            {
                // Unregister from events
                previousModel.selectedDidChange -= SelectedChanged;
            }

            if (currentModel != null)
            {
                // If this is a model that has no data set on it, populate it with default values
                if (currentModel.isFreshModel)
                {
                    currentModel.preventOwnershipTakeover = false;
                    currentModel.destroyWhenOwnerLeaves = false;
                    currentModel.selected = false;
                }

                // Set the owner to a random player if there is no owner yet
                // There must be an owner for the rigidbody to update
                if (realtimeTransform.isUnownedInHierarchy)
                {
                    SetRandomOwner();
                }

                // Register to events
                currentModel.selectedDidChange += SelectedChanged;
            }
        }

        // Called on all clients when this object is picked up / dropped
        private void SelectedChanged(RealtimeInteractableModel model, bool selected)
        {
            if (body)
            {
                body.isKinematic = selected;
            }
        }

        // Tries to assign a random player as the owner of this object
        private void SetRandomOwner(int prevOwner = -1)
        {
            // Get the list of potential ids
            List<int> ids = avatarManager.GetAllIDs();

            // Make sure there is at least one valid player to be the owner
            if (ids.Count > 0)
            {
                // Get a random index
                int index = Random.Range(0, ids.Count);

                Debug.Log(ids.Count + ", " + index);

                // If the index happens to point to the previous owner, increment the index by 1
                if (ids[index] == prevOwner)
                {
                    index = (index + 1) % ids.Count;
                }

                // Transfer ownership to the random id, if there is someone to transfer to
                if (ids[index] >= 0)
                {
                    realtimeTransform.SetOwnership(ids[index]);
                }
            }
        }
    }
}
