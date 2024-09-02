using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCMovementHandler : MonoBehaviour
{
#region Structures

    [Serializable]
    public enum ActionType
    {
        Wander,
        Loiter,
        Chase,
    }

    [Serializable]
    public struct MovementBehavior
    {
        [SerializeField]
        public string docstring;
        [SerializeField]
        public ActionType actionType;
        [SerializeField]
        [Tooltip("Options: Track, Meander")]
        public string movementAlgorithm;
        [SerializeField]
        public float movementSpeed;
        [SerializeField]
        public string[] args;
    }

#endregion

#region Editor Settings
    [Header("Movement")]
    [SerializeField]
    private LayerMask ground;
    [SerializeField]
    private float groundDistance;

    [Header("Aggression")]
    [SerializeField]
    private float discoverDistance;
    [SerializeField]
    private float aggressionTime;
    [SerializeField]
    [Tooltip("In degrees")]
    private float angleOfDetection;
    [SerializeField]
    private LayerMask blockView;

    [Header("Movement Behavior Presets")]
    [SerializeField]
    public List<MovementBehavior> movementBehaviors;
    [SerializeField]
    public ActionType defaultBehavior;

    [Header("Debug Information")]
    [SerializeField]
    [Tooltip("Information about current movement")]
    private string debugMovementMessageInfo;

    #endregion
    #region private variables

    private ActionType currActionType;
    private float movementSpeed;

    private NPCMovement currMovement;
    private Dictionary<ActionType, MovementBehavior> movementTypeMapping;

    private Vector3 currVelocity;

    //Aggression
    private Vector3 viewDirection;
    private float currAggressionTimer;

    #endregion

    void Awake()
    {
        movementTypeMapping = new Dictionary<ActionType, MovementBehavior>();
        foreach(MovementBehavior behavior in movementBehaviors)
        {
            movementTypeMapping.Add(behavior.actionType, behavior);
        }
        selectMovementScheme();

    }

    public void Move()
    {
        this.currVelocity = currMovement.Move(this.transform.position);

        if (Physics.Raycast(this.transform.position, this.currVelocity.normalized, 0.25f, blockView))
        {
            currMovement.Reset();
        }
        else
        {
            this.transform.position += this.currVelocity * movementSpeed * Time.deltaTime;
        }
        this.viewDirection = this.currVelocity.normalized;
        this.GetComponent<NPC>().SetView(this.viewDirection);
        this.debugMovementMessageInfo = currMovement.debugMessage();
        PlaceToGround();

        //Actions that can update action type
        ActionType tempAction = currActionType;
        CheckAggression();

        if (currActionType != tempAction)
        {
            selectMovementScheme();
        }
    }

    private void CheckAggression()
    {

        if (!this.GetComponent<NPC>().checkAggro())
        {
            this.currActionType = this.defaultBehavior;
        }
        else
        {
            this.currActionType = ActionType.Chase;
        }

        Vector3 target = PlayerControl.instance.player.transform.position;
        Vector3 dir = target - this.transform.position;

        if(dir.magnitude < discoverDistance)
        {
            float angle = Mathf.Acos(Vector3.Dot(viewDirection, dir.normalized));
            if(Mathf.Rad2Deg * angle <= angleOfDetection)
            {
                if(!Physics.Raycast(this.transform.position, dir, dir.magnitude, blockView))
                {
                    this.currActionType = ActionType.Chase;
                    currAggressionTimer = aggressionTime;

                    this.GetComponent<NPC>().AggroNPC();
                }
            }
        }
    }

    private void PlaceToGround()
    {
        if (Physics.Raycast(this.transform.position, Vector3.down, out RaycastHit hit, 1.5f, ground))
        {
            this.transform.position = hit.point + (Vector3.up * groundDistance);
        }
    }

    private void selectMovementScheme()
    {

        MovementBehavior behavior = this.movementTypeMapping[currActionType];
        this.movementSpeed = behavior.movementSpeed;
        string[] args = behavior.args;

        switch (behavior.movementAlgorithm.ToLower())
        {
            case "track":
                if (args.Length != 1)
                {
                    Debug.LogWarning("NPC MovementType is set to track, but does not have one argument input. " +
                                     "Track must have argument: Node Follow Preference");
                    return;
                }
                float.TryParse(args[0], out float nodePref);

                currMovement = new TargetMovement(PlayerControl.instance.player, nodePref);
                break;
            case "meander":
                if(args.Length != 5)
                {
                    Debug.LogWarning("NPC MovementType is set to meander, but does not have five arguments input. " +
                                     "Meander must have arguments: layermask name, loiter time, walking time, time scale, sigma w");
                    return;
                }
                float.TryParse(args[1], out float loiterTime);
                float.TryParse(args[2], out float walkingTime);
                float.TryParse(args[3], out float timescale);
                float.TryParse(args[4], out float sigmaW);
                currMovement = new MeanderMovement(args[0], loiterTime, walkingTime, timescale, sigmaW);
                break;
        }
    }

    public Vector3 GetVelocity()
    {
        return this.currVelocity;
    }

    private void OnDrawGizmos()
    {
        if(viewDirection.magnitude != 0)
        {
            Gizmos.DrawLine(this.transform.position, this.transform.position + viewDirection * discoverDistance);
            float dist = discoverDistance * Mathf.Tan(Mathf.Deg2Rad * angleOfDetection);
            Gizmos.DrawWireSphere(this.transform.position + viewDirection * discoverDistance, dist);
        }
    }
}
