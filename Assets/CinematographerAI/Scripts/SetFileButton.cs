using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetFileButton : MonoBehaviour
{

    private DataManager dataManager;
    private TextMeshProUGUI textField;

    // Start is called before the first frame update
    public void Start()
    {
        dataManager = FindObjectOfType<DataManager>();
        textField = this.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnClick()
    {
        textField.text = dataManager.GetFileName();
    }

}
