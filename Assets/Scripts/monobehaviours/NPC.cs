using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    /*private const int START_MONEY = 100;
    protected Rigidbody2D rigidbody;

    public int Money { get; private set; } = START_MONEY;
    public Entity Entity { get; private set; }

    // key is itemID, value is quantity of item
    private Dictionary<int, float> incompleteItems = new Dictionary<int, float>();
    private List<Item> inventory = new List<Item>();

    private int level, deathXP, hostility, birthPriority, XP;
    private bool isAlive;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();

        money = START_MONEY;
        inventory = new Dictionary<int, string>();

        XP = 0;
        isAlive = true;
        level = 1;
        deathXP = 10;
        hostility = 1;
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(this.gameObject.name != "player")
        {
            Destroy(this.gameObject);
            return;
        }

        NPC other = collision.gameObject.GetComponent<NPC>();
        this.loot(other);
    }

    void loot(NPC entity)
    {
        this.money += entity.money;
        this.XP += entity.deathXP;

        foreach(var item in entity.inventory)
        {
            if(this.inventory.ContainsKey(item.Key))
            {
                this.inventory[item.Key] += item.Value;
            } else
            {
                this.inventory.Add(item.Key, item.Value);
            }
        }

        Debug.Log(money);
        Debug.Log(XP);
        Debug.Log(inventory);
    }*/
}
