using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Camera targeting general")]
    [Tooltip("Focus of the camera")]
    [SerializeField]
    private GameObject target;
    [SerializeField]
    [Tooltip("How far from the player the camera is")]
    private float distanceFromPlayer;
    [Tooltip("How far above the target the camera is")]
    [SerializeField]
    private float heightAbovePlayer;
    [SerializeField]
    [Tooltip("Angle of camera around player in degrees")]
    [Range(0, 360)]
    private float startingTheta;
    [SerializeField]
    [Tooltip("Angle of camera to ground in degrees")]
    [Range(-90, 90)]
    private float groundAnglePhi;
    [SerializeField]
    [Tooltip("Minimum phi value")]
    private float minPhi;
    [SerializeField]
    [Tooltip("Maximum phi value")]
    private float maxPhi;
    [SerializeField]
    [Tooltip("phi rotation speed")]
    private float phiRotationSpeed;
    [SerializeField]
    [Tooltip("Rotation speed of camera around player in degrees")]
    private float rotationThetaSpeed;

    [Header("Additional")]
    [SerializeField]
    [Tooltip("Whether the mouse can control camera direction")]
    private bool MouseControl;
    [SerializeField]
    [Tooltip("Whether theta value can be changed")]
    private bool thetaCanChange;
    [SerializeField]
    [Tooltip("Whether the theta rotation should be snap based or fluid")]
    private bool snapTheta;
    [SerializeField]
    [Tooltip("Whether the phi value can be changed")]
    private bool phiCanChange;

    private Quaternion currentRotation;
    private float currRotationAngle;

    public static PlayerCamera playerCam;

    private void Start()
    {
        if(target == null)
        {
            target = PlayerControl.instance.player;
        }
    }
    private void GetRotateInput()
    {
        float rotateBy = 0;
        float phiChange = 0;

        if (thetaCanChange)
        {
            if (!MouseControl)
            {
                if (!snapTheta)
                {
                    if (Input.GetKey(KeyCode.Q))
                    {
                        rotateBy += rotationThetaSpeed;
                    }
                    if (Input.GetKey(KeyCode.E))
                    {
                        rotateBy -= rotationThetaSpeed;
                    }
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.Q))
                    {
                        rotateBy += rotationThetaSpeed;
                    }
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        rotateBy -= rotationThetaSpeed;
                    }
                }

            }
            else
            {
                Vector2 viewChange = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                rotateBy += viewChange.x * rotationThetaSpeed;
                phiChange -= viewChange.y * phiRotationSpeed;

            }
        }

        //If the rotation should be fluid or snap
        if (!snapTheta)
            rotateBy *= Time.deltaTime;

        currRotationAngle += rotateBy;

        if (currRotationAngle < 0)
        {
            currRotationAngle += 360;
        }
        else if (currRotationAngle >= 360)
        {
            currRotationAngle -= 360;
        }

        if (phiCanChange)
        {
            groundAnglePhi += phiChange * Time.deltaTime;
            groundAnglePhi = clamp(groundAnglePhi, minPhi, maxPhi);
        }
    }

    private void CalculateRotationAndPosition()
    {
        //Create a quaternion rotation by multiplying the current ground rotation by the angle off the ground
        this.currentRotation = Quaternion.AngleAxis(currRotationAngle, Vector3.up) *
            Quaternion.AngleAxis(groundAnglePhi, Vector3.right);


        this.transform.rotation = this.currentRotation;

        //Move the camera back in position to the specified distance
        this.transform.position = target.transform.position + (this.currentRotation * Vector3.back * distanceFromPlayer)
                                                            + (Vector3.up * heightAbovePlayer);

    }


    // Start is called before the first frame update
    void Awake()
    {
        playerCam = this;
        this.currRotationAngle = startingTheta;
    }

    // Update is called once per frame
    void Update()
    {
        GetRotateInput();
        CalculateRotationAndPosition();
    }

    private void OnValidate()
    {
        if (target != null)
        {
            this.currRotationAngle = startingTheta;
            CalculateRotationAndPosition();
        }
    }

    private float clamp(float val, float min, float max)
    {
        return (val >= min ? (val <= max ? val : max) : min);
    }

    public Quaternion GetCameraRotation()
    {
        return this.currentRotation;
    }
}