using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SILvr.Movement
{
    public class TeleportIndicatorController : MonoBehaviour
    {
        [Tooltip("The XRRayInteractor controlling teleportation")]
        public XRRayInteractor interactor;
        [Tooltip("The indicator GameObject to place at the end of the ray")]
        public GameObject indicator;
        [Tooltip("The interaction layer index of the NavMesh")]
        public int navMeshInteractionLayer = 1;
        [Space()]
        [Tooltip("The color gradient of the ray when a valid teleportation area is hovered")]
        public Gradient validAreaColorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
        };
        [Tooltip("The color gradient of the ray when a valid teleportation anchor is hovered")]
        public Gradient validAnchorColorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.yellow, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
        };

        // The line visual script for the interactor ray
        private XRInteractorLineVisual lineVisual;
        // The renderer for the indicator object
        private Renderer indicatorRenderer;
        // The material property block for the indicator
        private MaterialPropertyBlock block;

        // Start is called before the first frame update
        void Start()
        {
            indicatorRenderer = indicator.GetComponent<Renderer>();
            lineVisual = interactor.GetComponent<XRInteractorLineVisual>();
            block = new MaterialPropertyBlock();
        }

        // Update is called once per frame
        void Update()
        {
            // Get the hovered object (if it exists), and wheter it is on the nav mesh layer
            IXRHoverInteractable hoverable = null;
            bool navMeshHover = false;
            if (interactor.interactablesHovered.Count > 0)
            {
                hoverable = interactor.interactablesHovered[0];
                navMeshHover = hoverable.interactionLayers == (hoverable.interactionLayers | (1 << navMeshInteractionLayer));
            }

            // If it is a nav mesh hover, get the raycast hit point for interactor   
            if (navMeshHover && interactor.TryGetCurrent3DRaycastHit(out RaycastHit hit, out int raycastHitIndex))
            {
                // Enable indicator
                indicator.SetActive(true);

                // If this is an anchor, place the indicator at the anchor position
                if (hoverable is TeleportationAnchor)
                {
                    lineVisual.validColorGradient = validAnchorColorGradient;
                    indicator.transform.position = ((TeleportationAnchor)hoverable).teleportAnchorTransform.position;
                }
                else
                {
                    lineVisual.validColorGradient = validAreaColorGradient;
                    indicator.transform.position = hit.point;
                }

                // Set marker color to valid color
                block.SetColor("_MarkerColor", lineVisual.validColorGradient.Evaluate(1));
                indicatorRenderer.SetPropertyBlock(block);
            }
            else
            {
                indicator.SetActive(false);
            }
        }
    }
}
