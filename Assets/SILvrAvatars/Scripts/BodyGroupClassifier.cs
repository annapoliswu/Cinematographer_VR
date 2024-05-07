using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace SILvr.Avatars {

    public static class BodyGroupClassifier
    {
        // Associates bone names with body group
        static private Dictionary<string, HumanoidKit.BodyGroup> boneNameToBodyGroup = new Dictionary<string, HumanoidKit.BodyGroup>
        {
            { "Hips", HumanoidKit.BodyGroup.Torso },
            { "Spine", HumanoidKit.BodyGroup.Torso },
            { "Spine1", HumanoidKit.BodyGroup.Torso },
            { "LeftShoulder", HumanoidKit.BodyGroup.Torso },
            { "RightShoulder", HumanoidKit.BodyGroup.Torso },

            { "Neck", HumanoidKit.BodyGroup.Head },
            { "Head", HumanoidKit.BodyGroup.Head },

            { "LeftArm", HumanoidKit.BodyGroup.Arms },
            { "LeftForeArm", HumanoidKit.BodyGroup.Arms },
            { "LeftHand", HumanoidKit.BodyGroup.Arms },
            { "LeftHandIndex1", HumanoidKit.BodyGroup.Arms },
            { "LeftHandIndex2", HumanoidKit.BodyGroup.Arms },
            { "LeftHandIndex3", HumanoidKit.BodyGroup.Arms },
            { "LeftHandMiddle1", HumanoidKit.BodyGroup.Arms },
            { "LeftHandMiddle2", HumanoidKit.BodyGroup.Arms },
            { "LeftHandMiddle3", HumanoidKit.BodyGroup.Arms },
            { "LeftHandThumb1", HumanoidKit.BodyGroup.Arms },
            { "LeftHandThumb2", HumanoidKit.BodyGroup.Arms },
            { "LeftHandThumb3", HumanoidKit.BodyGroup.Arms },

            { "RightArm", HumanoidKit.BodyGroup.Arms },
            { "RightForeArm", HumanoidKit.BodyGroup.Arms },
            { "RightHand", HumanoidKit.BodyGroup.Arms },
            { "RightHandIndex1", HumanoidKit.BodyGroup.Arms },
            { "RightHandIndex2", HumanoidKit.BodyGroup.Arms },
            { "RightHandIndex3", HumanoidKit.BodyGroup.Arms },
            { "RightHandMiddle1", HumanoidKit.BodyGroup.Arms },
            { "RightHandMiddle2", HumanoidKit.BodyGroup.Arms },
            { "RightHandMiddle3", HumanoidKit.BodyGroup.Arms },
            { "RightHandThumb1", HumanoidKit.BodyGroup.Arms },
            { "RightHandThumb2", HumanoidKit.BodyGroup.Arms },
            { "RightHandThumb3", HumanoidKit.BodyGroup.Arms },

            { "LeftUpLeg", HumanoidKit.BodyGroup.Legs },
            { "LeftLeg", HumanoidKit.BodyGroup.Legs },
            { "LeftFoot", HumanoidKit.BodyGroup.Legs },
            { "LeftToeBase", HumanoidKit.BodyGroup.Legs },

            { "RightUpLeg", HumanoidKit.BodyGroup.Legs },
            { "RightLeg", HumanoidKit.BodyGroup.Legs },
            { "RightFoot", HumanoidKit.BodyGroup.Legs },
            { "RightToeBase", HumanoidKit.BodyGroup.Legs },
        };

        // Return the body group classification for every mesh in the given model
        static public void GenerateBodyGroups(GameObject model, out List<Mesh> meshes, out List<HumanoidKit.BodyGroup> groups)
        {
            // Instantiate lists
            meshes = new List<Mesh>();
            groups = new List<HumanoidKit.BodyGroup>();

            // Iterate through all skinned mesh renderers, classifying a body group for each
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                Mesh mesh = renderer.sharedMesh;

                // Generate group for mesh if one does not yet exist
                if (!meshes.Contains(mesh))
                {
                    meshes.Add(mesh);
                    groups.Add(GenerateBodyGroup(model, renderer, mesh));
                }
            }
        }

        // Return the body group classification for the given model and mesh
        static public HumanoidKit.BodyGroup GenerateBodyGroup(GameObject model, SkinnedMeshRenderer renderer, Mesh mesh)
        {
            // Get body group weights for every vertex in the mesh
            Vector4[] bodyGroupWeights = GetBodyGroupWeights(renderer, mesh);

            // Sum the body group weights
            Vector4 totalWeight = Vector4.zero;
            foreach(Vector4 weight in bodyGroupWeights)
            {
                totalWeight += weight;
            }

            // Get max weight
            float max = Mathf.Max(totalWeight.x, totalWeight.y, totalWeight.z, totalWeight.w);

            // Return body group that corresponds to the max weight
            if (max == totalWeight.x)
            {
                return HumanoidKit.BodyGroup.Head;
            }
            else if (max == totalWeight.z)
            {
                return HumanoidKit.BodyGroup.Arms;
            }
            else if (max == totalWeight.w)
            {
                return HumanoidKit.BodyGroup.Legs;
            }
            else
            {
                return HumanoidKit.BodyGroup.Torso;
            }
        }

        // Return the masks for every mesh in the given model
        static public void GenerateBodyGroupMasks(GameObject model, out List<Mesh> meshes, out List<Texture2D> masks)
        {
            // Instantiate lists
            meshes = new List<Mesh>();
            masks = new List<Texture2D>();

            // Iterate through all skinned mesh renderers, creating a mask for each
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                Mesh mesh = renderer.sharedMesh;

                if (!meshes.Contains(mesh))
                {
                    meshes.Add(mesh);
                    masks.Add(GenerateBodyGroupMask(model, renderer, mesh));
                }
            }

#if UNITY_EDITOR
            // Refresh asset database
            AssetDatabase.Refresh();

            for (int i = 0; i < meshes.Count; i++)
            {
                Mesh mesh = meshes[i];
                string path = GetMaskTexturePath(mesh, model);
                UpdateMaskImporter(path);
                masks[i] = GetMaskTextureFromPath(path);
            }
#endif
        }

        // Generate the mask for a single mesh
        static public Texture2D GenerateBodyGroupMask(GameObject model, SkinnedMeshRenderer renderer, Mesh mesh)
        {
            // Get body group weights for every vertex in the mesh
            Vector4[] bodyGroupWeights = GetBodyGroupWeights(renderer, mesh);
            // Generate mask
            Texture2D mask = GenerateMaskTexture(mesh, bodyGroupWeights, GetMaskSizeFromMesh(mesh));
            // Attempt to save mash 
            SaveMaskTexture(mask, mesh, model);

            return mask;
        }

        // Returns best mask size for the given mesh
        static private int GetMaskSizeFromMesh(Mesh mesh)
        {
            float volume = mesh.bounds.extents.x * mesh.bounds.extents.y * mesh.bounds.extents.z * 10000;

            if (volume < .1f)
            {
                return 32;
            }
            else if (volume < 1)
            {
                return 64;
            }
            else if (volume < 10)
            {
                return 128;
            }
            else
            {
                return 256;
            }

        }

        // Returns the body group weights for the given renderer and mesh
        static private Vector4[] GetBodyGroupWeights(SkinnedMeshRenderer renderer, Mesh mesh)
        {
            Vector4[] bodyGroupWeights = new Vector4[mesh.vertexCount];

            // Get bone weights
            NativeArray<BoneWeight1> boneWeights = mesh.GetAllBoneWeights();
            NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();

            // Get bones
            Transform[] bones = renderer.bones;

            // Iterate over the vertices to get body group weights for each vertex
            int weightIndex = 0;
            for (int vertIndex = 0; vertIndex < mesh.vertexCount; vertIndex++)
            {
                // Remember body group weight for this vertex
                Vector4 bodyGroupWeight = Vector4.zero;
                float totalWeight = 0;

                // For each vertex, iterate over its bone weights
                for (int i = 0; i < bonesPerVertex[vertIndex]; i++)
                {
                    // Get the bone weight, and the corresponding bone
                    BoneWeight1 boneWeight = boneWeights[weightIndex];
                    Transform bone = bones[boneWeight.boneIndex];

                    // Get bone name without prefix (get last item after a _)
                    string[] split = bone.name.Split("_");
                    string boneName = split[split.Length - 1];

                    // Ensure the bone name has a corresponding body group
                    if (boneName != null && boneNameToBodyGroup.ContainsKey(boneName))
                    {
                        // Add the weight of the bone to the proper weight for the bone's body group
                        switch (boneNameToBodyGroup[boneName])
                        {
                            case (HumanoidKit.BodyGroup.Head):
                                bodyGroupWeight.x += boneWeight.weight;
                                break;
                            case (HumanoidKit.BodyGroup.Torso):
                                bodyGroupWeight.y += boneWeight.weight;
                                break;
                            case (HumanoidKit.BodyGroup.Arms):
                                bodyGroupWeight.z += boneWeight.weight;
                                break;
                            case (HumanoidKit.BodyGroup.Legs):
                                bodyGroupWeight.w += boneWeight.weight;
                                break;
                        }
                        // Add weight to the total weight for this vertex
                        totalWeight += boneWeight.weight;
                    }
                    // Increment bone weight index
                    weightIndex++;
                }
                // Remember weights for this vector
                bodyGroupWeights[vertIndex] = bodyGroupWeight / totalWeight;
            }

            return bodyGroupWeights;
        }

        // Creates and saves the actual texture
        static private Texture2D GenerateMaskTexture(Mesh mesh, Vector4[] bodyGroupWeights, int size)
        {
            // Get UV coordinates for vertices
            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);

            // Create the texture
            Texture2D mask = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            Color32[] pixels = mask.GetPixels32();

            // Create queue for flood fill
            LinkedList<Vector2Int> points = new LinkedList<Vector2Int>();
            Dictionary<Vector2Int, Vector4> weights = new Dictionary<Vector2Int, Vector4>();

            // Seed flood fill with uvs
            for (int i = 0; i < uvs.Count; i++)
            {
                Vector2Int point = Vector2Int.RoundToInt(uvs[i] * size);

                points.AddLast(point);
                weights[point] = bodyGroupWeights[i];
            }

            // Flood fill
            while (points.Count > 0)
            {
                Vector2Int point = points.First.Value;
                points.RemoveFirst();

                ProcessPoint(point, new Vector2Int(-1, 0), size, points, weights);
                ProcessPoint(point, new Vector2Int(1, 0), size, points, weights);
                ProcessPoint(point, new Vector2Int(0, -1), size, points, weights);
                ProcessPoint(point, new Vector2Int(0, 1), size, points, weights);
            }

            // Gather pixel colors
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = x + size * y;
                    Color32 color = (Color)weights[new Vector2Int(x, y)];
                    pixels[index] = color;
                }
            }

            // Set pixel colors
            mask.SetPixels32(pixels);
            mask.Apply();

            return mask;
        }

        // Processes point in texture flood fill
        static private void ProcessPoint(Vector2Int parent, Vector2Int offset, int size, LinkedList<Vector2Int> points, Dictionary<Vector2Int, Vector4> weights)
        {
            Vector2Int point = parent + offset;
            if (point.x >= 0 && point.x < size && point.y >= 0 && point.y < size)
            {
                if (!weights.ContainsKey(point))
                {
                    points.AddLast(point);
                    weights[point] = weights[parent];
                }
            }
        }

        static public string GetMaskTextureDirectory(GameObject model)
        {
            return "/SILvrAvatars/Textures/" + model.name;
        }

        static public string GetMaskTexturePath(Mesh mesh, GameObject model)
        {
            return "/SILvrAvatars/Textures/" + model.name + "/" + mesh.name + "_bodyGroupMask.png";
        }

        static public Texture2D GetMaskTextureFromPath(string path)
        {
            Texture2D mask = null;
#if UNITY_EDITOR
            mask =  (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + path, typeof(Texture2D));
#endif
            return mask;
        }

        static private void UpdateMaskImporter(string path)
        {
#if UNITY_EDITOR
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath("Assets" + path);
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
#endif
        }

        static private void SaveMaskTexture(Texture2D mask, Mesh mesh, GameObject model)
        {
#if UNITY_EDITOR
            // Encode file
            byte[] bytes = ImageConversion.EncodeToPNG(mask);
            // Create directory
            System.IO.Directory.CreateDirectory(Application.dataPath + GetMaskTextureDirectory(model));
            // Save texture
            System.IO.File.WriteAllBytes(Application.dataPath + GetMaskTexturePath(mesh, model), bytes);
#endif
        }
    }
}
