using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

public class PlayerDetection : MonoBehaviour
{
    [Header("Ground Checks")]
    [SerializeField]
    private float checkLength;
    [SerializeField]
    private LayerMask groundCollisionLayerMask;
    [SerializeField]
    private int numGroundQueries;
    [SerializeField]
    private float groundCheckRadius;

    [Header("Detection Checks")]
    [SerializeField]
    private int numSweepChecks;

    [Header("Debug lines")]
    [SerializeField]
    private bool debugGround;
    [SerializeField]
    private bool debugSweep;
    

    struct oldHitResult
    {
        public RaycastHit raycast;
        public bool hit;
    }

    private oldHitResult oldgroundHit;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {

    }

    public bool QueryCameraSweep(out RaycastHit hit, LayerMask mask, float length, float angle_range_deg)
    {
        Quaternion rotation = PlayerCamera.playerCam.GetCameraRotation();

        for (int idx = 0; idx < numSweepChecks; idx++)
        {
            float angle = 2 * angle_range_deg * ((float)idx / numSweepChecks) - (angle_range_deg / 2);
            Vector3 forward = rotation * new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));

            bool result = (Physics.Raycast(this.transform.position, forward, out hit, length, mask));

            if (result)
                return result;

        }
        hit = new RaycastHit();
        return false;
    }

    public bool QueryCameraForward(out RaycastHit hit, LayerMask mask, float length)
    {
        Quaternion rotation = PlayerCamera.playerCam.GetCameraRotation();
        Vector3 forward = rotation * Vector3.forward;

        return (Physics.Raycast(this.transform.position, forward, out hit, length, mask));


    }

    public bool QueryGroundDetection(out RaycastHit hit, bool useResult = false)
    {
        if (!useResult)
        {
            Vector3 down = Vector3.down;
            Vector3 origin = this.transform.position;
            bool result = (Physics.Raycast(origin, down, out hit, checkLength, groundCollisionLayerMask));

            oldgroundHit.hit = result;
            oldgroundHit.raycast = hit;


            if (result)
                return result;

            for (int idx = 0; idx < numGroundQueries; idx++)
            {

                origin = this.transform.position + groundCheckRadius * new Vector3(Mathf.Cos(idx * 3.14f * 2 / numGroundQueries), 0, Mathf.Sin(idx * 3.14f * 2 / numGroundQueries));
                result = (Physics.Raycast(origin, down, out hit, checkLength, groundCollisionLayerMask));

                if (result)
                    return result;

            }

            return result;
        }
        else
        {
            hit = oldgroundHit.raycast;
            return oldgroundHit.hit;
        }
    }

    private void OnDrawGizmos()
    {
        if (debugGround)
        {
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * checkLength);
            for (int idx = 0; idx < numGroundQueries; idx++)
            {

                Vector3 origin = this.transform.position + (groundCheckRadius * new Vector3(Mathf.Cos(idx * 3.14f * 2 / numGroundQueries), 0, Mathf.Sin(idx * 3.14f * 2 / numGroundQueries)));
                Gizmos.DrawLine(origin, origin + Vector3.down * checkLength);

            }
        }

        if (debugSweep)
        {
            if (PlayerCamera.playerCam)
            {
                float angle_range = 45.0f;
                Quaternion rotation = PlayerCamera.playerCam.GetCameraRotation();

                for (int idx = 0; idx < numSweepChecks; idx++)
                {
                    float angle = 2 * angle_range * ((float)idx / numSweepChecks) - (angle_range / 2);
                    Vector3 forward = rotation * new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));

                    Gizmos.DrawLine(this.transform.position, forward * 2.0f + this.transform.position);


                }
            }
        }
    }
}