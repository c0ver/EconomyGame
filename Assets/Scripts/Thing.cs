using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thing
{
    private static Dictionary<int, Thing> thingList = new Dictionary<int, Thing>();
    private static int NextID = 0;

    public string Name { get; }
    public string Tag { get; }
    public int ID { get; }

    public Thing(string name)
    {
        ID = NextID++;
        Name = name;
        Tag = name + "#" + ID;

        thingList.Add(ID, this);
    }

    public override bool Equals(object obj)
    {
        if (obj != null)
        {
            Thing thing = (Thing)obj;
            return thing.Name.Equals(Name);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Tag.GetHashCode();
    }

    public override string ToString()
    {
        return Tag;
    }

    public virtual string Info()
    {
        return Tag;
    }

    public static string GetTag(int thingID)
    {
        if(thingID == -1)
        {
            return "N/A";
        }

        if(!thingList.ContainsKey(thingID)) {
            Debug.LogErrorFormat("Given {0} but not found in thinglList", thingID);
        }

        return thingList[thingID].Tag;
    }

    public static int GetID(string thingName)
    {
        foreach(var thing in thingList)
        {
            if(thing.Value.Name.Equals(thingName))
            {
                return thing.Key;
            }
        }

        Debug.LogErrorFormat("Given {0} but not found in thingList", thingName);
        return -1;
    }
}
