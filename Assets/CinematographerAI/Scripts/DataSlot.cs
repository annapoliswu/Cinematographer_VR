using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataSlot : MonoBehaviour
{
    [SerializeField]
    private DataManager dataManager;
    private void Start()
    {
        dataManager = FindObjectOfType<DataManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        DataRecord dataRecord = other.gameObject.GetComponent<DataRecord>();
        string fileName;
        if (dataRecord)
        {
            fileName = dataRecord.GetFileName();
            dataManager.SetFile(fileName);
        }
    }
}
