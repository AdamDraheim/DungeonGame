using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    protected bool bCanInteract;
    protected bool bExpended;

    public abstract void Interact();

    public void SetInteractable(bool canInteract)
    {
        this.bCanInteract = canInteract;
    }

}
