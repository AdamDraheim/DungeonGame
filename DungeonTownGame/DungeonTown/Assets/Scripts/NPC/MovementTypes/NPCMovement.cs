using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NPCMovement
{

    public abstract Vector3 Move(Vector3 currPosition);
    public abstract string debugMessage();

    public virtual void Reset() { }
}
