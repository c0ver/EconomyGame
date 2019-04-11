using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Consumable : Item
{
    [Newtonsoft.Json.JsonConstructor]
    public Consumable(string name, string desc, Rarity rarity, Type type, float decayFactor) :
        base(name, desc, rarity, type, decayFactor)
    {
    }
}
