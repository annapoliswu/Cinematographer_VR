using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Normal.Realtime;

namespace SILvr.Avatars
{
    [RequireComponent(typeof(TeleportationAnchor))]
    public class TeleportPoseTrigger : RealtimeComponent<TeleportPoseTriggerModel>
    {
        [Tooltip("The pose type to make the local player enter when this is teleported to")]
        public PoseType poseType;
        [Tooltip("The UI element to display when this teleport trigger is hovered over. Optional")]
        public Canvas ui;

        private TeleportationAnchor anchor;
        private Vector3 position;

        private Vector3 uiMaxScale;

        // Handle changes in model
        protected override void OnRealtimeModelReplaced(TeleportPoseTriggerModel previousModel, TeleportPoseTriggerModel currentModel)
        {
            if (previousModel != null)
            {
                // Unregister from events
                previousModel.activeDidChange -= ActiveChanged;
            }

            if (currentModel != null)
            {
                // If this is a model that has no data set on it, populate it with default values
                if (currentModel.isFreshModel)
                {
                    currentModel.destroyWhenOwnerLeaves = true;
                    currentModel.active = false;
                }

                ActiveChanged(currentModel, currentModel.active);

                // Register to events
                currentModel.activeDidChange += ActiveChanged;
            }      
        }

        // Called when active status changes
        private void ActiveChanged(TeleportPoseTriggerModel model, bool active)
        {
            // If trigger is already active (i.e. used by another player) disable teleporting to this anchor
            anchor.enabled = !active;

            // Disable collisions
            foreach (Collider collider in anchor.colliders)
            {
                collider.enabled = !active;
            }

            // Disable UI
            if (ui)
            {
                ui.gameObject.SetActive(false);
            }
        }
        
        private void OnEnable()
        {
            // Get anchor, and attach appropriate callback
            anchor = GetComponent<TeleportationAnchor>();
            anchor.teleporting.AddListener(OnTeleportQueued);
        }

        private void OnDisable()
        {
            // Remove callbacks on disable
            if (anchor)
            {
                anchor.teleporting.RemoveListener(OnTeleportQueued);
            }
        }

        private void Start()
        {
            if (ui)
            {
                // Remember max UI scale
                uiMaxScale = ui.transform.localScale;

                // Disable UI
                ui.transform.localScale = Vector3.zero;
                ui.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Update the UI
            if (ui)
            {
                if (anchor.isHovered)
                {
                    // Activate UI if necessary
                    if (!ui.gameObject.activeSelf)
                    {
                        ui.gameObject.SetActive(true);
                    }

                    // Animate UI size to grow
                    AnimateUISize(uiMaxScale);
                }
                else if (ui.gameObject.activeSelf)
                {
                    // Animate UI size to shrink
                    AnimateUISize(Vector3.zero);

                    // If below a certain threshold, hide the UI
                    if (ui.transform.localScale.sqrMagnitude < .00001f)
                    {
                        ui.transform.localScale = Vector3.zero;
                        ui.gameObject.SetActive(false);
                    }
                }

                // Rotate UI to face player
                if (ui.gameObject.activeSelf && SILvrAvatar.localAvatar)
                {
                    ui.transform.rotation = Quaternion.LookRotation(ui.transform.position - SILvrAvatar.localAvatar.localPlayer.head.position, Vector3.up);
                }
            }
        }

        private void AnimateUISize(Vector3 goalSize)
        {
            ui.transform.localScale += (goalSize - ui.transform.localScale) * 8 * Time.deltaTime;
        }

        // Called when a teleport to this object is queued (but not finished)
        private void OnTeleportQueued(TeleportingEventArgs args)
        {
            // Get ownership
            model.RequestOwnership();
            // When a teleport to this anchor is queued, get attach a callback to the provider's end locomotion event
            anchor.teleportationProvider.beginLocomotion += OnTeleportStarted;
            anchor.teleportationProvider.endLocomotion += OnTeleportFinished;
            // Save position the player is teleporting to
            position = args.teleportRequest.destinationPosition;
        }

        // Called when ANY teleport starts
        private void OnTeleportStarted(LocomotionSystem locomotionSystem)
        {
            // Toggle active on this trigger
            model.active = !model.active;

            // The second time this function is called (i.e. when user is teleporting away) active is false
            if (!model.active)
            {
                // Reset pose
                SILvrAvatar.localAvatar.TriggerPoseChangeEvent(PoseType.Idle);

                // Relinquish control
                model.ClearOwnership();

                // Remove anchors once this object is no longer used
                anchor.teleportationProvider.beginLocomotion -= OnTeleportStarted;
                anchor.teleportationProvider.endLocomotion -= OnTeleportFinished;
            }
        }

        // The first time this callback is called is when this object is teleported to.
        // Setting the pose happens when teleport finishes, to give time to reset pose
        private void OnTeleportFinished(LocomotionSystem locomotionSystem)
        {
            if (model.active)
            {
                // Set pose
                SILvrAvatar.localAvatar.TriggerPoseChangeEvent(poseType, position);
            }
        }
    }
}
