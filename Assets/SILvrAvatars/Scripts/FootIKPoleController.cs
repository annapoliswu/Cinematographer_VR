using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    public class FootIKPoleController : MonoBehaviour
    {
        public Transform target;
        public Transform root;

        public static FootIKPoleController CreateComponent(GameObject parent, Transform target, int chainLength)
        {
            FootIKPoleController controller = parent.AddComponent<FootIKPoleController>();
            controller.target = target;

            controller.root = target;
            for (int i = 0; i < chainLength + 1; i++)
            {
                controller.root = controller.root.parent;
            }

            return controller;
        }

        // Update is called once per frame
        void Update()
        {
            // Destroy this gameobject if the target has been destroyed
            if (!target)
                Destroy(gameObject);
            else
            {
                // Set rotation the direction perpendicular both to the root's right direction, and the direction from the root facing the foot
                transform.rotation = Quaternion.LookRotation(Vector3.Cross(target.position - root.position, root.right));
                // Put pole position halfway between root and foot, and outward in the direction of the rotation
                transform.position = (root.position + target.position) / 2 + transform.forward;
            }            
        }
    }
}
