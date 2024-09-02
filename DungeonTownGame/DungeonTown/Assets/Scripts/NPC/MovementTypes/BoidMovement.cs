using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Timers;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class BoidMovement : NPCMovement
{

    private NPC[] boids;
    private Vector3 position;
    private float radius;
    private float cohesiveness;
    private Vector3 currVelocity;
    private float boidInteractRadius;
    private int id;
    private float sr;

    public BoidMovement(float radius, float cohesiveness, float boidInteractRadius, float sr) : base()
    {
        this.radius = radius;
        this.cohesiveness = cohesiveness;
        this.boidInteractRadius = boidInteractRadius;
        this.sr = sr;
    }

    public override Vector3 Move(Vector3 currPosition)
    {
        this.position = currPosition;
        Collect();
        Align();
        Separate();

        return currVelocity.normalized;
    }

    private void Separate()
    {
        Vector3 dir = currVelocity.normalized;

        Vector3 endPos = position + dir;
        foreach(NPC boid in boids)
        {

            if (boid.GetID() != id)
            {
                float dist = Vector3.Distance(endPos, boid.transform.position);
                if (dist < radius || Vector3.Distance(position, boid.transform.position) < radius)
                {
                    float radianChange = 3.14f * sr;
                    currVelocity = Vector3.RotateTowards(currVelocity, -currVelocity, radianChange, 1.0f);
                    break;
                }
            }
            
        }
    }

    private void Align()
    {
        Vector3 avgVel = Vector3.zero;
        int count = 0;
        foreach(NPC boid in boids)
        {
            if (Vector3.Distance(position, boid.transform.position) <= boidInteractRadius)
            {
                avgVel += boid.GetComponent<NPCMovementHandler>().GetVelocity();
                count++;
            }
        }

        avgVel /= count;
        currVelocity = (currVelocity * (1-cohesiveness)) + (avgVel * cohesiveness);

    }

    private void Collect()
    {
        Vector3 avgPos = Vector3.zero;
        int count = 0;
        foreach (NPC boid in boids)
        {
            if (Vector3.Distance(position, boid.transform.position) <= boidInteractRadius)
            {
                avgPos += boid.transform.position;
                count++;
            }
        }

        avgPos /= count;

        Vector3 dir = (avgPos - position).normalized;
        currVelocity = (currVelocity * (1 - cohesiveness)) + (dir * cohesiveness);
    }

    public override string debugMessage()
    {
        return "(Boid) Is a boid"; //TODO need better debug message
    }

    public void AssignBoidGroup(NPC[] boids)
    {
        this.boids = boids;
    }

    public void SetID(int id)
    {
        this.id = id;
    }


}
