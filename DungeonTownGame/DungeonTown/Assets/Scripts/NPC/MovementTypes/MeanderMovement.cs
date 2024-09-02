using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class MeanderMovement : NPCMovement
{

    private Vector3 currVel;
    private bool bWalking;
    private float currTime;

    private LayerMask mask;
    private float loiter;
    private float walking;
    private float sigmaW;
    private float timescale;

    public MeanderMovement(string layermask, float loiter_time, float walking_time, float timescale, float sigmaW) : base()
    {
        bWalking = UnityEngine.Random.Range(0.0f, 1.0f) < 0.5 ? true : false;
        if (bWalking)
        {
            currTime = walking_time;
        }
        else
        {
            currTime = loiter_time;
        }
        this.mask = LayerMask.NameToLayer(layermask);
        this.loiter = loiter_time; 
        this.walking = walking_time;
        this.sigmaW = sigmaW;
        this.timescale = timescale;
    }

    public override Vector3 Move(Vector3 position)
    {
        currTime -= Time.deltaTime;

        if(currTime < 0.0f)
        {
            bWalking = !bWalking;
            if (bWalking) 
            {
                currTime = walking;
                currVel = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0, UnityEngine.Random.Range(-1.0f, 1.0f));
                if (currVel.magnitude == 0) currVel = Vector3.forward;
                currVel.Normalize();
            } 
            else 
            {
                currTime = loiter;
            }
        }

        if (!this.bWalking)
        {
            return Vector3.zero;
        }
        else
        {
            currVel = RandomMotion(currVel);

            if (Physics.Raycast(position, currVel, currVel.magnitude * 2.0f, this.mask))
            {
                currVel *= -1;
            }
            return currVel.normalized;
        }

    }


    public override string debugMessage()
    {
        if (bWalking)
        {
            return "(Meander) Walking: Curr velocity - " + currVel.ToString() + " Time left - " + currTime.ToString();
        }
        else
        {
            return "(Meander) Waiting: Time left - " + currTime.ToString();
        }
    }

    public override void Reset()
    {
        currVel = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0, UnityEngine.Random.Range(-1.0f, 1.0f));
        if (currVel.magnitude == 0) currVel = Vector3.forward;
        currVel.Normalize();
    }

    /// <summary>
    /// Follows Land-Atmos interaction solution for random walk
    /// </summary>
    /// <param name="currVel"></param>
    /// <returns></returns>
    private Vector3 RandomMotion(Vector3 currVel)
    {
        if(this.timescale == 0)
        {
            Debug.LogWarning("(Meander) Timescale is set to 0, should be a positive number. Setting to 100");
            this.timescale = 100;
        }
        float R = Mathf.Exp(-Time.deltaTime / this.timescale);
        float lambda = getGaussianRandom() * Mathf.Sqrt(1.0f - (R * R));
        Vector3 adj = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0, UnityEngine.Random.Range(-1.0f, 1.0f));

        return R * currVel + (lambda * adj);
    }

    private float getGaussianRandom()
    {
        float u1 = 1.0f - UnityEngine.Random.Range(0, 1.0f); //uniform(0,1] random doubles
        float u2 = 1.0f - UnityEngine.Random.Range(0, 1.0f);
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                     Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)
        float randNormal = this.sigmaW * randStdNormal; //random normal(mean,stdDev^2)
        return randNormal;
    }


}
