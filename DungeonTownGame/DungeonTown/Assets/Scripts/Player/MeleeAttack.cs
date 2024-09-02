using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : PlayerAttack
{
    [SerializeField]
    private LayerMask hitMask;

    [SerializeField]
    private float radius;
    [SerializeField]
    private float angle;

    public override void Attack()
    {

        if (!bRefreshed)
            return;

        GetWeaponAttributes();
        if(this.GetComponentInChildren<PlayerDetection>().QueryCameraSweep(out RaycastHit hit, hitMask, radius, angle))
        {
            if(hit.transform.GetComponent<NPC>() != null)
            {
                hit.transform.GetComponent<NPC>().ApplyDamage(this.baseDamage);
                this.bRefreshed = false;
                this.mCurrCooldown = this.cooldownTime;
            }
        }
    }

    protected override void GetWeaponAttributes()
    {
        //temp auto assign
        //this.radius = 1.0f;
        //this.angle = 45.0f;
        //this.cooldownTime = 1.0f;
    }
}
