using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    public class FootPlacer : MonoBehaviour
    {
        [Tooltip("The root part to raycast from. Usually the hips.")]
        public Transform root;
        [Tooltip("The offset of the foot from the waist. Y offset is ignored and controlled by the raycast.")]
        public Vector3 posOffset = new Vector3(.1f, 0, 0);
        [Tooltip("The offset of this foot's y Euler angle from that of the root.")]
        public float rotOffset = 10;
        [Tooltip("The offset of the foot from the ground, when placed.")]
        public float groundOffset = .1f;
        [Tooltip("Whether this is the right or left foot")]
        public bool isLeft;
        [Space()]
        [Tooltip("How far a foot must move before it starts to lerp")]
        public float distanceThreshold = .15f;
        [Tooltip("How far a foot must rotate before it starts to lerp")]
        public float angleThreshold = 45;
        [Tooltip("Past this distance, feet will teleport instead of lerping")]
        public float teleportThreshold = .3f;
        [Tooltip("How quickly the foot lerps between positions")]
        public float lerpSpeed = 10;
        [Tooltip("The height difference between root and this transform at which point feet are offset by sitOffset")]
        public float sitThreshold = .25f;
        [Tooltip("The offset to add to the foot from the root when sitting")]
        public Vector3 sitOffset = new Vector3(0, 0, .4f);

        private Vector3 lerpOffset;
        private float lerpOffsetMag;
        private Quaternion startRotation;
        private Vector3 startPosition;
        [HideInInspector] public Quaternion goalRotation;
        [HideInInspector] public Vector3 goalPosition;
        private float lerpPercent = 1;

        public static FootPlacer CreateComponent(GameObject parent, Transform foot, int chainLength, bool isLeft)
        {
            FootPlacer footPlacer = parent.AddComponent<FootPlacer>();
            footPlacer.isLeft = isLeft;

            footPlacer.SetRoot(foot, chainLength);

            return footPlacer;
        }

        public static FootPlacer CreateComponent(GameObject parent, Transform foot, int chainLength, Vector3 posOffset, float rotOffset, float groundOffset, bool isLeft)
        {
            FootPlacer footPlacer = parent.AddComponent<FootPlacer>();
            footPlacer.posOffset = posOffset;
            footPlacer.rotOffset = rotOffset;
            footPlacer.groundOffset = groundOffset;
            footPlacer.isLeft = isLeft;

            footPlacer.SetRoot(foot, chainLength);

            return footPlacer;
        }

        private void SetRoot(Transform foot, int chainLength)
        {
            root = foot;
            for (int i = 0; i < chainLength + 1; i++)
            {
                root = root.parent;
            }
        }

        void Start()
        {
            goalRotation = transform.rotation;
            goalPosition = transform.position;

            if (isLeft)
            {
                posOffset.x *= -1;
                rotOffset *= -1;
                sitOffset.x *= -1;
            }

            lerpOffset = posOffset + Quaternion.Euler(0, -90, 0) * posOffset;
            lerpOffsetMag = lerpOffset.magnitude;
        }


        // Update is called once per frame
        void Update()
        {
            // Destroy this gameobject if the root has been destroyed
            if (!root)
                Destroy(gameObject);
            else
            {
                // Get waist rotation
                float angle = root.eulerAngles.y + rotOffset;

                // Calculate starting position for foot raycast
                Vector3 raycastPos = root.position + root.rotation * posOffset;
                // If root (waist) is close to the foot in the y position, move to a more sitting-like position (foot raycasting in front of waist)
                if (root.position.y - transform.position.y < sitThreshold)
                    raycastPos += Quaternion.Euler(0, angle, 0) * sitOffset;

                // Raycast down
                RaycastHit hit;
                // Ignore trigger colliders
                if (Physics.Raycast(raycastPos, -Vector3.up, out hit, 2, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    // Set rotation based on waist rotation and ground normal
                    Quaternion newRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, angle, 0);

                    // Set position to hit point
                    // Raise foot off the ground by groundOffset
                    Vector3 newPosition = hit.point + newRotation * new Vector3(0, groundOffset, 0);

                    // Exaggerate position of foot when comparing distance with lerpOffset
                    // Essentially just moving the right foot to the right and forward, and the left foot to the left and backward
                    // This way, the correct foot moves first when shifting position
                    // If the new position is far enough away from the old one, update the position
                    // Use square distance for performance reasons (sqrt is slow, and they'll be a lot of feet!)
                    float distance = (transform.position - (newPosition + root.rotation * lerpOffset)).sqrMagnitude;
                    // If distance is too far, just teleport the foot
                    if (distance > (teleportThreshold + lerpOffsetMag) * (teleportThreshold + lerpOffsetMag))
                    {
                        goalRotation = newRotation;
                        goalPosition = newPosition;
                        lerpPercent = 1;
                    }
                    // If distance is greater than threshold, or rotation is, lerp to the new position
                    else if ((distance > (distanceThreshold + lerpOffsetMag) * (distanceThreshold + lerpOffsetMag))
                        || (Quaternion.Angle(transform.rotation, newRotation) > angleThreshold))
                    {
                        // Set values for a lerp
                        startRotation = transform.rotation;
                        startPosition = transform.position;

                        goalRotation = newRotation;
                        goalPosition = newPosition;

                        lerpPercent = 0;
                    }
                }

                // Lerp, if necessary
                if (lerpPercent < 1)
                {
                    // Increase lerp percent
                    lerpPercent += lerpSpeed * Time.deltaTime;
                    Mathf.Clamp(lerpPercent, 0, 1);
                    // Perform the lerp
                    transform.rotation = Quaternion.Lerp(startRotation, goalRotation, lerpPercent);
                    transform.position = Vector3.Lerp(startPosition, goalPosition, lerpPercent);
                }
                // Otherwise, keep putting the foot at the position (to eliminate movement due to it's parent moving)
                else
                {
                    transform.rotation = goalRotation;
                    transform.position = goalPosition;
                }
            }
        }
    }
}