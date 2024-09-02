using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCMovementHandler)), RequireComponent(typeof(NPCAttackHandler))]
public class NPC : MonoBehaviour
{


    [Header("Base Stats")]
    public int maxHealth;
    public float deathTime;

    [Header("Aggression Stats")]
    [SerializeField]
    private float aggroTime;

    private int npcID;
    private bool bAggro;
    private float currAggroTime;
    private int currHealth;
    private bool bMoving;

    private float currDyingTime;
    private bool bAlive;
    private Vector3 view;

    [Header("Debug")]
    public bool bAlwaysAggro;
    public string debugMessage;

    // Start is called before the first frame update
    void Start()
    {
        if(SystemID.system)
            this.npcID = SystemID.system.GenerateNewID("NPC");
        this.currHealth = maxHealth;
        this.bAlive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(bAlwaysAggro)
            AggroNPC();

        if (bAlive)
        {
            HandleAggro();

            if (bAggro && this.GetComponent<NPCAttackHandler>().WithinAttackRange())
            {
                this.GetComponent<NPCAttackHandler>().Attack();
                bMoving = false;
            }
            else
            {
                this.GetComponent<NPCMovementHandler>().Move();
                bMoving = true;
            }
        }
        else
        {
            currDyingTime += Time.deltaTime;
            if (currDyingTime > deathTime) { Destroy(this.gameObject); return; };
        }
        HandleAnim();
    }

    private void HandleAggro()
    {
        if (bAggro)
        {
            this.currAggroTime -= Time.deltaTime;
            if(this.currAggroTime <= 0)
            {
                bAggro = false;
            }
        }
    }

    public int GetID()
    {
        return npcID;
    }

    public void ApplyDamage(int dmg)
    {
        if (!bAlive) return;

        Debug.Log("HIT NPC: " + this.name);
        this.currHealth -= dmg;
        this.AggroNPC();

        WorldSprite2D w2d = this.GetComponentInChildren<WorldSprite2D>();
        if (w2d != null) w2d.ApplyDamage();

        if (currHealth <= 0)
            bAlive = false;
    }


    public void AggroNPC()
    {
        this.bAggro = true;
        this.currAggroTime = this.aggroTime;
    }

    public bool checkAggro()
    {
        return bAggro;
    }

    private void HandleAnim()
    {
        Animator anim = this.GetComponentInChildren<Animator>();
        if (anim == null) return;
        anim.SetBool("isMoving", bMoving);
        anim.SetFloat("attackTime", this.GetComponent<NPCAttackHandler>().GetAttackTimeProp());

        if(!bAlive)
            anim.SetFloat("dyingTime", Mathf.Min((currDyingTime / deathTime), 1.0f));
    }

    public void SetView(Vector3 view)
    {
        this.view = view;
    }

    public Vector3 GetView()
    {
        return this.view;
    }
}
