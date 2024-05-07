using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MachineButton : MonoBehaviour
{
    
    public UnityEvent buttonEvent;
    public float translateHeight = .1f;
    public int waitTime = 1;
    bool waiting = false;

    private Renderer mRenderer;
    private Material defaultMat;
    public Material pressMat;
    public Material activeMat;

    private void Start()
    {
        mRenderer = gameObject.GetComponent<Renderer>();
        defaultMat = mRenderer.material;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand") && !waiting )
        {
            //Debug.Log("triggered button");

            transform.localPosition = transform.localPosition - new Vector3(0, translateHeight, 0);
            StartCoroutine( Waiter() );
            buttonEvent.Invoke();
        }
    }

    IEnumerator Waiter()
    {
        waiting = true;
        yield return new WaitForSeconds(waitTime);
        transform.localPosition = transform.localPosition + new Vector3(0, translateHeight, 0);
        waiting = false;
    }

    public void SetActive()
    {
        mRenderer.material = activeMat;
    }

    public void SetInactive()
    {
        mRenderer.material = defaultMat;
    }
}
