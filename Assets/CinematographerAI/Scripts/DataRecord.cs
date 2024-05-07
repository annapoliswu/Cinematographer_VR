using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DataRecord : MonoBehaviour
{
    public TextMeshPro textbox;
    public string fileName;
    public TextAsset textAsset;

    public void Start()
    {
        textbox = gameObject.GetComponentInChildren<TextMeshPro>();
        if(textAsset != null)
        {
            fileName = textAsset.name;
            textbox.text = fileName;
        }
    }

    public string GetFileName()
    {
        return fileName;
    }
    public void SetFileName(string fname)
    {
        fileName = fname;
        textbox.text = fileName;
    }
}
