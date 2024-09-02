using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{

    public GameObject player;
    public static PlayerControl instance;

    public int maxHealth;
    private int currHealth;
    

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }

        this.currHealth = maxHealth;
    }

    public void Damage(float damage)
    {
        this.currHealth -= (int)damage;

        Debug.Log(this.currHealth + "/" + this.maxHealth);
        if(this.currHealth <= 0)
        {
            Debug.Log("DEAD");
        }

    }

    public void RegisterPlayer(GameObject player)
    {
        this.player = player;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
