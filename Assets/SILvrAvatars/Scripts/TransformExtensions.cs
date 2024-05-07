using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    // Makes this transform match the position and rotation of the other transform
    public static void Match(this Transform transform, Transform other)
    {
        transform.position = other.position;
        transform.rotation = other.rotation;
    }
}

// Non-monobehaviour script that stores the same info as a transform
public class TransformValue
{
    public Vector3 position;
    public Quaternion rotation;

    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 localScale;

    // Makes a copy of the given transform by value, not by reference
    public TransformValue(Transform transform)
    {
        position = transform.position;
        rotation = transform.rotation;

        localPosition = transform.localPosition;
        localRotation = transform.localRotation;
        localScale = transform.localScale;
    }
}
