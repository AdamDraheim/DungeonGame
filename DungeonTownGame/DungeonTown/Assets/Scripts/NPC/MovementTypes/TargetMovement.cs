using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMovement : NPCMovement
{

    private GameObject target;
    private float nodePref;
    public TargetMovement(GameObject target, float nodePref) : base()
    {
        this.target = target;
        this.nodePref = nodePref;
    }

    public override Vector3 Move(Vector3 position)
    {
        return FollowMap(position);
    }

    private Vector3 FollowMap(Vector3 position)
    {
        Vector3 move = MovementMap.map.GetTrajectory(position, target.transform.position, 400, nodePref);
        string[] masks = {"Blockable"};
        RaycastHit hit;
        if (Physics.Raycast(position, move, out hit, move.magnitude, LayerMask.GetMask(masks)))
        {
            Vector3 normal = hit.normal;
            //move = (0.5f * move) + (0.5f * normal);
        }
        

        if(move == Vector3.zero)
        {
            //move = (target.transform.position - position).normalized;
        }
        return move;
    }

    public override string debugMessage()
    {
        return "(Target) Targeting Player at " + target.transform.position.ToString();
    }
}
