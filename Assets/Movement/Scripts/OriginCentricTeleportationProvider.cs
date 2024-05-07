using UnityEngine.Assertions;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
namespace SILvr.Movement
{
    // The default TeleportationProvider centers the camera on the destination
    // This provider centers the origin on the destination
    public class OriginCentricTeleportationProvider : TeleportationProvider
    {
        protected override void Update()
        {
            if (!validRequest || !BeginLocomotion())
                return;

            var xrOrigin = system.xrOrigin;
            if (xrOrigin != null)
            {
                switch (currentRequest.matchOrientation)
                {
                    case MatchOrientation.WorldSpaceUp:
                        xrOrigin.MatchOriginUp(Vector3.up);
                        break;
                    case MatchOrientation.TargetUp:
                        xrOrigin.MatchOriginUp(currentRequest.destinationRotation * Vector3.up);
                        break;
                    case MatchOrientation.TargetUpAndForward:
                        // Match origin exactly
                        xrOrigin.Origin.transform.rotation = currentRequest.destinationRotation;
                        break;
                    case MatchOrientation.None:
                        // Change nothing. Maintain current origin rotation.
                        break;
                    default:
                        Assert.IsTrue(false, $"Unhandled {nameof(MatchOrientation)}={currentRequest.matchOrientation}.");
                        break;
                }

                // Don't center the camera, center the origin
                xrOrigin.Origin.transform.position = currentRequest.destinationPosition;

                // Move floor offset to center camera on the origin
                Vector3 delta = xrOrigin.Origin.transform.position - xrOrigin.Camera.transform.position;
                delta.y = 0;
                xrOrigin.CameraFloorOffsetObject.transform.position += delta;

            }

            EndLocomotion();
            validRequest = false;
        }
    }
}
