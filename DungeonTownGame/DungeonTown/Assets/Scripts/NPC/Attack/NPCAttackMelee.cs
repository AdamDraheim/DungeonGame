using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCAttackMelee : NPCAttackHandler
{

    public float attackRadius;

    private float currCooldownTime;

    public void Awake()
    {
        this.currAttackTime = 0;
    }

    public void Update()
    {
        currCooldownTime -= Time.deltaTime;
        currCooldownTime = Mathf.Max(currCooldownTime, 0);
    }

    public override void Attack()
    {

        if (currAttackTime > 0)
        {
            performAttack();
        }

        else if (this.WithinAttackRange() && currCooldownTime <= 0)
        {

            currAttackTime = attackTime;
            
        }


    }

    private void performAttack()
    {
        currAttackTime -= Time.deltaTime;
        if (currAttackTime > 0)
        {
            return;
        }

        PlayerControl.instance.Damage(this.baseDam);
        currCooldownTime = this.cooldownTime;
    }

    public override bool WithinAttackRange()
    {
        return Vector3.Distance(this.transform.position, PlayerControl.instance.player.transform.position) < attackRadius;
    }

}
