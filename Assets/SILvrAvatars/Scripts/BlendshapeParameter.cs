using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    [System.Serializable]
    public class BlendshapeParameter : AvatarParameter
    {
        public string path; // Path to renderer
        public int blendshape; // Blendshape index on the part's renderer

        public static BlendshapeParameter Create(Kit kit, string name, string path, int blendshape)
        {
            // Create the parameter
            BlendshapeParameter parameter = CreateInstance<BlendshapeParameter>();
            parameter.name = name;
            parameter.min = 0;
            parameter.max = 100;
            parameter.path = path;
            parameter.blendshape = blendshape;

            // Save the parameter
            parameter = Save(kit, parameter);

            // Return the parameter
            return parameter;
        }

        public override void ApplyUnclamped(SILvrAvatar avatar, int value)
        {
            // Try to find the transform
            Transform part = avatar.avatarObject.transform.Find(path);
            if (!part)
            {
                return;
            }

            // Try to get the renderer
            SkinnedMeshRenderer renderer = part.GetComponent<SkinnedMeshRenderer>();
            if (!renderer)
            {
                return;
            }

            // Make sure blendshape index is valid
            if (blendshape >= renderer.sharedMesh.blendShapeCount)
            {
                return;
            }

            // Set the blendshape weight
            renderer.SetBlendShapeWeight(blendshape, value);
        }
    }
}
