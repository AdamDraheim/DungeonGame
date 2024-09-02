using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NPCAttackHandler : MonoBehaviour
{
    [SerializeField]
    protected float baseDam;
    [SerializeField]
    protected float attackTime;
    [SerializeField]
    protected float cooldownTime;

    protected float currAttackTime;

    public abstract void Attack();
    public abstract bool WithinAttackRange();

    public float GetAttackTimeProp() { return Mathf.Min((attackTime - currAttackTime ) / attackTime, 1.0f); }
}
