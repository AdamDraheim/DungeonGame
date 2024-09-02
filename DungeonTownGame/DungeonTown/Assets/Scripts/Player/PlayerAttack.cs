using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class PlayerAttack : MonoBehaviour
{
    public enum AttackType
    {
        Melee,
        Magic,
        Ranged,
        Self
    }

    public enum InputType
    {
        Clicked,
        Held,
        Released
    }

    [Header("Attacks")]
    [SerializeField]
    protected float cooldownTime;
    [SerializeField]
    protected float attackTime;
    [SerializeField]
    protected AttackType attackType;
    [SerializeField]
    protected int baseDamage;
    [SerializeField]
    protected InputType inputType;

    protected float mCurrCooldown;
    protected bool bRefreshed;

    public abstract void Attack();
    protected abstract void GetWeaponAttributes();

    public void Start()
    {
        mCurrCooldown = 0;
        bRefreshed = true;
    }

    public void Update()
    {
        GetInput();
        Cooldown();
    }

    //TODO add weapons for access and setting
    private void ReadWeapon()
    {

    }

    private void GetInput()
    {
        switch (inputType)
        {
            case InputType.Clicked:
                if(Input.GetMouseButtonDown(0))
                    Attack();
                break;
            case InputType.Held:
                if (Input.GetMouseButton(0))
                    Attack();
                break;
            case InputType.Released:
                if (Input.GetMouseButtonUp(0))
                    Attack();
                break;
        }

    }

    private void Cooldown()
    {
        mCurrCooldown -= Time.deltaTime;
        if(mCurrCooldown < 0)
        {
            mCurrCooldown = 0;
            bRefreshed = true;
        }
    }

    public float GetAttackTime()
    {
        return this.attackTime;
    }
}
