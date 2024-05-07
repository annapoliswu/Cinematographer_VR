using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars
{
    [System.Serializable]
    public class PartParameter : AvatarParameter
    {
        public string[] paths; // Paths to renderers for this part

        public static PartParameter Create(Kit kit, string name, string[] paths)
        {
            // Create the parameter
            PartParameter parameter = CreateInstance<PartParameter>();
            parameter.name = name;
            parameter.min = 0;
            parameter.max = paths.Length - 1;
            parameter.paths = paths;

            // Save the parameter
            parameter = Save(kit, parameter);

            // Return the parameter
            return parameter;

        }

        public override void ApplyUnclamped(SILvrAvatar avatar, int value)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                // Try to find the transform
                Transform part = avatar.avatarObject.transform.Find(paths[i]);
                if (!part)
                {
                    break;
                }

                // Try to get the renderer
                SkinnedMeshRenderer renderer = part.GetComponent<SkinnedMeshRenderer>();
                if (!renderer)
                {
                    return;
                }

                // Enable renderer if this is the chosen renderer for the part,
                // Otherwise disable
                renderer.enabled = (i == value);
            }
        }
    }
}
