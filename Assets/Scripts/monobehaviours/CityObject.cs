using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityObject : MonoBehaviour
{
    private City thisCity;

    public int Population { get { return thisCity.Population;  } }

    // Start is called before the first frame update
    void Awake()
    {
        thisCity = new City("Default");
    }

    // Update is called once per frame
    void Update()
    {
        //thisCity.Update();
    }

    public void BeginDay()
    {
        thisCity.BeginDay();
    }

    public void EndDay()
    {
        thisCity.EndDay();
    }
}
