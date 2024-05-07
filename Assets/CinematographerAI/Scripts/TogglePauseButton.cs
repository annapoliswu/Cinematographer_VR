using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TogglePauseButton : MonoBehaviour
{

    private DataManager dataManager;
    private TextMeshProUGUI textField;
    private Toggle toggle;
    public Color onColor;
    public Color offColor;

    // Start is called before the first frame update
    public void Start()
    {
        dataManager = FindObjectOfType<DataManager>();
        textField = this.GetComponentInChildren<TextMeshProUGUI>();
        toggle = GetComponent<Toggle>();
        ToggleButton(toggle.isOn);
        
        dataManager.onDataStateChange.AddListener(OnDataChange);
    }

    public void OnDataChange() //on data change instead for keyboard presses too
    {
        DataManager.State dataState = dataManager.GetState();
        if (dataState == DataManager.State.PausedReplay || dataState == DataManager.State.PausedWriteLabel)
        {
            ToggleButton(true);
        }
        else
        {
            ToggleButton(false);
        }
    }

    private void ToggleButton(bool isOn)
    {
        var colorsArray = toggle.colors;
        colorsArray.normalColor = isOn ? onColor : offColor;
        textField.text = isOn ? "Paused" : "Unpaused";
        toggle.colors = colorsArray;
    }


}
