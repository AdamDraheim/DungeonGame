using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{

    [SerializeField]
    private float maxHorizMoveSpeed;
    [SerializeField]
    private float playerAcceleration;
    [SerializeField]
    private float gravity;
    [SerializeField]
    private float terminalVelocity;

    [SerializeField]
    private float groundFriction;

    [SerializeField]
    private float airResist;

    private bool left, right, up, down, noinput, jump;

    private void Awake()
    {
        PlayerControl.instance.RegisterPlayer(this.gameObject);
        //this.transform.parent = null;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ReadAndAssignInput();
        MovePlayer();
    }

    private void MovePlayer()
    {
        Vector3 newAcceleration = GetBaseMovementOnInput() * playerAcceleration;

        newAcceleration = RotateMovementTowardCameraAngle(newAcceleration);
        newAcceleration = SetMovementToMatchGroundNormal(newAcceleration);

        if (this.GetComponentInChildren<PlayerDetection>().QueryGroundDetection(out RaycastHit hit, true))
        {
            this.GetComponent<Rigidbody>().velocity += newAcceleration;
        }

        EnforceGroundSpeed();
        ApplyGravity();


    }

    private void ReadAndAssignInput()
    {
        left = Input.GetKey(KeyCode.A);
        right = Input.GetKey(KeyCode.D);
        up = Input.GetKey(KeyCode.W);
        down = Input.GetKey(KeyCode.S);

        noinput = !(left | right | up | down);
    }

    /// <summary>
    /// Gets the input and converts into a movement vector
    /// </summary>
    private Vector3 GetBaseMovementOnInput()
    {
        Vector3 currMovement = Vector3.zero;

        if (left)
        {
            currMovement += new Vector3(-1, 0, 0);
        }
        if (right)
        {
            currMovement += new Vector3(1, 0, 0);
        }
        if (up)
        {
            currMovement += new Vector3(0, 0, 1);
        }
        if (down)
        {
            currMovement += new Vector3(0, 0, -1);
        }
        return currMovement;
    }

    /// <summary>
    /// Transforms the current movement vector to match the camera coordinates
    /// </summary>
    private Vector3 RotateMovementTowardCameraAngle(Vector3 currMovement)
    {
        if (PlayerCamera.playerCam == null)
        {
            return currMovement;
        }

        Quaternion rotation = PlayerCamera.playerCam.GetCameraRotation();

        Vector3 rotated_angle = rotation * currMovement;
        Vector3 y_projection = Vector3.up * Vector3.Dot(rotated_angle, Vector3.up);

        return (rotated_angle - y_projection).normalized * Vector3.Magnitude(currMovement);

    }

    /// <summary>
    /// Transforms the current movement vector to match the ground normal
    /// </summary>
    private Vector3 SetMovementToMatchGroundNormal(Vector3 currMovement)
    {
        Vector3 normal = Vector3.up;
        if (this.GetComponentInChildren<PlayerDetection>().QueryGroundDetection(out RaycastHit hit))
        {
            normal = hit.normal;
        }

        Vector3 horiz = Vector3.Cross(normal, Vector3.up).normalized;

        if (normal.Equals(Vector3.up) || horiz.Equals(Vector3.zero))
        {
            horiz = Vector3.right;
        }

        Vector3 straight = Vector3.Cross(normal, horiz);
        Vector3 horiz_proj = Vector3.Dot(horiz, currMovement) * horiz;
        Vector3 straight_proj = Vector3.Dot(straight, currMovement) * straight;

        return horiz_proj + straight_proj;
        

    }

    private void ApplyGravity()
    {
        if (!this.GetComponentInChildren<PlayerDetection>().QueryGroundDetection(out RaycastHit hit, true))
        {

            this.GetComponent<Rigidbody>().velocity += Vector3.down * gravity;

            Vector3 fallingDir = Vector3.Project(this.GetComponent<Rigidbody>().velocity, Vector3.up);

            this.GetComponent<Rigidbody>().velocity = this.GetComponent<Rigidbody>().velocity - fallingDir;
            if (fallingDir.magnitude > terminalVelocity)
            {
                fallingDir = terminalVelocity * (fallingDir / fallingDir.magnitude);
            }
            this.GetComponent<Rigidbody>().velocity = this.GetComponent<Rigidbody>().velocity + fallingDir;
        }

    }

    private void EnforceGroundSpeed()
    {
        if(this.GetComponentInChildren<PlayerDetection>().QueryGroundDetection(out RaycastHit hit, true))
        {
            Vector3 normalSpeed = Vector3.Project(this.GetComponent<Rigidbody>().velocity, hit.normal);
            Vector3 groundSpeed = this.GetComponent<Rigidbody>().velocity - normalSpeed;

            if(groundSpeed.magnitude > maxHorizMoveSpeed)
            {
                groundSpeed = maxHorizMoveSpeed * (groundSpeed / groundSpeed.magnitude);
            }

            if (noinput)
            {
                groundSpeed *= (1-groundFriction);
            }

            this.GetComponent<Rigidbody>().velocity = groundSpeed + normalSpeed;

        }
        else
        {
            Vector3 normalSpeed = Vector3.Project(this.GetComponent<Rigidbody>().velocity, Vector3.up);
            Vector3 airSpeed = this.GetComponent<Rigidbody>().velocity - normalSpeed;

            if (airSpeed.magnitude > maxHorizMoveSpeed)
            {
                airSpeed = maxHorizMoveSpeed * (airSpeed / airSpeed.magnitude);
            }

            airSpeed *= (1 - airResist);
           
            this.GetComponent<Rigidbody>().velocity = airSpeed + normalSpeed;
        }
    }


}
