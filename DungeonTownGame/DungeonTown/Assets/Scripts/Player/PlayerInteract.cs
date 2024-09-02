using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    private Interactable currInteractable;
    public LayerMask interactableMask;
    public float maxInteractDistance;

    // Update is called once per frame
    void Update()
    {
        if(this.GetComponentInChildren<PlayerDetection>().QueryCameraForward(out RaycastHit hit, interactableMask, maxInteractDistance))
        {
            if(hit.transform.GetComponent<Interactable>() != null)
            {
                hit.transform.GetComponent<Interactable>().SetInteractable(true);

                if(currInteractable == null || !currInteractable.Equals(hit.transform.GetComponent<Interactable>()))
                {
                    currInteractable.SetInteractable(false);
                    currInteractable = hit.transform.GetComponent<Interactable>();
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    currInteractable.Interact();
                }

            }
        }
    }
}
