using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonFix : MonoBehaviour
{
    Button button;
    Animator animator;
    public void Start()
    {
        button = GetComponent<Button>();
        animator = GetComponent<Animator>();

    }

    
    public void Fix()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
}
