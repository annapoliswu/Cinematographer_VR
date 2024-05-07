using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ReplayModelDisplay : MonoBehaviour
{
    [SerializeField]
    private List<SkinnedMeshRenderer> renderers;
    [SerializeField]
    private Material talkingMat;
    [SerializeField]
    private Material defaultMat;
    [SerializeField]
    private bool? talkingState = null; //talking or not 

    [SerializeField]
    private TextMesh avatarLabel;
    [SerializeField]
    private GameObject labelPrefab;


    [SerializeField]
    private List<GameObject> hats;

    private void Start()
    {
        talkingState = null;
    }

    //switches avatar material based on boolean passed in
    public void SetTalking(bool newTalkingState)
    {
        
        if(talkingState != newTalkingState) { //only set renderers if changed
            talkingState = newTalkingState;
            Material currentMat = (bool)talkingState ? talkingMat : defaultMat;
            foreach (SkinnedMeshRenderer r in renderers)
            {
                Material[] mats = {currentMat};
                r.materials = mats;
            }
        }
    }

    public void SetHat(int hatIndex, Transform parent)
    {
        if(hatIndex <= hats.Count)
        {
            GameObject hat = Instantiate(hats[hatIndex], parent.position, parent.rotation );
            hat.transform.SetParent(parent);
        }
        else
        {
            Debug.Log("Index error: hat does not exist for index");
        }
    }
    //have to set head parented object this way because of avatar instantiation process
    //children to avatar model do not move with parent -> need to parent to player / player overrides
    public void SetLabelText(string someText, Transform parent)
    {
        //Debug.Log(someText);
        GameObject label = Instantiate(labelPrefab, parent.position, parent.rotation );
        label.transform.SetParent(parent);
        label.GetComponent<TextMesh>().text = someText;
        //avatarLabel.text = someText;
    }

}
