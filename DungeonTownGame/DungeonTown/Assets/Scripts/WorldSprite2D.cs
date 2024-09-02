using System.Collections;
using System.Collections.Generic;
using UnityEditor.XR;
using UnityEngine;

public class WorldSprite2D : MonoBehaviour
{
    /// <summary>
    /// 0 = front
    /// 1 = left
    /// 2 = back
    /// 3 = right
    /// </summary>
    private int dirIdx;

    private Vector3 currDirection;
    private Quaternion rotationDir;

    [Header("Damage Reading")]
    public Color defaultColor;
    public Color damageColor;
    public float damageTime;
    public Vector3 damageShakeThresholds;

    private Color currColor;
    private float currDamageTime;
    private bool dmgEffects;
    private Vector3 defaultLocation;

    private void Start()
    {
        currDirection = Vector3.right;
        defaultLocation = this.transform.localPosition;

    }

    // Update is called once per frame
    void Update()
    {
        CalculateCameraFacing();
        this.transform.rotation = rotationDir;
        ApplySpriteEffects();
        AssignAnimValues();
    }

    private void CalculateCameraFacing()
    {

        Vector3 playerDir = PlayerControl.instance.player.transform.position - this.transform.position;

        playerDir = new Vector3(playerDir.x, 0, playerDir.z).normalized;


        float dot = Vector3.Dot(playerDir, currDirection);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;

        float side = Vector3.Dot(playerDir, Get2DOrthogonal(currDirection));

        //Use the dot product along the orthogonal to find whether to left or right
        if (side < 0)
        {
            angle *= -1;
        }

        //Rotate by 45 so that the split is along corners not faces
        angle += 235;

        //Adding 45 may put angle above 360, so subtract 360 if over
        if (angle > 360)
        {
            angle -= 360;
        }

        //Sprite Renderer is at 90 degrees to the current facing so adjust toward correct facing
        rotationDir = Quaternion.Euler(0, angle + 45, 0);

        //Divide by 90 and take the int to get index, however the values are all shifted by 2
        dirIdx = ((int)(angle / 90.0f) + 2) % 4;

    }

    public void ApplyDamage()
    {
        currDamageTime = damageTime;
        currColor = defaultColor;
        dmgEffects = true;
    }

    private void ApplySpriteEffects()
    {
        if(damageTime <= 0)
        {
            Debug.LogWarning("WorldSprite2D asset has sin damage time less than or equal to zero.");
            return;
        }

        float prop = Mathf.Sin((currDamageTime / damageTime) * Mathf.PI);
        Vector3 currShake = new Vector3(UnityEngine.Random.Range(0.0f, damageShakeThresholds.x),
                                        UnityEngine.Random.Range(0.0f, damageShakeThresholds.y),
                                        UnityEngine.Random.Range(0.0f, damageShakeThresholds.z));

        this.transform.localPosition = defaultLocation + currShake;

        if(currDamageTime < 0)
        {
            dmgEffects = false;
            this.transform.localPosition = defaultLocation;
        }
        else
        {
            currDamageTime -= Time.deltaTime;
        }

        if (dmgEffects)
        {
            currColor = prop * damageColor + (1 - prop) * defaultColor;
        }
        else
        {
            currColor = defaultColor;
        }

        this.GetComponent<SpriteRenderer>().color = currColor;


    }


    private void AssignAnimValues()
    {
        //this.GetComponent<Animator>().SetInteger("Direction", dirIdx);
    }

    private Vector3 Get2DOrthogonal(Vector3 dir)
    {
        return new Vector3(dir.z, 0, -dir.x);
    }
}
