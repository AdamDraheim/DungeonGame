using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCAttackRanged : NPCAttackHandler
{

    [Header("Attack Fields")]
    public float attackRadius;
    public float tooCloseRadius;

    [Header("Projectiles")]
    public NPCAttackProjectile projectile;
    public Vector2 fireOffset;
    public float fireVelocity;

    private float currCooldownTime;

    public void Awake()
    {
        if (attackRadius < tooCloseRadius)
            Debug.LogWarning(this.transform.name + " NPCAttackRanged: attack Radius (" + attackRadius + ") is less than the too close radius (" + tooCloseRadius + ")");

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

        FireProjectile();
        currCooldownTime = this.cooldownTime;
    }

    private void FireProjectile()
    {
        NPCAttackProjectile proj = Instantiate(this.projectile, this.transform);
        Vector3 offset = this.GetComponent<NPC>().GetView() * fireOffset.x + Vector3.up * fireOffset.y;
        proj.transform.position = this.transform.localPosition + offset;

        if(fireVelocity == 0)
        {
            Debug.LogWarning("NPCAttackRanged: Fire Projectile has a fire velocity of 0");
            return;
        }

        if(proj.GetGravity(out Vector3 gravity))
        {
            //TODO make equataion work for any gravity not just y
            Vector3 thisPos = proj.transform.position;
            Vector3 targetPos = PlayerControl.instance.player.transform.position;

            Vector2 planar = new Vector2(thisPos.x, thisPos.z);
            Vector2 targetPlanar = new Vector2(targetPos.x, targetPos.z);

            float deltaY = (targetPos.y - thisPos.y);
            float deltaX = Vector2.Distance(targetPlanar, planar) / fireVelocity;

            float vy = (deltaY - (gravity.y * deltaX * deltaX)) / deltaX;

            Vector2 planarDir = (targetPlanar - planar).normalized * fireVelocity / 2.0f;
            Vector3 initProj = new Vector3(planarDir.x, vy, planarDir.y);

            proj.SetProjectileInitials(initProj, this.baseDam);

        }
        else
        {
            Vector3 dir = PlayerControl.instance.player.transform.position - proj.transform.position;
            proj.SetProjectileInitials(dir.normalized * fireVelocity, this.baseDam);
        }

    }

    public override bool WithinAttackRange()
    {
        return Vector3.Distance(this.transform.position, PlayerControl.instance.player.transform.position) < attackRadius
            && Vector3.Distance(this.transform.position, PlayerControl.instance.player.transform.position) > tooCloseRadius;
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(this.transform.position, attackRadius);
        Gizmos.DrawWireSphere(this.transform.position, tooCloseRadius);
        Vector3 offset = this.GetComponent<NPC>().GetView() * fireOffset.x + Vector3.up * fireOffset.y;
        Gizmos.DrawSphere(this.transform.position + offset, 0.25f);
    }
}
