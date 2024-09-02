using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCAttackProjectile : MonoBehaviour
{

    public float maxLifetime;

    [Header("Collisions")]
    public bool canHitNPC;
    public LayerMask collisions;

    [Header("Gravity")]
    public bool hasGravity;
    public Vector3 gravity;

    private Vector3 currVelocity;
    private float damage;
    private float currLife;

    void Update()
    {
        this.currVelocity += gravity * Time.deltaTime * (hasGravity ? 1 : 0);

        if(Physics.Raycast(this.transform.position, currVelocity.normalized, out RaycastHit hit, currVelocity.magnitude * Time.deltaTime, collisions))
        {
            if(hit.transform.GetComponent<PlayerMovement>() != null)
            {
                PlayerControl.instance.Damage(this.damage);
            }
            else if (hit.transform.GetComponent<NPC>() && canHitNPC)
            {
                hit.transform.GetComponent<NPC>().ApplyDamage((int)damage);
            }

            Destroy(this.gameObject);
            return;
        }

        this.transform.position += currVelocity * Time.deltaTime;

        currLife += Time.deltaTime;
        if (currLife > maxLifetime)
            Destroy(this.gameObject);

    }

    public void SetProjectileInitials(Vector3 initialVelocity, float damage)
    {
        this.currVelocity = initialVelocity;
        this.damage = damage;
    }

    public bool GetGravity(out Vector3 gravityForce)
    {
        gravityForce = this.gravity;
        return hasGravity;
    }

}
