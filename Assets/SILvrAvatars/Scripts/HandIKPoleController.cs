using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    public class HandIKPoleController : MonoBehaviour
    {
        [Tooltip("The target transform being moved by IK")]
        public Transform target;
        [Tooltip("The root of the IK arm")]
        public Transform root;
        [Tooltip("Which hand this HandIKPoleController represents")]
        public Hand hand;

        // Positive when hand is right, negative when hand is left
        private float sign;

        // Constructor method that attaches a new HandIKPoleController component to the given parent, using the given parameters
        public static HandIKPoleController CreateComponent(GameObject parent, Transform target, int chainLength, Hand hand)
        {
            // Create the component
            HandIKPoleController controller = parent.AddComponent<HandIKPoleController>();
            controller.target = target;
            controller.hand = hand;

            // Find the root based on chain length
            controller.root = target;
            for (int i = 0; i < chainLength; i++)
            {
                controller.root = controller.root.parent;
            }

            // Calculate sign based on handedness
            controller.sign = (hand == Hand.Right) ? 1 : -1;

            return controller;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            // Destroy this gameobject if the target has been destroyed
            if (!target)
            {
                Destroy(gameObject);
            }
            else
            {
                // Get difference between root and target (ignoring the y dimension)
                Vector3 delta = target.position - root.position;
                delta.y = 0;

                // Calculate how inward/outward the target is from the body 
                // (if dot is negative, the target is inwards, positive means outwards)
                float dot = Vector3.Dot(delta.normalized, root.parent.right) * sign;

                // Pole position when target is fully outwards away from the body.
                // Elbow should point backwards and downwards
                Vector3 outwardPos = root.position + delta / 2 - root.parent.forward + Vector3.down;

                // Pole position when target is fully inwards towards the body.
                // Elbow should point out away from the body
                Vector3 inwardPos  = root.position + delta / 2 + root.parent.right * sign;

                // Lerp between the two positions, depending on how inward/outward the target is
                transform.position = Vector3.Lerp(inwardPos, outwardPos, (dot + 1) / 2);              
            }         
        }
    }
}
