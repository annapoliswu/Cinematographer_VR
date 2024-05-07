using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SILvr.Avatars
{
    [RequireComponent(typeof(SILvrAvatar))]
    public class HandAnimator : RealtimeComponent<HandAnimatorModel>
    {
        [Tooltip("Which hand to animate")]
        public Hand hand;

        [Tooltip("How fast to transition between hand poses")]
        public float animationSpeed = 8;

        [Space()]
        [Tooltip("Input action tracking thumb value")]
        public InputActionReference thumbReference;
        [Tooltip("Input action tracking pointer value")]
        public InputActionReference pointerReference;
        [Tooltip("Input action tracking middle value")]
        public InputActionReference middleReference;

        // References to components
        private SILvrAvatar avatar;
        private Animator animator;

        // References to local animator values
        private float thumb;
        private float pointer;
        private float middle;

        // Whether this is a local component or not
        private bool local;

        protected override void OnRealtimeModelReplaced(HandAnimatorModel previousModel, HandAnimatorModel currentModel)
        {
            if (currentModel != null)
            {
                // If this is a model that has no data set on it, populate it with default values
                if (currentModel.isFreshModel)
                {
                    currentModel.thumb = 0;
                    currentModel.pointer = 0;
                    currentModel.middle = 0;
                }
            }
        }

        void Start()
        {
            // Get components
            avatar = GetComponent<SILvrAvatar>();
            animator = avatar.avatarObject.GetComponent<Animator>();

            // If avatar has no animator, this script serves no purpose. Destroy it.
            if (!animator)
            {
                Destroy(this);
            }

            // Set default values
            thumb = model.thumb;
            pointer = model.pointer;
            middle = model.middle;

            // Check if this script is on the local avatar
            local = avatar.localPlayer != null;
        }

        void Update()
        {
            // Update values to approach model values
            thumb = GetUpdatedValue(thumb, model.thumb);
            pointer = GetUpdatedValue(pointer, model.pointer);
            middle = GetUpdatedValue(middle, model.middle);

            // Update animator using values
            animator.SetFloat(GetParameterName("Thumb"), thumb);
            animator.SetFloat(GetParameterName("Pointer"), pointer);
            animator.SetFloat(GetParameterName("Middle"), middle);

            // If this is the local hand, update model values
            if (local)
            {
                model.thumb = thumbReference.action.ReadValue<float>();
                model.pointer = pointerReference.action.ReadValue<float>();
                model.middle = middleReference.action.ReadValue<float>();
            }
        }

        // Returns animator parameter name for the given finger name
        private string GetParameterName(string finger)
        {
            return hand  + " " + finger;
        }

        // Returns updated value for this frame
        private float GetUpdatedValue(float value, float goal)
        {
            // If value doesn't need to be updated, don't update it
            if (value == goal)
            {
                return goal;
            }

            // Otherwise, update value to approach goal
            float sign = Mathf.Sign(goal - value);
            if ((goal - value) * sign < animationSpeed * Time.deltaTime)
            {
                return goal;
            }
            else
            {
                return value + sign * animationSpeed * Time.deltaTime;
            }      
        }
    }
}
